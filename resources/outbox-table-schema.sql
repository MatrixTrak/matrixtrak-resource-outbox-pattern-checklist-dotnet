-- Outbox Table Schema for SQL Server
-- Adapt for Postgres/MySQL as needed
-- Purpose: Store events atomically with business data for reliable publishing

CREATE TABLE outbox_events (
    -- Unique identifier for the event (auto-increment for ordering)
    id                   BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Event discriminator: tells publisher and consumers how to deserialize
    -- Examples: "OrderCreated", "PaymentCompleted", "InventoryReserved"
    event_type           NVARCHAR(256)   NOT NULL,

    -- Full event payload as JSON
    -- Store everything the consumer needs; avoid foreign key lookups
    event_payload        NVARCHAR(MAX)   NOT NULL,

    -- When the event was written (for ordering and debugging)
    created_at_utc       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    -- When the event was successfully published
    -- NULL = unpublished (publisher should pick this up)
    published_at_utc     DATETIME2       NULL,

    -- Request/trace correlation for observability
    -- Ties event back to the originating request
    correlation_id       NVARCHAR(128)   NULL,

    -- Publisher retry tracking
    -- Use for alerting and dead-letter decisions
    retry_count          INT             NOT NULL DEFAULT 0,

    -- Optional: partition key for ordering guarantees
    -- partition_key     NVARCHAR(128)   NULL,

    -- Optional: dead letter timestamp for failed events
    -- dead_lettered_at  DATETIME2       NULL,

    -- Optional: error message for failed publishes
    -- last_error        NVARCHAR(1000)  NULL
);

-- Index for publisher: find unpublished events quickly
-- Filtered index keeps it small and fast
CREATE INDEX ix_outbox_unpublished ON outbox_events (created_at_utc)
    WHERE published_at_utc IS NULL;

-- Optional: index for cleanup job (find old published events)
-- CREATE INDEX ix_outbox_published ON outbox_events (published_at_utc)
--     WHERE published_at_utc IS NOT NULL;


-- ============================================================
-- Postgres equivalent (uncomment if using Postgres)
-- ============================================================
/*
CREATE TABLE outbox_events (
    id                   BIGSERIAL PRIMARY KEY,
    event_type           VARCHAR(256)    NOT NULL,
    event_payload        JSONB           NOT NULL,
    created_at_utc       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    published_at_utc     TIMESTAMPTZ     NULL,
    correlation_id       VARCHAR(128)    NULL,
    retry_count          INT             NOT NULL DEFAULT 0
);

CREATE INDEX ix_outbox_unpublished ON outbox_events (created_at_utc)
    WHERE published_at_utc IS NULL;
*/


-- ============================================================
-- MySQL equivalent (uncomment if using MySQL)
-- ============================================================
/*
CREATE TABLE outbox_events (
    id                   BIGINT AUTO_INCREMENT PRIMARY KEY,
    event_type           VARCHAR(256)    NOT NULL,
    event_payload        JSON            NOT NULL,
    created_at_utc       DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    published_at_utc     DATETIME(6)     NULL,
    correlation_id       VARCHAR(128)    NULL,
    retry_count          INT             NOT NULL DEFAULT 0,
    INDEX ix_outbox_unpublished (created_at_utc)
);
-- Note: MySQL doesn't support filtered indexes; consider a separate query
*/
