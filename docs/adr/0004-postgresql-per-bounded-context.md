# ADR 0004 — PostgreSQL persistence per bounded context

## Status

Accepted.

## Context

The initial Patient Registry slice used an in-memory repository. It could not preserve data across restarts, demonstrate migrations, or provide a realistic boundary for future transactional messaging.

## Decision

Use EF Core 10 with the Npgsql provider. Patient Registry owns the `patient_registry` PostgreSQL schema and exposes persistence only through `IPatientRepository`. Map domain types explicitly, keep database concerns in Infrastructure, and version every schema change with EF Core migrations.

The medical record number has a database-level unique constraint. The repository translates only that known constraint violation into the domain-facing duplicate result; other database failures remain visible.

## Consequences

- Local development requires PostgreSQL from Docker Desktop.
- Application and Domain remain independent of EF Core.
- A future Outbox can share the same database transaction without leaking into the Domain.
- Integration tests against a real PostgreSQL container remain a Phase 4 concern.
