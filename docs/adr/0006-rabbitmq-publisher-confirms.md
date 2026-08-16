# ADR 0006 — RabbitMQ publisher confirms

## Status

Accepted.

## Context

The HL7 gateway must hand accepted integration events to an asynchronous transport without coupling the interoperability boundary to future consumers. A successful client API call alone does not prove that RabbitMQ accepted a publication.

## Decision

Use RabbitMQ through a packable transport adapter. Publish versioned JSON events to the durable topic exchange `pulseguard.events` with persistent delivery mode and publisher confirms enabled. Reuse a recovered connection and serialize channel access because concurrent publishing on a shared channel is unsafe.

The Device Gateway waits for the broker confirmation before returning an application acceptance ACK. Credentials are supplied through .NET User Secrets in development and are never committed. Telemetry contains only technical event metadata; raw clinical payloads are excluded.

## Consequences

- A successful publish means the broker confirmed the message.
- Connection and topology recovery handle transient connection restoration, but the client does not silently buffer failed publications.
- The HL7 processing boundary and RabbitMQ publication are not yet atomic.
- Transactional Outbox/Inbox, retry with jitter, idempotent consumers, and dead-letter handling remain explicit Phase 2 increments.
