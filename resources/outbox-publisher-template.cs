using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YourNamespace.Infrastructure;

/// <summary>
/// Background service that polls the outbox table and publishes events.
/// 
/// Usage:
/// 1. Register in Program.cs: builder.Services.AddHostedService&lt;OutboxPublisher&gt;();
/// 2. Implement IMessageBroker for your broker (RabbitMQ, Azure Service Bus, etc.)
/// 3. Configure polling interval and batch size as needed.
/// 
/// For high throughput, consider CDC (Debezium) instead of polling.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBroker _broker;
    private readonly ILogger<OutboxPublisher> _logger;

    // Configuration (move to IOptions<OutboxOptions> for production)
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 5;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IMessageBroker broker,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var publishedCount = await ProcessBatchAsync(stoppingToken);

                if (publishedCount > 0)
                {
                    _logger.LogInformation(
                        "OutboxPublisher processed {Count} events",
                        publishedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisher stopped");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Fetch unpublished events in order
        var batch = await db.OutboxEvents
            .Where(e => e.PublishedAtUtc == null)
            .Where(e => e.RetryCount < _maxRetries)
            .OrderBy(e => e.Id)
            .Take(_batchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
            return 0;

        var published = 0;

        foreach (var evt in batch)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _broker.PublishAsync(evt.EventType, evt.EventPayload, ct);

                evt.PublishedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Outbox event published: {EventId} {EventType} {CorrelationId} {DurationMs}",
                    evt.Id,
                    evt.EventType,
                    evt.CorrelationId,
                    sw.ElapsedMilliseconds);

                published++;
            }
            catch (Exception ex)
            {
                evt.RetryCount++;
                await db.SaveChangesAsync(ct);

                _logger.LogWarning(ex,
                    "Outbox event failed: {EventId} {EventType} {RetryCount} {DurationMs} {Error}",
                    evt.Id,
                    evt.EventType,
                    evt.RetryCount,
                    sw.ElapsedMilliseconds,
                    ex.Message);

                // If max retries exceeded, log for dead-letter handling
                if (evt.RetryCount >= _maxRetries)
                {
                    _logger.LogError(
                        "Outbox event dead-lettered: {EventId} {EventType} {RetryCount}",
                        evt.Id,
                        evt.EventType,
                        evt.RetryCount);
                }
            }
        }

        return published;
    }
}

/// <summary>
/// Outbox event entity. Add to your DbContext:
/// public DbSet&lt;OutboxEvent&gt; OutboxEvents =&gt; Set&lt;OutboxEvent&gt;();
/// </summary>
public class OutboxEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventPayload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Message broker interface. Implement for your broker.
/// </summary>
public interface IMessageBroker
{
    Task PublishAsync(string eventType, string payload, CancellationToken ct);
}

/// <summary>
/// Example: RabbitMQ implementation (pseudocode).
/// Replace with your actual broker client.
/// </summary>
public class RabbitMqBroker : IMessageBroker
{
    // private readonly IConnection _connection;

    public async Task PublishAsync(string eventType, string payload, CancellationToken ct)
    {
        // Example implementation:
        // var channel = _connection.CreateModel();
        // var body = Encoding.UTF8.GetBytes(payload);
        // channel.BasicPublish(exchange: "events", routingKey: eventType, body: body);
        // await Task.CompletedTask;

        throw new NotImplementedException("Implement for your broker");
    }
}

/// <summary>
/// Example: Azure Service Bus implementation (pseudocode).
/// </summary>
public class AzureServiceBusBroker : IMessageBroker
{
    // private readonly ServiceBusSender _sender;

    public async Task PublishAsync(string eventType, string payload, CancellationToken ct)
    {
        // Example implementation:
        // var message = new ServiceBusMessage(payload)
        // {
        //     Subject = eventType,
        //     ContentType = "application/json"
        // };
        // await _sender.SendMessageAsync(message, ct);

        throw new NotImplementedException("Implement for your broker");
    }
}

// =============================================================
// Usage Example: Writing to Outbox in Same Transaction
// =============================================================

/*
public class OrderService
{
    private readonly AppDbContext _db;

    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        // 1) Write business data
        _db.Orders.Add(order);

        // 2) Write event to outbox (same transaction)
        var outboxEvent = new OutboxEvent
        {
            EventType = "OrderCreated",
            EventPayload = JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                CreatedAtUtc = DateTime.UtcNow
            }),
            CorrelationId = Activity.Current?.Id
        };
        _db.OutboxEvents.Add(outboxEvent);

        // 3) Commit both or roll back both
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
*/

// =============================================================
// Registration in Program.cs
// =============================================================

/*
// Add to your service registration:
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddSingleton<IMessageBroker, RabbitMqBroker>(); // or your broker
*/
