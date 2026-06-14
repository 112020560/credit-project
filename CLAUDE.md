# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build CreditBackend.sln

# Run the API
dotnet run --project Src/Core/CreditSystem.Api/CreditSystem.Api.csproj

# Run all tests
dotnet test CreditBackend.sln

# Run tests in a specific project
dotnet test Src/Core/CreditSystem.Tests/CreditSystem.Tests.csproj
```

> **No EF Core migrations** — the project uses Dapper + raw Npgsql. Schema is managed manually.

## Architecture

This is a **.NET 9** credit management backend following **Clean Architecture** with **DDD**, **CQRS**, and **Event Sourcing**.

### Project Layers

```
Src/
  Core/
    CreditSystem.Domain        — Aggregates, value objects, domain events, rules engine, read models, domain service abstractions
    CreditSystem.Application   — Use cases: commands, queries, handlers (MediatR), validation behaviors, background jobs
    CreditSystem.Infrastructure — Event store (Postgres), projection store (Dapper), repositories, workers, messaging, webhooks
    CreditSystem.Api           — Minimal API endpoints, Program.cs, global exception handler
  Shared/
    SharedKernel               — Messaging contracts (CustomerCreated, CustomerUpdated, payment messages)
    SmartCore.Telemetry        — OpenTelemetry + Serilog configuration
```

### Dependency Graph

```
CreditSystem.Api → CreditSystem.Application + CreditSystem.Infrastructure + SmartCore.Telemetry
CreditSystem.Application → CreditSystem.Domain
CreditSystem.Infrastructure → CreditSystem.Domain + CreditSystem.Application + SharedKernel
CreditSystem.Domain (no external framework dependencies)
```

### Key Patterns

**Event Sourcing** — Both aggregates (`LoanContractAggregate`, `RevolvingCreditAggregate`) accumulate `IDomainEvent` instances in `UncommittedEvents`. State is a separate immutable `record` (`LoanContractState`, `RevolvingCreditState`) updated via a pure `ApplyEvent` switch. Aggregates can be rehydrated from event history or from a snapshot + delta events via their constructors.

**CQRS via MediatR** — Commands and queries are dispatched through MediatR with two pipeline behaviors: `ValidationBehavior` (FluentValidation) and `LoggingBehavior`. Handlers live in `CreditSystem.Application`.

**Projection system** — After persisting events, the `IProjectionEngine` fans out to all registered `IProjection` implementations (scoped) which upsert read models into `PostgresProjectionStore` (Dapper). Read models live in `CreditSystem.Domain/Models/ReadModels/`.

**Rules engine** — `ContractEngine` evaluates a sorted list of `IContractRule` implementations when creating a loan. Rules: `CreditScoreRule`, `DebtToIncomeRule`, `CollateralRule`, `MaxLoanAmountRule`, `ActiveLoansRule`. Hard-stop rules (implement `IHardStopRule`) abort evaluation immediately. The engine accumulates rate adjustments from rules and returns an `Approved`/`Rejected` `ContractEvaluationResponse`.

**Minimal API endpoints** — Endpoint groups are registered as static extension methods (not `IEndpoint`). Each group is explicitly called in `Program.cs`:
```csharp
app.MapLoanContractEndpoints();
app.MapAdminEndpoints();
app.MapDelinquentLoansEndpoints();
app.MapRevolvingCreditEndpoints();
app.MapPaymentsEndpoints();
app.MapWebhooksEndpoints();
```
No auto-discovery via reflection. No URL versioning — routes follow the pattern `/api/{resource}`.

**Outbox pattern** — `OutboxPublisherWorker` (hosted service) polls the outbox table and publishes pending messages via MassTransit, ensuring at-least-once delivery.

**Webhooks** — `WebhookDeliveryWorker` (hosted service) delivers webhook notifications to registered subscribers via `HttpClient`. Repositories: `WebhookSubscriptionRepository`, `WebhookDeliveryRepository`.

### Infrastructure

- **Persistence**: PostgreSQL. No EF Core — uses **Dapper** + **Npgsql** for all database access (event store, projections, query services, repositories).
- **Connection string key**: `CreditDb` (in `appsettings.json` under `ConnectionStrings`).
- **Event store**: `PostgresEventStore` (custom implementation using Dapper). Stores `StoredEvent`, `EventStream`, `EventSnapshot`. Serialization via `JsonEventSerializer`; integrity via `Sha256HashGenerator`.
- **Projection store**: `PostgresProjectionStore` (Dapper). Projectors: `LoanSummaryProjector`, `DelinquentLoansProjector`, `PaymentHistoryProjector`, `LoanPortfolioProjector`, `RevolvingCreditSummaryProjector`, `PaymentTrackingProjector`.
- **Messaging**: MassTransit over RabbitMQ. Config key: `RabbitMqSettings:Uri`.
  - Queue `credit-service-customer-events`: `CustomerCreatedConsumer`, `CustomerUpdatedConsumer`
  - Queue `credit-service-payments`: `ProcessPaymentConsumer`, `ProcessRevolvingPaymentConsumer` (with retry policy)
- **Background workers** (all `IHostedService`): `InterestAccrualWorker`, `PaymentMissedWorker`, `RevolvingInterestAccrualWorker`, `StatementGenerationWorker`, `RevolvingPaymentMissedWorker`, `OutboxPublisherWorker`, `WebhookDeliveryWorker`.
- **Telemetry**: `SmartCore.Telemetry` — OpenTelemetry (ASP.NET Core, HTTP, MassTransit) + Serilog with Seq sink. Configured via `TelemetryOptions`. Service name: `credit-system-service`.
- **Object mapping**: Mapster (in Application layer).
- **Validation**: FluentValidation — each command has a `*Validator` class; `ValidationBehavior` MediatR pipeline runs validation before the handler.

### Domain Model

**Aggregates:**

| Aggregate | States | Key operations |
|-----------|--------|----------------|
| `LoanContractAggregate` | `Approved → Active → Delinquent → Default → PaidOff` | `Create()`, `Disburse()`, `ApplyPayment()`, `AccrueInterest()`, `RecordMissedPayment()`, `MarkAsDefault()`, `Restructure()` |
| `RevolvingCreditAggregate` | `Pending → Active → Frozen → Closed` | `Create()`, `Activate()`, `DrawFunds()`, `ApplyPayment()`, `AccrueInterest()`, `GenerateStatement()`, `Freeze()`, `Unfreeze()`, `ChangeCreditLimit()`, `Close()` |

**Value Objects** (`CreditSystem.Domain/ValueObjects/`): `Money`, `InterestRate`, `AmortizationEntry`, `PaymentSchedule`.

**Entities** (`CreditSystem.Domain/Entities/`): `CustomerReference`, `OutboxMessage`, `WebhookSubscription`.

**Amortization calculators** (`CreditSystem.Domain/Services/Amortization/`): `FrenchAmortizationCalculator`, `GermanAmortizationCalculator`, `FlatAmortizationCalculator`, `AmericanAmortizationCalculator`, `InterestOnlyAmortizationCalculator`. Selection via `AmortizationCalculatorFactory` by `AmortizationMethod` enum. To add a new method: implement `IAmortizationCalculator` and register it in the factory.

**Payment application order** (both aggregates): fees → accrued interest → principal.
