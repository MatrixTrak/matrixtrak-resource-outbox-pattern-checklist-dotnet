# Outbox Pattern Rollout Checklist

Use this checklist to roll out the outbox pattern without breaking production. Each phase has validation steps before proceeding to the next.

---

## Phase 1: Add the Table and Dual Write

**Goal:** Validate that outbox rows are written correctly in the same transaction as business data.

### Tasks

- [ ] Create `outbox_events` table using the provided schema
- [ ] Add EF Core entity or repository method for outbox writes
- [ ] Update critical write paths to insert outbox row in same transaction
- [ ] **Keep existing direct publish calls** (dual write mode)
- [ ] Add logging: `outbox_event_id`, `event_type`, `correlation_id`, `outbox_action: created`

### Validation

- [ ] Deploy to staging/test environment
- [ ] Execute a write that triggers an event
- [ ] Verify: business row exists AND outbox row exists
- [ ] Verify: both have matching correlation_id
- [ ] Verify: outbox row has `published_at_utc = NULL`
- [ ] Verify: logs show `outbox_action: created`

### Rollback

If issues occur:
- [ ] Remove outbox insert code
- [ ] Keep direct publish calls unchanged
- [ ] Drop or truncate outbox table

---

## Phase 2: Deploy the Publisher (Shadow Mode)

**Goal:** Validate that the publisher can read and publish events without replacing the existing path.

### Tasks

- [ ] Deploy `OutboxPublisher` as a BackgroundService
- [ ] Configure polling interval (start: 5 seconds)
- [ ] Configure batch size (start: 100)
- [ ] Add logging: `outbox_action: published`, `duration_ms`, `retry_count`
- [ ] **Keep existing direct publish calls** (events will be duplicated intentionally)

### Validation

- [ ] Deploy to staging/test environment
- [ ] Execute a write that triggers an event
- [ ] Verify: outbox row has `published_at_utc` set
- [ ] Verify: logs show `outbox_action: published`
- [ ] Verify: consumer receives event (may be duplicate of direct publish)
- [ ] Compare: outbox events vs direct publish events (should match)

### Rollback

If issues occur:
- [ ] Stop/disable the OutboxPublisher
- [ ] Direct publish path continues to work
- [ ] Outbox rows remain but are ignored

---

## Phase 3: Remove Direct Publish, Rely on Outbox

**Goal:** The outbox is now the only source of events. Direct publish calls are removed.

### Tasks

- [ ] Remove direct publish calls from write paths
- [ ] Outbox insert is now the only event write
- [ ] OutboxPublisher is the only event source
- [ ] Add alerting for unpublished event backlog (threshold: 100 events older than 1 minute)
- [ ] Add health check for OutboxPublisher

### Validation

- [ ] Deploy to staging/test environment
- [ ] Execute writes and verify events arrive via outbox only
- [ ] Kill the publisher process and verify:
  - [ ] Events are NOT lost (they remain in outbox table)
  - [ ] After restart, pending events are published
- [ ] Verify: no duplicate events (consumer idempotency works)

### Rollback

If issues occur:
- [ ] Re-add direct publish calls (revert code)
- [ ] OutboxPublisher continues to run (harmless duplicates)
- [ ] Fix consumer idempotency if duplicates cause issues

---

## Phase 4: Add Cleanup and Alerting

**Goal:** Production-ready operation with monitoring and maintenance.

### Tasks

- [ ] Add cleanup job: delete events where `published_at_utc < NOW() - 72 hours`
- [ ] Add metric: count of unpublished events
- [ ] Add metric: count of events with `retry_count > 3`
- [ ] Add alert: unpublished backlog > 100 (warn) / > 1000 (critical)
- [ ] Add alert: publisher not running (health check fails)
- [ ] Document runbook for common issues

### Validation

- [ ] Verify cleanup job runs and deletes old events
- [ ] Verify metrics are emitted
- [ ] Trigger alert conditions and verify notifications
- [ ] Run load test and verify no backlog under normal load

### Ongoing

- [ ] Review retry_count distribution monthly
- [ ] Tune polling interval if latency is an issue
- [ ] Consider CDC if polling becomes a bottleneck

---

## Incident Response Quick Reference

| Issue | Check | Fix |
|-------|-------|-----|
| Events not arriving | Is OutboxPublisher running? | Restart service |
| Backlog growing | Is broker reachable? | Check broker health, fix connection |
| Duplicate events | Is consumer idempotent? | Add event_id dedup to consumer |
| Old events stuck | High retry_count? | Check broker errors, consider dead-letter |
| Table growing fast | Cleanup job running? | Verify job schedule, reduce TTL if needed |

---

## Success Criteria

After completing all phases:

- [ ] Events are never lost after a successful database commit
- [ ] Publisher can crash and recover without losing events
- [ ] Consumers handle duplicate deliveries gracefully
- [ ] Alerts fire when backlog grows or publisher fails
- [ ] Table size is bounded by cleanup job
