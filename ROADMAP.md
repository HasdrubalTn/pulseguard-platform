# Roadmap

## Phase 1 — Platform foundation

- [x] Clean Architecture solution structure
- [x] DDD patient aggregate and CQRS command
- [x] REST/OpenAPI and FHIR R4 projection
- [x] HL7 v2 parser and bounded MLLP framing
- [x] TCP/MLLP device gateway
- [x] gRPC client-streaming telemetry contract
- [x] Duende IdentityServer OIDC host
- [x] Unit and architecture tests
- [ ] Persistent PostgreSQL repositories and migrations
- [ ] OpenTelemetry logs, metrics, and traces

## Phase 2 — Reliable asynchronous processing

- [ ] RabbitMQ transport adapter
- [ ] Transactional Outbox and Inbox
- [ ] Idempotent telemetry consumer
- [ ] Retry with backoff and jitter
- [ ] Circuit Breaker and Bulkhead
- [ ] Dead-Letter Queue and replay tooling
- [ ] Backpressure and load shedding

## Phase 3 — Monitoring and alerting

- [ ] Threshold profiles and MonitoringSession aggregate
- [ ] ClinicalAlert aggregate
- [ ] Alert escalation Saga / Process Manager
- [ ] SignalR realtime gateway
- [ ] Synthetic React dashboard
- [ ] Audit context

## Phase 4 — Production engineering labs

- [ ] Testcontainers integration tests
- [ ] Contract and compatibility tests
- [ ] Performance and resilience tests
- [ ] Failure injection scenarios
- [ ] Kubernetes and Helm deployment
- [ ] SLOs, runbooks and incident exercises
