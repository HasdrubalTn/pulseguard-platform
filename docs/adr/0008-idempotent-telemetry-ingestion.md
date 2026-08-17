# ADR 0008 — Idempotent telemetry ingestion

## Status

Accepted.

## Context

The gRPC telemetry contract uses client streaming. A device or intermediary can lose the final response and resend measurements that the server already committed. Treating every delivery as new would duplicate time-series data and could later trigger duplicate monitoring evaluations or alerts.

An identifier alone is insufficient when a faulty producer reuses it for different clinical content. The consumer must distinguish a legitimate redelivery from a conflicting message.

## Decision

Use `measurement_id` as both the telemetry measurement primary key and the transactional Inbox key.

The reusable `TransactionalInboxProcessor<TDbContext>`:

- computes and stores a SHA-256 fingerprint of the canonical application input;
- invokes the consumer handler only for a first delivery or an explicitly pending Inbox row;
- commits the Inbox row, its processed timestamp, and every tracked consumer side effect with one `SaveChanges` transaction;
- suppresses a completed message when its ID, event type, and fingerprint match;
- rejects the same ID when its event type or fingerprint differs;
- uses a fresh context per message so failed processing cannot leak tracked state into the next stream item.

Telemetry stores measurements in its own PostgreSQL `telemetry` schema. The gRPC response reports accepted, rejected, and duplicate counts separately. Logs and trace attributes contain technical IDs and measurement types only; patient IDs and clinical values are excluded.

## Consequences

- Retrying an identical measurement is safe and does not duplicate its side effect.
- An Inbox record and its measurement are either both committed or both rolled back.
- A conflicting ID reuse is visible and rejected rather than silently treated as a duplicate.
- The Inbox table grows with accepted measurements; retention and archival policy must be designed before production use.
- This decision does not add transport retries, exponential backoff, jitter, dead-letter queues, or replay. Those remain separate Phase 2 increments.
