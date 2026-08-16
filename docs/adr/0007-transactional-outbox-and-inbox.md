# ADR 0007 — Transactional Outbox and Inbox

## Status

Accepted.

## Context

Saving a Patient Registry aggregate and publishing its Integration Event are two independent resource operations. Publishing before the database commit can announce state that is later rolled back; publishing after the commit can lose the event when the process stops between both operations. RabbitMQ publisher confirms do not close this database-to-broker gap.

Consumers must also assume at-least-once delivery. A publisher can stop after RabbitMQ confirms a message but before the Outbox row is marked as published, causing the same event ID to be published again.

## Decision

Use the packable `PulseGuard.Framework.Messaging.EntityFrameworkCore` building block:

- a SaveChanges interceptor maps selected Domain Events to versioned Integration Events;
- the aggregate and serialized Outbox row are committed by the same EF Core transaction;
- a hosted dispatcher publishes committed rows through `IIntegrationEventPublisher` and marks each row only after its publisher confirm;
- every bounded context stores its Outbox and Inbox tables in its own database schema;
- the Inbox primary key is the Integration Event ID, and a SHA-256 payload fingerprint detects an invalid reuse of that ID;
- an Inbox row and its consumer side effects must be saved by the same local database transaction.

The Patient Registry emits `PatientRegisteredIntegrationEvent` with only its technical patient ID. Medical record numbers and other clinical identifiers are excluded from the event.

## Consequences

- Patient registration and event intent are atomic.
- Dispatch provides at-least-once delivery, not exactly-once delivery.
- A crash between broker confirmation and the Outbox status update can produce a duplicate; consumers suppress it with the Inbox event ID.
- Outbox payloads are trusted application data but event types must be registered explicitly before deserialization.
- Fixed polling is used in this increment. Backoff with jitter, distributed claim leases, the idempotent telemetry consumer, dead-letter handling and replay remain separate Phase 2 backlog items.
