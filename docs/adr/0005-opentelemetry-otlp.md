# ADR 0005 — OpenTelemetry with OTLP

## Status

Accepted.

## Context

Console logs alone cannot correlate requests, database calls, gRPC operations, and background ingestion across process boundaries.

## Decision

Use the OpenTelemetry .NET SDK for logs, metrics, and traces. Centralize host configuration in the packable `PulseGuard.Framework.Observability` building block and export with OTLP. Use the OpenTelemetry Collector as the local boundary so application code does not depend on a specific observability backend.

Telemetry is disabled by default outside Development. Configuration uses `OpenTelemetry:Enabled` and `OpenTelemetry:OtlpEndpoint`, while the exporter also remains compatible with standard OTLP environment configuration.

## Consequences

- Every host emits consistently attributed telemetry.
- A backend can be replaced without changing application code.
- Raw clinical payloads, credentials, and patient data are forbidden in telemetry attributes.
- The local collector currently uses the debug exporter; durable storage and dashboards are deferred.
