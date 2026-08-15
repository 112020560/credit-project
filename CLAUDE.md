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

**Projection system** — Projections are **asynchronous and eventually consistent**. Handlers only write to the event store; `ProjectionDispatcherWorker` (background service) reads events from `stored_events` and dispatches them to all registered `IProjection` implementations. `IProjectionEngine` remains registered in DI but is no longer called from command handlers or jobs. Read models live in `CreditSystem.Domain/Models/ReadModels/`.

**Rules engine** — `ContractEngine` evaluates a sorted list of `IContractRule` implementations when creating a loan. Rules: `CreditScoreRule`, `DebtToIncomeRule`, `CollateralRule`, `MaxLoanAmountRule`, `ActiveLoansRule`, `MemberSharesRule`, `PaymentCapacityRule`. Hard-stop rules (implement `IHardStopRule`) abort evaluation immediately. The engine accumulates rate adjustments and returns an `Approved`/`Rejected` `ContractEvaluationResponse`.

- `ContractEngine` constructor takes only `IEnumerable<IContractRule>` — it does **not** receive `UnderwritingPolicy` directly.
- All rules read policy parameters from `context.Policy` (a `UnderwritingPolicy` field on `ContractEvaluationContext`). No constructor-injected policy in rules.
- The fallback base rate used when no rule adjusts the rate is `context.Policy.BaseInterestRate`.

**Minimal API endpoints** — Endpoint groups are registered as static extension methods (not `IEndpoint`). Each group is explicitly called in `Program.cs`:
```csharp
app.MapLoanContractEndpoints();
app.MapAdminEndpoints();
app.MapDelinquentLoansEndpoints();
app.MapRevolvingCreditEndpoints();
app.MapPaymentsEndpoints();
app.MapWebhooksEndpoints();
app.MapUnderwritingPolicyEndpoints();
app.MapProjectionAdminEndpoints();    // GET|POST /api/v1/admin/projection/*
```
No auto-discovery via reflection. No URL versioning — routes follow the pattern `/api/{resource}`. When adding a new endpoint group: create a static class in `Src/Core/CreditSystem.Api/Endpoints/`, add the `Map*` call in `Program.cs`.

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
- **Background workers** (all `IHostedService`): `InterestAccrualWorker`, `PaymentMissedWorker`, `RevolvingInterestAccrualWorker`, `StatementGenerationWorker`, `RevolvingPaymentMissedWorker`, `OutboxPublisherWorker`, `WebhookDeliveryWorker`, `ProjectionDispatcherWorker`.
- **Telemetry**: `SmartCore.Telemetry` — OpenTelemetry (ASP.NET Core, HTTP, MassTransit) + Serilog with Seq sink. Configured via `TelemetryOptions`. Service name: `credit-system-service`.
- **Object mapping**: Mapster (in Application layer).
- **Validation**: FluentValidation — each command has a `*Validator` class; `ValidationBehavior` MediatR pipeline runs validation before the handler.

### Event Store — Qué es y cómo funciona

En lugar de guardar el estado actual de un objeto (como haría un CRUD), el event store guarda la secuencia de cosas que pasaron:

```
CRUD normal:
  tabla loans → { id, balance: 450000, status: 'Active', rate: 16.5 }

Event Store:
  stream fdb3f4d4 → [
    ContractCreated   (monto: 1,500,000, tasa: 16.5%, plazo: 12)
    ContractApproved  (tasa aprobada: 16.5%)
    LoanDisbursed     (método: WIRE)
    PaymentApplied    (monto: 30,000, nuevo balance: 1,470,000)
  ]
```

El balance actual de 1,470,000 no se guarda en ninguna columna — se calcula reproduciendo los eventos en orden.

**Tablas en Postgres:**

| Tabla | Propósito |
|-------|-----------|
| `event_streams` | Un registro por aggregate (`stream_id`, `version`) |
| `stored_events` | Todos los eventos de todos los aggregates |
| `event_outbox` | Cola para publicar eventos vía RabbitMQ (OutboxPublisherWorker) |
| `event_snapshots` | Foto del estado cada N eventos (optimización de recarga) |
| `projection_checkpoints` | Última `sequence` procesada por cada projector |
| `projection_failures` | Eventos que fallaron proyección después de reintentos |

**Dos campos distintos en `stored_events`:**

| Campo | Alcance | Propósito |
|-------|---------|-----------|
| `version` | Por aggregate | Control de concurrencia optimista (`expectedVersion`) |
| `sequence` | Global (BIGSERIAL) | Posición en el log global; asignada automáticamente por Postgres |

`version` responde "¿cuál es el estado de este contrato?". `sequence` responde "¿en qué orden llegaron todos los eventos del sistema?".

**Flujo al crear un contrato (estado actual):**

```
POST /loans
  ↓ Handler evalúa reglas
  ↓ LoanContractAggregate.Create() → genera ContractCreated + ContractApproved en memoria
  ↓ LoanContractRepository.SaveAsync()
      → INSERT event_streams (version = 2)
      → INSERT stored_events (ContractCreated, ContractApproved) — Postgres asigna sequence
      → INSERT event_outbox
  ↓ return 201  ← "comando aceptado, eventos persistidos"
  ↓
  [segundos después]
  ProjectionDispatcherWorker (tick cada 5s)
      → GetCheckpointAsync("LoanSummary") → e.g. 1000
      → GetEventsSinceSequenceAsync(1000, 100) → eventos nuevos
      → Deserializar → projector.ProjectAsync(event)
          → LoanSummaryProjector: ContractCreated → INSERT rm_loan_summaries
          → DelinquentLoansProjector: ContractCreated → no hace nada
          → ...
      → SaveCheckpointAsync("LoanSummary", 1002)
```

**Cómo sabe cada projector a qué tabla escribir:**

El worker no sabe. Solo deserializa el evento y lo entrega a todos los projectors. Cada projector tiene un `switch` interno:

```csharp
// LoanSummaryProjector
switch (@event)
{
    case ContractCreated e:   → UPSERT rm_loan_summaries
    case LoanDisbursed e:     → UPDATE rm_loan_summaries
    case PaymentApplied e:    → UPDATE rm_loan_summaries
    case FundsDrawn e:        → no hace nada (evento de revolving)
}
```

El mismo `ContractCreated` pasa por los 7 projectors; cada uno decide si le interesa.

**Primer arranque / checkpoint vacío:**

Si `projection_checkpoints` está vacía (o `last_sequence = 0`), el worker reproduce todos los eventos desde el principio. Esto es correcto: los projectors usan `UPSERT`, por lo que el replay es idempotente. No hay pérdida de datos de negocio — el event store siempre tiene los eventos. Los read models son derivados reconstruibles.

**Visibilidad de fallos:**

Si un projector falla 4 veces (1 intento + 3 reintentos con backoff 1s/2s/4s), el error se registra en `projection_failures` y el checkpoint avanza igualmente (para no bloquear eventos posteriores). Los fallos son visibles en `GET /api/v1/admin/projection/failures`. Para reconstruir todo desde cero: `POST /api/v1/admin/projection/rebuild`.

**Health check:**

`ProjectionHealthCheck` devuelve `Degraded` si hay fallos no resueltos en los últimos 30 minutos, `Healthy` si no.

**Invariante fundamental:**

> El event store es la fuente de verdad. Los read models son derivados eventualmente consistentes. Un 201 significa "comando aceptado y eventos persistidos", no "read model actualizado".

### Domain Model

**Aggregates:**

| Aggregate | States | Key operations |
|-----------|--------|----------------|
| `LoanContractAggregate` | `Approved → Active → Delinquent → Default → PaidOff` | `Create()`, `Disburse()`, `ApplyPayment()`, `AccrueInterest()`, `RecordMissedPayment()`, `MarkAsDefault()`, `Restructure()` |
| `RevolvingCreditAggregate` | `Pending → Active → Frozen → Closed` | `Create()`, `Activate()`, `DrawFunds()`, `ApplyPayment()`, `AccrueInterest()`, `GenerateStatement()`, `Freeze()`, `Unfreeze()`, `ChangeCreditLimit()`, `Close()` |

**Value Objects** (`CreditSystem.Domain/ValueObjects/`): `Money`, `InterestRate`, `AmortizationEntry`, `PaymentSchedule`.

**Entities** (`CreditSystem.Domain/Entities/`): `CustomerReference`, `OutboxMessage`, `WebhookSubscription`.

**Models** (`CreditSystem.Domain/Models/`):
- `UnderwritingPolicy` — record with parameters: `BaseInterestRate`, `AutoDefaultThresholdDays`, `NoScoreBehavior`, `SharesMultiplierLimit`, `RequireActiveMembership`, `GracePeriodDays`, `PenaltyRate`, `OriginationFeeRate`, `EnforceSharesCapacityLimit`, `MaxDtiRatio`, `Id`.
- `CreditProduct` — exposes `UnderwritingPolicyId` (string FK to `underwriting_policies.id`, default `"default"`).

**UnderwritingPolicy loading** — There is **no** `AddSingleton<UnderwritingPolicy>`. Policy is loaded per evaluation:
- `CreateContractCommandHandler`: calls `IUnderwritingPolicyRepository.GetByIdAsync(product.UnderwritingPolicyId)` after loading the product. Returns error if null. Assigns the policy to `ContractEvaluationContext.Policy`.
- `PaymentMissedJob` / `LoanQueryService`: call `IUnderwritingPolicyRepository.GetActiveAsync()` (loads `id = 'default'`) on demand.
- The `"default"` row must always exist in `underwriting_policies` (guaranteed by the original seed migration). `IUnderwritingPolicyRepository` is registered as **scoped**.

**Amortization calculators** (`CreditSystem.Domain/Services/Amortization/`): `FrenchAmortizationCalculator`, `GermanAmortizationCalculator`, `FlatAmortizationCalculator`, `AmericanAmortizationCalculator`, `InterestOnlyAmortizationCalculator`. Selection via `AmortizationCalculatorFactory` by `AmortizationMethod` enum. To add a new method: implement `IAmortizationCalculator` and register it in the factory.

**Payment application order** (both aggregates): fees → accrued interest → principal.

## Engineering Rules

### General

- Preserve the existing architecture unless a change explicitly requires an architectural decision.
- Do not introduce new frameworks, libraries, or architectural patterns without justification.
- Prefer existing abstractions and patterns over introducing new ones.
- Do not modify unrelated code while implementing a change.
- Do not infer business rules when they are not explicitly defined in the domain model or specification.
- When existing code and documentation disagree, inspect the implementation and tests before making a decision.

### Domain

- Domain logic belongs in `CreditSystem.Domain`.
- Domain must not depend on Application, Infrastructure, API, or external frameworks.
- Business invariants must be enforced by aggregates or domain services.
- Do not move business rules into API endpoints or infrastructure code.
- Domain events must represent meaningful domain behavior, not technical persistence events.

### Application

- Application handlers orchestrate use cases; they should not contain domain business rules.
- Commands mutate state through aggregates.
- Queries must use read models or query services rather than reconstructing aggregates unnecessarily.
- Validation that represents business invariants belongs in the domain; request/input validation belongs in FluentValidation.

### Infrastructure

- PostgreSQL access must use Dapper + Npgsql.
- Do not introduce Entity Framework Core.
- Do not create EF Core migrations.
- Infrastructure implementations must respect abstractions defined by the Domain/Application layers.
- Messaging and external integrations belong in Infrastructure.

### API

- Keep Minimal API endpoints thin.
- Endpoints should delegate business operations to Application handlers.
- Do not implement domain logic inside endpoint definitions.
- Follow the existing `/api/{resource}` routing convention.
- Do not introduce URL versioning unless explicitly required.

### Testing

- Every new business rule must have domain-level tests.
- Every new use case must have application-level tests where appropriate.
- Changes affecting persistence, messaging, projections, or integrations must include appropriate integration tests.
- Existing tests must remain passing before considering a change complete.

### Changes

- Before implementing a non-trivial change, inspect the relevant OpenSpec specifications and existing implementation.
- If the requested behavior is not defined, identify the ambiguity instead of inventing business behavior.
- Architectural changes require explicit analysis before implementation.
- Keep changes focused on the requested behavior.

## Development Workflow (OpenSpec)

Changes to this project follow a spec-driven workflow managed via the `openspec` CLI and the `opsx:*` skills.

### Change lifecycle

```
opsx:propose  →  opsx:apply  →  opsx:archive
```

| Step | Command | What it does |
|------|---------|--------------|
| Propose | `/opsx:propose <name>` | Creates `proposal.md`, `design.md`, specs, and `tasks.md` under `openspec/changes/<name>/` |
| Apply | `/opsx:apply <name>` | Implements tasks from `tasks.md` one by one, marking each `[x]` when done |
| Archive | `/opsx:archive <name>` | Moves the change folder to `openspec/changes/archive/YYYY-MM-DD-<name>/` and syncs delta specs to main specs |

### Directory structure

```
openspec/
  changes/
    <active-change>/          — In-progress change (proposal, design, specs, tasks)
    archive/
      YYYY-MM-DD-<name>/      — Completed changes
  specs/
    <capability>/spec.md      — Canonical specs (updated on archive via sync)
```

### Rules

- Active changes live in `openspec/changes/`. Do not edit files in `archive/`.
- `tasks.md` is the source of truth for implementation progress. Mark tasks `[x]` immediately after completing them.
- Delta specs in `openspec/changes/<name>/specs/` are merged into `openspec/specs/` on archive.
- Migrations go in `Src/Core/CreditSystem.Infrastructure/Migrations/` with naming `YYYYMMDD_Description.sql`. They are applied manually (no EF Core).