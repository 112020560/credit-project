## Purpose

Defines how the CRM service publishes domain events reliably using the Transactional Outbox pattern via `SmartCore.Outbox`. All event publishing from CommandHandlers goes through `IOutboxWriter.AppendAsync()` into a dedicated `outbox_db`. A standalone `outbox-worker` reads the outbox and delivers events to RabbitMQ asynchronously.

## Requirements

### Requirement: Domain events are persisted to the outbox before delivery
The system SHALL write every domain event to the `outbox_db` via `IOutboxWriter.AppendAsync()` within the same application request that mutates domain state. The outbox write occurs after the domain `SaveChanges()` succeeds. The `outbox-worker` (independent service) is responsible for reading the outbox and delivering events to RabbitMQ.

#### Scenario: Event persisted successfully after domain commit
- **WHEN** a CommandHandler successfully commits domain state and calls `IOutboxWriter.AppendAsync()`
- **THEN** a row is inserted into `outbox_db.Events` with `Status = Pending`, the correct `ServiceName`, `EventType`, `AggregateId`, and a JSON `Payload`

#### Scenario: Domain commit succeeds but outbox write fails
- **WHEN** the domain `SaveChanges()` succeeds but `IOutboxWriter.AppendAsync()` throws
- **THEN** the domain state is preserved, the error is logged as a warning, and the HTTP response still returns success (the event can be reinserted via replay)

### Requirement: Each event carries a deterministic deduplication key
The system SHALL assign a `DeduplicationKey` to every `OutboxEvent` using the format `{EventType}:{AggregateId}`. The `outbox_db` enforces a UNIQUE constraint on this column, so re-inserting the same logical event is a no-op. For events that can occur multiple times on the same aggregate (e.g. `CustomerUpdated`), the key SHALL include a timestamp suffix.

#### Scenario: Duplicate event insertion is silently ignored
- **WHEN** `IOutboxWriter.AppendAsync()` is called twice with the same `DeduplicationKey`
- **THEN** only one row exists in `outbox_db.Events` and no exception is thrown to the caller

### Requirement: Consumers MUST check idempotency before processing
Any service consuming events delivered by the `outbox-worker` SHALL verify via `IIdempotencyGuard.AlreadyProcessedAsync()` before executing business logic, and SHALL call `IIdempotencyGuard.MarkAsProcessedAsync()` after successful processing.

#### Scenario: Redelivered event is skipped by consumer
- **WHEN** RabbitMQ redelivers an event that was already processed (e.g., after a consumer crash)
- **THEN** the consumer detects it via `AlreadyProcessedAsync()` and returns without executing business logic again

#### Scenario: First-time event is processed and marked
- **WHEN** a consumer receives an event for the first time
- **THEN** business logic executes and `MarkAsProcessedAsync()` records the `(EventId, ConsumerName)` pair in `outbox_db.ProcessedEvents`

### Requirement: SmartCore.Outbox is configured via a dedicated connection string
The system SHALL read the outbox database connection from the configuration key `Outbox:ConnectionString`. This connection points to `outbox_db`, a dedicated PostgreSQL database separate from the CRM domain database. The `SmartCore.Outbox` NuGet applies its own EF migrations to this database on startup.

#### Scenario: Outbox registered correctly at startup
- **WHEN** `AddSmartOutbox(options => { options.ConnectionString = ...; options.ServiceName = "crm"; })` is called in the infrastructure registration
- **THEN** `IOutboxWriter` and `IIdempotencyGuard` are resolvable from the DI container and the `outbox_db` schema is up to date
