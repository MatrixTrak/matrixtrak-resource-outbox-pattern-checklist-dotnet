# Outbox Pattern Checklist + Schema for .NET

This package helps teams implement reliable event publishing using the outbox pattern. Use it when you write to a database and publish events as separate operations, and you've seen (or cannot rule out) lost events after successful commits.

## What's Inside

| File | Purpose |
|------|---------|
| `outbox-table-schema.sql` | Minimal SQL Server schema (adapt for Postgres/MySQL) |
| `outbox-rollout-checklist.md` | Phase-by-phase rollout plan with validation steps |
| `outbox-publisher-template.cs` | Starter BackgroundService for polling |
| `README.md` | This file |

## Quick Start

1. **Create the outbox table** using `outbox-table-schema.sql`
2. **Update your write paths** to insert an outbox row in the same transaction as business data
3. **Deploy the publisher** using `outbox-publisher-template.cs` as a starting point
4. **Make consumers idempotent** by storing processed event IDs

## How to Use

### On Call

- Check if unpublished events are piling up: `SELECT COUNT(*) FROM outbox_events WHERE published_at_utc IS NULL`
- Check publisher health: is the BackgroundService running?
- If events are lost, query the outbox for correlation_id to trace the issue

### Tech Lead

- Add the outbox table to your migration scripts
- Wrap business writes + outbox inserts in a single transaction
- Deploy the publisher as a hosted service
- Add alerting for unpublished event backlog

### CTO

- The outbox guarantees events are not lost after database commits
- Operational cost: one table, one background service, cleanup job
- Risk reduction: eliminates "order created but notification never sent" incidents

## Prerequisites

- SQL Server 2016+ (or adapt schema for Postgres/MySQL)
- .NET 6+ for the publisher template
- Entity Framework Core (or Dapper/raw ADO.NET)

## Customization

The schema and publisher are minimal starting points. Extend as needed:

- Add `aggregate_id` or `partition_key` for ordering
- Add `dead_lettered_at_utc` for failed events
- Switch from polling to CDC (Debezium) for high throughput
- Add schema versioning for `event_payload`

## Related Resources

- [Outbox pattern blog post](/blog/outbox-pattern-without-enterprise-baggage-reliable-writes-events)
- [Idempotency keys for APIs](/blog/idempotency-keys-for-apis-prevent-duplicate-orders-emails-writes)
- [Microsoft outbox pattern guidance](https://learn.microsoft.com/azure/architecture/best-practices/transactional-outbox)

## Support

If you need help implementing the outbox pattern or hardening your event pipeline, see [MatrixTrak services](/services).
