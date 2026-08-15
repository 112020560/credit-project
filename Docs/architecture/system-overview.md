# Credit System — Arquitectura y Documentación Técnica

> **Versión:** 1.0 | **Fecha:** 2026-08-10 | **Stack:** .NET 9 · PostgreSQL · RabbitMQ

---

## Índice

1. [Visión General](#1-visión-general)
2. [Estructura del Proyecto](#2-estructura-del-proyecto)
3. [Arquitectura de Capas](#3-arquitectura-de-capas)
4. [Dominio — Modelo de Negocio](#4-dominio--modelo-de-negocio)
5. [Agregados y Ciclo de Vida](#5-agregados-y-ciclo-de-vida)
6. [Event Sourcing](#6-event-sourcing)
7. [CQRS y Pipeline de Mediator](#7-cqrs-y-pipeline-de-mediator)
8. [Motor de Reglas de Suscripción](#8-motor-de-reglas-de-suscripción)
9. [Amortización](#9-amortización)
10. [API REST — Endpoints](#10-api-rest--endpoints)
11. [Sistema de Proyecciones](#11-sistema-de-proyecciones)
12. [Workers en Segundo Plano](#12-workers-en-segundo-plano)
13. [Mensajería y Patrón Outbox](#13-mensajería-y-patrón-outbox)
14. [Webhooks](#14-webhooks)
15. [Motor de Documentos](#15-motor-de-documentos)
16. [Tasas Variables y Reajuste](#16-tasas-variables-y-reajuste)
17. [Clasificación de Riesgo (SUGEF 1-05)](#17-clasificación-de-riesgo-sugef-1-05)
18. [Infraestructura de Producción](#18-infraestructura-de-producción)
19. [Base de Datos — Esquema](#19-base-de-datos--esquema)
20. [Telemetría y Observabilidad](#20-telemetría-y-observabilidad)
21. [Testing](#21-testing)

---

## 1. Visión General

El **Credit System** es un backend de gestión de crédito para cooperativas y financieras, construido sobre .NET 9 siguiendo principios de **Clean Architecture**, **Domain-Driven Design (DDD)**, **CQRS** y **Event Sourcing**.

El sistema gestiona dos productos crediticios:

| Producto | Descripción |
|---|---|
| **Préstamo a Plazo** (`LoanContractAggregate`) | Crédito amortizable con tabla de pagos fija. Soporta tasa fija y tasa variable (spread + tasa de referencia). |
| **Línea de Crédito Revolvente** (`RevolvingCreditAggregate`) | Crédito rotativo con ciclos de facturación, disposiciones parciales y pago mínimo. |

### Principios de Diseño

- **Sin ORM**: Toda la persistencia usa Dapper + Npgsql. No hay EF Core.
- **Event Sourcing completo**: El estado de los agregados se reconstruye reproduciendo eventos.
- **Separación lectura/escritura**: Los read models se proyectan en tablas `rm_*` de PostgreSQL.
- **At-least-once delivery**: Patrón Outbox para mensajes a RabbitMQ.
- **Multi-instancia seguro**: Distributed locking via advisory locks de PostgreSQL para workers.

---

## 2. Estructura del Proyecto

```
CreditBackend.sln
├── Src/
│   ├── Core/
│   │   ├── CreditSystem.Domain           # Dominio puro — sin dependencias de framework
│   │   ├── CreditSystem.Application      # Casos de uso: commands, queries, jobs
│   │   ├── CreditSystem.Infrastructure   # Persistencia, workers, messaging, documentos
│   │   ├── CreditSystem.Api              # Minimal API endpoints + Program.cs
│   │   └── CreditSystem.Tests            # Tests unitarios (xUnit + FluentAssertions + NSubstitute)
│   └── Shared/
│       ├── SharedKernel                  # Contratos de mensajes cross-service
│       └── SmartCore.Telemetry           # OpenTelemetry + Serilog
├── Docs/                                 # Documentación técnica
│   ├── architecture/                     # Diagramas y decisiones de arquitectura
│   └── adr/                              # Architecture Decision Records
└── openspec/                             # Especificaciones de cambios (OpenSpec workflow)
    ├── specs/                            # Specs vivas del sistema
    └── changes/                          # Cambios en progreso y archivados
```

### Dependencias entre proyectos

```
                    ┌─────────────────────────────┐
                    │      CreditSystem.Api        │
                    │  (Minimal API + Program.cs)  │
                    └──────────┬──────────┬────────┘
                               │          │
              ┌────────────────┘          └─────────────────┐
              ▼                                             ▼
┌─────────────────────────┐             ┌──────────────────────────────┐
│ CreditSystem.Application│             │ CreditSystem.Infrastructure  │
│ (Commands, Queries, Jobs)│◄───────────│ (EventStore, Projections,    │
└────────────┬────────────┘             │  Workers, Documents, MQ)     │
             │                          └──────────────┬───────────────┘
             │                                         │
             └─────────────────┬───────────────────────┘
                               ▼
              ┌────────────────────────────┐
              │    CreditSystem.Domain     │
              │ (Aggregates, ValueObjects, │
              │  Events, Abstractions)     │
              └────────────────────────────┘
```

---

## 3. Arquitectura de Capas

### Domain (`CreditSystem.Domain`)

Núcleo del sistema. Sin dependencias externas de framework. Contiene:

- **Agregados**: `LoanContractAggregate`, `RevolvingCreditAggregate`
- **Value Objects**: `Money`, `InterestRate`, `AmortizationEntry`, `PaymentSchedule`
- **Entidades**: `CustomerReference`, `OutboxMessage`, `WebhookSubscription`
- **Eventos de dominio**: todos los `IDomainEvent` por agregado
- **Abstracciones**: `ILoanContractRepository`, `IRevolvingCreditRepository`, `IEventStore`, `IDocumentGenerator`, etc.
- **Servicios de dominio**: calculadoras de amortización (`IAmortizationCalculator`)
- **Read Models**: modelos de lectura que mapean a tablas `rm_*`
- **Motor de reglas**: `IContractRule`, `ContractEngine`

### Application (`CreditSystem.Application`)

Orquesta los casos de uso. Depende solo de Domain.

- **Commands / Handlers**: crean, mudan y persisten agregados via `IMediator`
- **Queries / Handlers**: consultan read models via `ILoanQueryService`, `IRevolvingCreditQueryService`
- **Pipeline Behaviors**: `ValidationBehavior` (FluentValidation), `LoggingBehavior`
- **Jobs**: `IInterestAccrualJob`, `IPaymentMissedJob`, `IStatementGenerationJob`, etc.
- **Validadores**: `*Validator` por cada command

### Infrastructure (`CreditSystem.Infrastructure`)

Implementaciones concretas. Depende de Domain + Application + SharedKernel.

- **Event Store**: `PostgresEventStore` + `JsonEventSerializer` + `Sha256HashGenerator`
- **Repositories**: `LoanContractRepository`, `RevolvingCreditRepository`, `CustomerReadRepository`
- **Projections**: `IProjectionEngine` + 6 projectors
- **Workers**: 7 `BackgroundService`s
- **Messaging**: MassTransit (consumidores y outbox publisher)
- **Documents**: `ScribanTemplateEngine`, `QuestPdfRenderer`, `ExcelExporter`, `DocumentGenerator`
- **Locking**: `PostgresDistributedLock` (advisory locks)
- **DI**: `DependencyInjection.cs` registra todos los servicios

### API (`CreditSystem.Api`)

Capa de entrada HTTP. Depende de Application + Infrastructure.

- **Minimal API**: endpoints agrupados en clases estáticas de extensión
- **Versioning**: URL segment (`/api/v1/...`)
- **Swagger/OpenAPI**: generado automáticamente
- **GlobalExceptionHandler**: traduce excepciones de dominio a `ProblemDetails`
- **Health Checks**: `/health` (liveness) y `/health/ready` (readiness con PostgreSQL + RabbitMQ)

---

## 4. Dominio — Modelo de Negocio

### Value Objects

| Tipo | Propiedades | Invariantes |
|---|---|---|
| `Money` | `Amount (decimal)`, `Currency (string)` | Amount >= 0; currencies deben coincidir en operaciones |
| `InterestRate` | `AnnualRate`, `RateType (Fixed/Variable)`, `Spread?`, `ReferenceRateId?` | AnnualRate > 0; Variable requiere ReferenceRateId |
| `AmortizationEntry` | `EntryNumber`, `DueDate`, `Payment`, `Interest`, `Principal`, `RemainingBalance` | Todos los montos >= 0 |
| `PaymentSchedule` | `IReadOnlyList<AmortizationEntry> Entries` | Inmutable |

### Entidades

| Entidad | Propósito |
|---|---|
| `CustomerReference` | Referencia al cliente externo (sync via mensajería). Contiene score crediticio, ingresos, deuda existente. |
| `OutboxMessage` | Mensaje pendiente de publicar a RabbitMQ |
| `WebhookSubscription` | Suscripción de un cliente a notificaciones de eventos |
| `ReferenceRate` | Tasa de referencia externa (TBP, LIBOR, etc.) para préstamos variables |

### Enumeraciones Clave

```csharp
// Estado del préstamo
ContractStatus: Approved → Active → Delinquent → Default → PaidOff

// Estado de la línea de crédito
RevolvingCreditStatus: Pending → Active → Frozen → Closed

// Método de amortización
AmortizationMethod: French | German | Flat | American | InterestOnly

// Tipo de tasa
RateType: Fixed | Variable

// Categoría de riesgo (SUGEF 1-05)
RiskCategory: A1 | A2 | B1 | B2 | C1 | C2 | D | E
```

---

## 5. Agregados y Ciclo de Vida

### LoanContractAggregate — Máquina de Estados

```mermaid
stateDiagram-v2
    [*] --> Approved : Create() + ContractApproved event
    Approved --> Active : Disburse() + LoanDisbursed event
    Active --> Active : ApplyPayment() / AccrueInterest()
    Active --> Delinquent : RecordMissedPayment() (N missed)
    Active --> PaidOff : PayoffContract()
    Delinquent --> Active : ApplyPayment() (full catch-up)
    Delinquent --> Default : MarkAsDefault()
    Delinquent --> Active : Restructure()
    Default --> Active : Restructure()
    Active --> PaidOff : ApplyPayment() (balance = 0)
    PaidOff --> [*]
```

**Operaciones disponibles:**

| Método | Pre-condición | Evento emitido |
|---|---|---|
| `Create()` | — | `ContractCreated`, `ContractApproved` |
| `Disburse()` | `Approved` | `LoanDisbursed` |
| `ApplyPayment()` | `Active \| Delinquent` | `PaymentApplied` [+ `ContractPaidOff`] |
| `AccrueInterest()` | `Active \| Delinquent` | `InterestAccrued` |
| `RecordMissedPayment()` | `Active \| Delinquent` | `PaymentMissed` |
| `MarkAsDefault()` | `Delinquent` | `ContractDefaulted` |
| `Restructure()` | `Delinquent \| Default` | `ContractRestructured` |
| `AdjustRate()` | `Active` (variable) | `RateAdjusted` |

**Orden de aplicación de pagos:** Comisiones → Interés acumulado → Principal

---

### RevolvingCreditAggregate — Máquina de Estados

```mermaid
stateDiagram-v2
    [*] --> Pending : Create() + CreditLineCreated event
    Pending --> Active : Activate() + CreditLineActivated event
    Active --> Active : DrawFunds() / ApplyPayment() / AccrueInterest() / GenerateStatement()
    Active --> Frozen : Freeze() + CreditLineFrozen event
    Frozen --> Active : Unfreeze() [manual o auto al recibir pago mínimo]
    Active --> Closed : Close() [balance = 0]
    Frozen --> Closed : Close() [balance = 0]
    Closed --> [*]
```

**Operaciones disponibles:**

| Método | Pre-condición | Evento emitido |
|---|---|---|
| `Create()` | — | `CreditLineCreated` |
| `Activate()` | `Pending` | `CreditLineActivated` |
| `DrawFunds()` | `Active`, crédito disponible | `FundsDrawn` |
| `ApplyPayment()` | `!Closed` | `RevolvingPaymentApplied` [+ `CreditLineUnfrozen`] |
| `AccrueInterest()` | balance > 0 | `RevolvingInterestAccrued` |
| `GenerateStatement()` | `Active \| Frozen` | `StatementGenerated` |
| `Freeze()` | `Active` | `CreditLineFrozen` |
| `Unfreeze()` | `Frozen` | `CreditLineUnfrozen` |
| `ChangeCreditLimit()` | `!Closed` | `CreditLimitChanged` |
| `Close()` | balance = 0 | `CreditLineClosed` |

**Auto-unfreeze**: Al aplicar un pago sobre una línea `Frozen`, si el monto cubre el pago mínimo del período, la línea se descongela automáticamente (`CreditLineUnfrozen` emitido en la misma transacción).

---

## 6. Event Sourcing

### Flujo de Persistencia

```mermaid
sequenceDiagram
    participant Handler as Command Handler
    participant Aggregate as Aggregate
    participant EventStore as PostgresEventStore
    participant Projection as ProjectionEngine
    participant ReadDB as rm_* tables

    Handler->>Aggregate: command.Execute()
    Aggregate->>Aggregate: Apply(event, isNew: true)
    Aggregate->>Aggregate: Accumulate in UncommittedEvents
    Handler->>EventStore: AppendEventsAsync(aggregateId, events, expectedVersion)
    EventStore->>EventStore: Serialize + hash + insert event_store
    EventStore->>EventStore: BEGIN TRANSACTION
    EventStore->>ReadDB: Outbox insert (same tx)
    EventStore->>EventStore: COMMIT
    Handler->>Projection: ProjectAsync(events)
    Projection->>ReadDB: UPSERT rm_loan_summaries / rm_payment_history / ...
```

### Estructura del Event Store

```sql
-- Tabla principal del event store
event_store (
    id           UUID PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    event_type   VARCHAR(200) NOT NULL,
    payload      JSONB NOT NULL,
    version      BIGINT NOT NULL,
    timestamp    TIMESTAMPTZ NOT NULL,
    hash         VARCHAR(64) NOT NULL,        -- SHA-256 del payload
    UNIQUE(aggregate_id, version)
)

-- Snapshots para rehidratación eficiente
event_snapshots (
    aggregate_id  UUID PRIMARY KEY,
    aggregate_type VARCHAR(100) NOT NULL,
    snapshot_data JSONB NOT NULL,
    version       BIGINT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL
)
```

### Rehidratación de Agregados

El repositorio soporta dos estrategias:

1. **Full replay**: Lee todos los eventos desde `version = 0`
2. **Snapshot + delta**: Carga el snapshot más reciente, luego aplica solo los eventos posteriores

```csharp
// Constructor con soporte de snapshot
new LoanContractAggregate(snapshot: LoanContractState?, events: IEnumerable<IDomainEvent>)
new RevolvingCreditAggregate(snapshot: RevolvingCreditState?, events: IEnumerable<IDomainEvent>)
```

---

## 7. CQRS y Pipeline de Mediator

### Flujo de un Command

```mermaid
flowchart LR
    A[HTTP Request] --> B[Minimal API Endpoint]
    B --> C[IMediator.Send]
    C --> D[ValidationBehavior]
    D -->|Valid| E[LoggingBehavior]
    D -->|Invalid| F[ValidationException → 400]
    E --> G[CommandHandler]
    G --> H[Load Aggregate from EventStore]
    H --> I[Aggregate.Method]
    I --> J[AppendEvents to EventStore]
    J --> K[Run Projections]
    K --> L[Response]
```

### Flujo de una Query

```mermaid
flowchart LR
    A[HTTP Request] --> B[Minimal API Endpoint]
    B --> C[IMediator.Send / QueryService direct]
    C --> D[QueryHandler / QueryService]
    D --> E[Dapper SELECT from rm_* tables]
    E --> F[Read Model → Response DTO]
```

### Pipeline Behaviors

| Behavior | Orden | Función |
|---|---|---|
| `ValidationBehavior<TRequest, TResponse>` | 1 | Ejecuta todos los `IValidator<TRequest>` via FluentValidation. Lanza `ValidationException` si hay errores. |
| `LoggingBehavior<TRequest, TResponse>` | 2 | Registra inicio, duración y resultado de cada request via ILogger. |

---

## 8. Motor de Reglas de Suscripción

El `ContractEngine` evalúa si un cliente puede obtener un préstamo antes de crear el contrato. Las reglas se ejecutan en orden de prioridad.

```mermaid
flowchart TD
    A[CreateContractCommand] --> B[ContractEngine.EvaluateAsync]
    B --> C{Hard Stop Rule?}
    C -->|Yes, Failed| D[Return Rejected immediately]
    C -->|No or Passed| E[Continue to next rule]
    E --> F{More rules?}
    F -->|Yes| C
    F -->|No| G[Aggregate rate adjustments]
    G --> H[Return Approved with final rate]
```

### Reglas Implementadas

| Regla | Tipo | Descripción |
|---|---|---|
| `CreditScoreRule` | Hard Stop | Rechaza si score < umbral mínimo |
| `DebtToIncomeRule` | Hard Stop | Rechaza si DTI > umbral máximo |
| `CollateralRule` | Standard | Ajusta tasa según ratio de colateral |
| `MaxLoanAmountRule` | Hard Stop | Rechaza si monto > máximo permitido |
| `ActiveLoansRule` | Hard Stop | Rechaza si cliente tiene préstamos activos en mora |

Las reglas `IHardStopRule` abortan la evaluación inmediatamente al fallar. Las reglas estándar acumulan ajustes de tasa (`+/- basis points`).

La configuración de umbrales se gestiona via **Underwriting Policy** — tabla `underwriting_policies` en PostgreSQL, cargada en memoria con health check (`UnderwritingPolicyHealthCheck`).

---

## 9. Amortización

El sistema soporta 5 métodos de amortización, todos implementando `IAmortizationCalculator`:

| Método | `AmortizationMethod` | Cuota | Descripción |
|---|---|---|---|
| Francés | `French` | Fija | Cuota constante; interés decrece, capital crece. |
| Alemán | `German` | Decreciente | Capital fijo por cuota; interés sobre saldo. |
| Plano | `Flat` | Fija | Interés sobre capital original (no sobre saldo). |
| Americano | `American` | Variable | Solo intereses hasta la última cuota (balloon). |
| Solo Interés | `InterestOnly` | Variable | N cuotas de interés + 1 cuota de capital total. |

### Selección via Factory

```csharp
AmortizationCalculatorFactory.GetCalculator(AmortizationMethod method)
// → IAmortizationCalculator
```

Para agregar un nuevo método: implementar `IAmortizationCalculator` y registrarlo en `AmortizationCalculatorFactory`.

---

## 10. API REST — Endpoints

Todos los endpoints están bajo el prefijo `/api/v1/`. Versioning via URL segment.

### Préstamos (`/loans`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/loans` | Crear contrato (evalúa reglas, calcula tabla) |
| `POST` | `/loans/{id}/disburse` | Desembolsar préstamo aprobado |
| `GET` | `/loans/{id}` | Obtener resumen del préstamo |
| `GET` | `/loans/customer/{externalCustomerId}` | Préstamos por cliente |
| `POST` | `/loans/{id}/payments` | Aplicar pago (con idempotencia) |
| `GET` | `/loans/{id}/payments` | Historial de pagos |
| `GET` | `/loans/{id}/payoff-amount` | Calcular monto de liquidación |
| `POST` | `/loans/{id}/payoff` | Liquidar préstamo completo |
| `POST` | `/loans/{id}/restructure` | Reestructurar préstamo |
| `GET` | `/loans/{id}/restructure-history` | Historial de reestructuraciones |
| `POST` | `/loans/{id}/default` | Marcar como incumplimiento |
| `GET` | `/loans/defaulted` | Préstamos en default |
| `GET` | `/loans/paid-off` | Préstamos liquidados |

### Morosidad (`/delinquent-loans`)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/delinquent-loans` | Listar morosos (filtros: días vencidos, estado cobranza) |
| `GET` | `/delinquent-loans/{id}` | Detalle de préstamo moroso |

### Crédito Revolvente (`/revolving-credits`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/revolving-credits` | Crear línea de crédito |
| `POST` | `/revolving-credits/{id}/activate` | Activar línea pendiente |
| `POST` | `/revolving-credits/{id}/draw` | Disponer fondos |
| `POST` | `/revolving-credits/{id}/payments` | Aplicar pago (con idempotencia) |
| `POST` | `/revolving-credits/{id}/change-limit` | Cambiar límite de crédito |
| `POST` | `/revolving-credits/{id}/freeze` | Congelar línea |
| `POST` | `/revolving-credits/{id}/unfreeze` | Descongelar línea |
| `POST` | `/revolving-credits/{id}/close` | Cerrar línea (balance = 0) |
| `GET` | `/revolving-credits/{id}` | Resumen de la línea |
| `GET` | `/revolving-credits/{id}/transactions` | Transacciones (disposiciones y pagos) |
| `GET` | `/revolving-credits/{id}/statements` | Estados de cuenta generados |
| `GET` | `/revolving-credits/customer/{customerId}` | Líneas por cliente |

### Documentos (`/loans/{id}/documents`)

| Método | Ruta | Content-Type | Descripción |
|---|---|---|---|
| `GET` | `/loans/{id}/documents/payment-receipt/{paymentId}` | `application/pdf` | Comprobante de pago en PDF |
| `GET` | `/loans/{id}/documents/balance-letter` | `application/pdf` | Carta de saldo en PDF |
| `GET` | `/loans/{id}/documents/amortization-table?format=pdf` | `application/pdf` | Tabla de amortización en PDF |
| `GET` | `/loans/{id}/documents/amortization-table?format=xlsx` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | Tabla de amortización en Excel |

### Riesgo (`/loans`)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/loans/risk-summary` | Resumen de cartera por categoría SUGEF 1-05 |
| `GET` | `/loans/risk-summary/{category}` | Detalle de préstamos en una categoría |
| `PUT` | `/loans/{loanId}/risk-category` | Reclasificación manual (solo degradación) |

### Tasas de Referencia (`/reference-rates`)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/reference-rates/{id}` | Obtener valor actual (ej. `TBP_CRC`) |
| `PUT` | `/reference-rates/{id}` | Crear o actualizar tasa de referencia |

### Socios (`/members`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/members` | Registrar socio de la cooperativa |
| `GET` | `/members/{id}` | Obtener perfil del socio |
| `PUT` | `/members/{id}` | Actualizar datos del socio |
| `GET` | `/members` | Listar socios (paginado) |

### Productos (`/products`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/products` | Crear producto crediticio |
| `GET` | `/products` | Listar productos |
| `GET` | `/products/{id}` | Obtener producto |
| `PUT` | `/products/{id}` | Actualizar producto |

### Garantías (`/guarantees`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/loans/{loanId}/guarantees` | Registrar garantía para un préstamo |
| `GET` | `/loans/{loanId}/guarantees` | Listar garantías del préstamo |

### Admin (`/admin`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/admin/jobs/interest-accrual` | Ejecutar acumulación de intereses manualmente |
| `POST` | `/admin/jobs/payment-missed` | Ejecutar detección de pagos vencidos |
| `POST` | `/admin/jobs/revolving-interest-accrual` | Acumulación intereses revolventes |
| `POST` | `/admin/jobs/statement-generation` | Generación de estados de cuenta |
| `POST` | `/admin/jobs/revolving-payment-missed` | Detección pagos vencidos revolventes |
| `POST` | `/admin/loans/{id}/accrue-interest` | Acumular interés a préstamo específico |
| `POST` | `/admin/revolving-credits/{id}/accrue-interest` | Acumular interés a línea específica |

### Webhooks (`/webhooks`)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/webhooks/subscriptions` | Suscribirse a un tipo de evento |
| `GET` | `/webhooks/subscriptions/{customerId}` | Obtener suscripciones de un cliente |
| `DELETE` | `/webhooks/subscriptions/{id}` | Cancelar suscripción |

### Características Transversales

**Idempotencia** (en pagos de préstamos y revolventes):
```http
POST /api/v1/loans/{id}/payments
Idempotency-Key: {uuid}
```
El servidor almacena el resultado en `idempotency_keys` (24h TTL). Respuestas repetidas incluyen el header `Idempotency-Replayed: true`.

**Auditoría**:
```http
POST /api/v1/loans/{id}/payments
X-User-Id: {userId}
```
Las operaciones de escritura registran entradas en `audit_log` con acción, entidad, usuario y detalles.

---

## 11. Sistema de Proyecciones

Después de que el Event Store persiste eventos, el `IProjectionEngine` los distribuye a todos los `IProjection` registrados (scoped por DI).

```mermaid
flowchart TD
    A[EventStore.AppendEventsAsync] --> B[IProjectionEngine.ProjectAsync]
    B --> C{Fan-out}
    C --> D[LoanSummaryProjector]
    C --> E[DelinquentLoansProjector]
    C --> F[PaymentHistoryProjector]
    C --> G[LoanPortfolioProjector]
    C --> H[RevolvingCreditSummaryProjector]
    C --> I[PaymentTrackingProjector]
    D --> J[UPSERT rm_loan_summaries]
    E --> K[UPSERT rm_delinquent_loans]
    F --> L[INSERT rm_payment_history]
    G --> M[UPSERT rm_loan_portfolio]
    H --> N[UPSERT rm_revolving_credit_summaries]
    I --> O[UPDATE rm_payment_tracking]
```

### Read Models y sus Tablas

| Read Model | Tabla PostgreSQL | Proyector |
|---|---|---|
| `LoanSummaryReadModel` | `rm_loan_summaries` | `LoanSummaryProjector` |
| `DelinquentLoanReadModel` | `rm_delinquent_loans` | `DelinquentLoansProjector` |
| `PaymentHistoryReadModel` | `rm_payment_history` | `PaymentHistoryProjector` |
| `LoanPortfolioReadModel` | `rm_loan_portfolio` | `LoanPortfolioProjector` |
| `RevolvingCreditSummaryReadModel` | `rm_revolving_credit_summaries` | `RevolvingCreditSummaryProjector` |
| Payment tracking | `rm_payment_tracking` | `PaymentTrackingProjector` |

---

## 12. Workers en Segundo Plano

Todos los workers son `BackgroundService` con:
- **Distributed locking**: `PostgresDistributedLock` via advisory locks de PostgreSQL. Cada worker tiene un `WorkerLockId` único. Garantiza que solo una instancia procese en sistemas con múltiples réplicas.
- **Configuración**: intervalos y horarios via `appsettings.json` (`Jobs:*:IntervalHours`, `Jobs:*:RunTime`)
- **Reintentos**: en caso de error, espera 5 minutos antes de reintentar

### Resumen de Workers

| Worker | Horario | Función |
|---|---|---|
| `InterestAccrualWorker` | Configurable (default 02:00 UTC) | Acumula interés diario en todos los préstamos `Active` y `Delinquent` |
| `PaymentMissedWorker` | Configurable (default 03:00 UTC) | Detecta cuotas vencidas y llama `RecordMissedPayment()` en los agregados |
| `RevolvingInterestAccrualWorker` | Configurable | Acumula interés en líneas de crédito `Active` y `Frozen` |
| `StatementGenerationWorker` | Configurable | Genera estados de cuenta en la fecha del ciclo de facturación de cada línea |
| `RevolvingPaymentMissedWorker` | Configurable | Detecta pagos mínimos vencidos y congela líneas automáticamente |
| `RiskClassificationWorker` | Configurable | Reclasifica préstamos según categorías SUGEF 1-05 (días vencidos + monto) |
| `RateAdjustmentWorker` | Configurable | Recalcula tasa efectiva de préstamos con tasa variable cuando cambia la tasa de referencia |

### Flujo del InterestAccrualWorker

```mermaid
sequenceDiagram
    participant W as InterestAccrualWorker
    participant L as DistributedLock
    participant J as InterestAccrualJob
    participant R as LoanContractRepository
    participant A as LoanContractAggregate

    W->>W: Wait until RunTime (02:00 UTC)
    W->>L: TryAcquireAsync(WorkerLockId.InterestAccrual)
    L-->>W: acquired = true
    W->>J: ExecuteAsync()
    J->>R: GetAllActiveAsync()
    loop Por cada préstamo activo
        J->>R: GetByIdAsync(loanId)
        R-->>J: LoanContractAggregate (rehidratado)
        J->>A: AccrueInterest(periodStart, periodEnd)
        J->>R: SaveAsync(aggregate)
    end
    W->>L: ReleaseAsync(WorkerLockId.InterestAccrual)
```

---

## 13. Mensajería y Patrón Outbox

El sistema usa **MassTransit** sobre **RabbitMQ** para integración con sistemas externos (principalmente el servicio de clientes).

### Patrón Outbox

Los mensajes salientes se insertan en la misma transacción que el evento de negocio, garantizando consistencia:

```mermaid
sequenceDiagram
    participant Handler as Command Handler
    participant DB as PostgreSQL
    participant Outbox as outbox_messages
    participant Worker as OutboxPublisherWorker
    participant MQ as RabbitMQ

    Handler->>DB: BEGIN TRANSACTION
    Handler->>DB: INSERT INTO event_store
    Handler->>Outbox: INSERT INTO outbox_messages (status=PENDING)
    Handler->>DB: COMMIT
    Worker->>Outbox: SELECT WHERE status=PENDING (poll cada 10s)
    Worker->>MQ: Publish message
    Worker->>Outbox: UPDATE status=PUBLISHED
```

### Colas y Consumidores

| Cola | Consumidores | Propósito |
|---|---|---|
| `credit-service-customer-events` | `CustomerCreatedConsumer`, `CustomerUpdatedConsumer` | Sincroniza referencias de clientes desde el servicio externo |
| `credit-service-payments` | `ProcessPaymentConsumer`, `ProcessRevolvingPaymentConsumer` | Procesa pagos recibidos externamente (async). Incluye política de reintentos. |

### Mensajes del SharedKernel

```csharp
// Mensajes recibidos (desde servicio de clientes)
CustomerCreated { CustomerId, ExternalId, Name, CreditScore, ... }
CustomerUpdated { CustomerId, Name, CreditScore, ... }

// Mensajes de pago async
ProcessPaymentMessage { LoanId, PaymentId, Amount, Currency, Method }
ProcessRevolvingPaymentMessage { CreditLineId, PaymentId, Amount, Currency, Method }
```

---

## 14. Webhooks

Los clientes pueden registrar URLs para recibir notificaciones push cuando ocurren eventos de pago.

```mermaid
sequenceDiagram
    participant Client as Cliente API
    participant API as Credit System API
    participant DB as PostgreSQL
    participant Worker as WebhookDeliveryWorker
    participant CB as Callback URL (cliente)

    Client->>API: POST /webhooks/subscriptions
    API->>DB: INSERT webhook_subscriptions (+ secret_key)

    note over DB: Evento de pago ocurre

    API->>DB: INSERT webhook_deliveries (status=PENDING)
    Worker->>DB: SELECT pending deliveries
    Worker->>Worker: Firma payload con HMAC-SHA256 usando secret_key
    Worker->>CB: POST {payload}
    CB-->>Worker: 200 OK
    Worker->>DB: UPDATE status=DELIVERED

    note over Worker: En caso de fallo: exponential backoff + retry
```

**Seguridad**: Cada suscripción tiene un `secret_key`. El payload del webhook se firma con HMAC-SHA256. El receptor debe verificar el header `X-Signature`.

**Tipos de evento soportados**: `payment.applied`, `payment.missed`, `loan.disbursed`, `loan.paid_off`, `loan.defaulted`

---

## 15. Motor de Documentos

Sistema de generación de documentos PDF y Excel para productos crediticios.

### Arquitectura del Motor

```mermaid
flowchart LR
    subgraph Application
        Q1[GetPaymentReceiptQuery]
        Q2[GetBalanceLetterQuery]
        Q3[GetAmortizationTableQuery]
    end

    subgraph Infrastructure["Infrastructure — DocumentGenerator"]
        TE[ScribanTemplateEngine<br/>.sbn templates → string]
        PDF[QuestPdfRenderer<br/>string → PDF bytes]
        XLS[ExcelExporter<br/>AmortizationTableData → xlsx bytes]
    end

    subgraph Templates
        T1[payment-receipt.sbn]
        T2[balance-letter.sbn]
        T3[amortization-table.sbn]
    end

    Q1 -->|PaymentReceiptData + Pdf| TE
    Q2 -->|BalanceLetterData + Pdf| TE
    Q3 -->|AmortizationTableData + Pdf| TE
    Q3 -->|AmortizationTableData + Excel| XLS
    TE --> T1 & T2 & T3
    TE --> PDF
    PDF --> |byte[]| Q1 & Q2 & Q3
    XLS --> |byte[]| Q3
```

### Componentes

| Componente | Biblioteca | Función |
|---|---|---|
| `ScribanTemplateEngine` | Scriban 5.12.0 | Lee `.sbn` desde disco (output dir), expone datos como `model`, renderiza a string |
| `QuestPdfRenderer` | QuestPDF 2026.7.2 | Construye PDF con header (cooperativa + título), body (contenido), footer (fecha + página N/M) |
| `ExcelExporter` | ClosedXML 0.105.1 | Genera `.xlsx` con encabezados, datos de tabla de amortización, fila de totales |
| `DocumentGenerator` | — | Orquesta: para PDF usa Scriban+QuestPDF; para Excel solo acepta `AmortizationTableData` |

### Data Contracts de Dominio

```csharp
PaymentReceiptData   { LoanId, LoanNumber, CustomerName, Currency, PaymentDate,
                       PrincipalApplied, InterestApplied, FeesApplied?, TotalPaid, RemainingBalance }

BalanceLetterData    { LoanId, LoanNumber, CustomerName, Currency, DisbursementDate,
                       OriginalAmount, CurrentBalance, InterestRate, RateType, Spread?,
                       ReferenceRateId?, NextPaymentDate, NextPaymentAmount, MaturityDate,
                       Status, IssuedAt }

AmortizationTableData { LoanId, LoanNumber, CustomerName, Currency, OriginalAmount,
                        InterestRate, TermMonths, GeneratedAt,
                        Rows: IReadOnlyList<AmortizationRowData>,
                        TotalPayment, TotalInterest, TotalPrincipal }
```

### Templates Scriban (`.sbn`)

Los templates se almacenan en `Infrastructure/Documents/Templates/` y se copian al directorio de salida (`CopyToOutputDirectory: Always`).

| Template | Documento |
|---|---|
| `payment-receipt.sbn` | Comprobante con desglose de capital/interés/comisiones. Omite comisiones si son 0. |
| `balance-letter.sbn` | Carta formal con datos del préstamo, tasa, próximo pago. Sección condicional para tasa variable. Aviso de vigencia (30 días). |
| `amortization-table.sbn` | Tabla iterando `model.rows` con N°, fecha, cuota, interés, capital, saldo. Fila de totales. |

---

## 16. Tasas Variables y Reajuste

Los préstamos pueden ser de tasa fija o tasa variable. La tasa variable se expresa como:

```
Tasa Efectiva = Tasa de Referencia + Spread
```

### Componentes

- **`ReferenceRate`** (entidad): almacenada en `reference_rates`. Actualizable via `PUT /reference-rates/{id}`.
- **`RateAdjustmentWorker`**: detecta cambios en tasas de referencia y aplica `AdjustRate()` en los agregados afectados, emitiendo `RateAdjusted`.
- **`InterestRate` value object**: encapsula `RateType`, `Spread`, `ReferenceRateId`, y `AnnualRate` (calculada).

### Flujo de Reajuste

```mermaid
sequenceDiagram
    participant Admin as Administrador
    participant API as PUT /reference-rates/TBP_CRC
    participant DB as reference_rates
    participant Worker as RateAdjustmentWorker
    participant Repo as LoanContractRepository
    participant Agg as LoanContractAggregate

    Admin->>API: { currentValue: 5.25, source: "BCCR" }
    API->>DB: UPSERT reference_rates
    Worker->>DB: Detecta cambio (last_adjusted_at)
    Worker->>Repo: GetAllVariableRateLoans(referenceRateId)
    loop Por cada préstamo variable
        Worker->>Repo: GetByIdAsync(loanId)
        Worker->>Agg: AdjustRate(newEffectiveRate)
        Worker->>Repo: SaveAsync(aggregate)
    end
```

---

## 17. Clasificación de Riesgo (SUGEF 1-05)

El sistema implementa la clasificación de cartera conforme a la normativa SUGEF 1-05 de Costa Rica.

### Categorías de Riesgo

| Categoría | Días Vencidos | Provisión Estimada |
|---|---|---|
| A1 | 0 | 0% |
| A2 | 1–30 | 0.5% |
| B1 | 31–60 | 1% |
| B2 | 61–90 | 5% |
| C1 | 91–120 | 25% |
| C2 | 121–150 | 50% |
| D | 151–180 | 75% |
| E | > 180 | 100% |

### `RiskClassificationWorker`

Ejecuta periódicamente y:
1. Consulta todos los préstamos activos en `rm_loan_summaries`
2. Calcula días vencidos basado en `days_overdue`
3. Determina categoría SUGEF
4. Actualiza `risk_category` y `estimated_provision` en `rm_loan_summaries`
5. Respeta la regla de **no mejora automática**: solo degrada vía worker; la mejora requiere acción manual o criterio adicional

**Reclasificación manual**: `PUT /loans/{id}/risk-category` permite degradar manualmente (nunca mejorar automáticamente).

---

## 18. Infraestructura de Producción

### Stack Tecnológico

| Componente | Tecnología | Versión |
|---|---|---|
| Runtime | .NET | 9 |
| Base de datos | PostgreSQL | 14+ |
| Message broker | RabbitMQ | 3.x |
| Mensajería | MassTransit | — |
| ORM | Dapper + Npgsql | — |
| Validación | FluentValidation | — |
| Mapeo | Mapster | — |
| Mediator | MediatR | — |
| PDF | QuestPDF | 2026.7.2 (Community) |
| Templates | Scriban | 5.12.0 |
| Excel | ClosedXML | 0.105.1 |
| Telemetría | OpenTelemetry | — |
| Logging | Serilog + Seq | — |
| API Explorer | Swashbuckle | — |

### Configuración (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "CreditDb": "Host=...;Database=credit_db;Username=...;Password=..."
  },
  "RabbitMqSettings": {
    "Uri": "amqp://user:pass@rabbitmq:5672"
  },
  "Telemetry": {
    "OtlpEndpoint": "http://otel-collector:4317"
  },
  "Jobs": {
    "InterestAccrual": { "IntervalHours": 1, "RunTime": "02:00:00" },
    "PaymentMissed":   { "IntervalHours": 1, "RunTime": "03:00:00" },
    "RateAdjustment":  { "IntervalHours": 6 }
  }
}
```

### Health Checks

| Endpoint | Tipo | Verifica |
|---|---|---|
| `GET /health` | Liveness | Siempre 200 (proceso vivo) |
| `GET /health/ready` | Readiness | PostgreSQL + RabbitMQ + UnderwritingPolicy |

### Seguridad

- Distributed locking: advisory locks de PostgreSQL (sin infraestructura adicional de locking)
- No hay autenticación en la API actualmente (pendiente implementación)
- Webhooks firmados con HMAC-SHA256
- Idempotencia en pagos con TTL de 24h

---

## 19. Base de Datos — Esquema

### Event Sourcing

```
event_store          — Eventos de dominio (append-only)
event_snapshots      — Snapshots de estado de agregados
```

### Read Models (tablas `rm_*`)

```
rm_loan_summaries            — Resumen de préstamos (estado, saldos, tasas, riesgo)
rm_delinquent_loans          — Préstamos en mora con datos de cobranza
rm_payment_history           — Historial de pagos por préstamo
rm_loan_portfolio            — Métricas agregadas del portafolio
rm_revolving_credit_summaries — Resumen de líneas de crédito revolvente
rm_payment_tracking          — Estado de procesamiento de pagos async
```

### Operaciones

```
customer_references          — Referencias de clientes sincronizadas via mensajería
underwriting_policies        — Configuración de reglas de suscripción (umbrales)
reference_rates              — Tasas de referencia externas (TBP, LIBOR, etc.)
```

### Infraestructura

```
outbox_messages              — Mensajes pendientes de publicar a RabbitMQ
idempotency_keys             — Claves de idempotencia para pagos (TTL 24h)
audit_log                    — Log de auditoría de operaciones sensibles
webhook_subscriptions        — Suscripciones de clientes a webhooks
webhook_deliveries           — Intentos de entrega de webhooks
```

### Tablas de Socios/Productos

```
cooperative_members          — Socios de la cooperativa
credit_products              — Catálogo de productos crediticios
loan_guarantees              — Garantías asociadas a préstamos
```

### Migraciones

Las migraciones son SQL puro (sin EF Core). Formato: `YYYYMMDD_NombreMigracion.sql`. Se aplican manualmente o via pipeline de CI/CD.

```
20240315_AddAsyncPaymentTables.sql
20260617_AddFinancialFieldsToCustomerReferences.sql
20260624_AddUnderwritingPoliciesTable.sql
20260624_CustomerCreditProfileRename.sql
20260808_AddCooperativeMembersTable.sql
20260808_AddCreditProductsTable.sql
20260808_AddLoanGuaranteesTable.sql
20260809_AddMoraYComisionesColumns.sql
20260809_AddRiskCategoryColumns.sql
20260810_AddIdempotencyKeysTable.sql
20260810_AddAuditLogTable.sql
20260810_AddReferenceRatesTable.sql
20260810_AddRateTypeColumnsToLoanSummary.sql
```

---

## 20. Telemetría y Observabilidad

### Configuración (`SmartCore.Telemetry`)

```csharp
builder.Services.AddSmartCoreTelemetry(options => {
    options.ServiceName    = "credit-system-service";
    options.Version        = "1.0.0";
    options.OtlpEndpoint   = "http://otel-collector:4317";
    options.EnableMassTransit = true;  // traces de mensajería
    options.SamplerRatio   = 1.0;       // 100% sampling
});
```

### Qué se instrumenta

| Componente | Instrumentación |
|---|---|
| ASP.NET Core | Traces de requests HTTP (duración, status, ruta) |
| Dapper/Npgsql | Traces de queries SQL |
| MassTransit | Traces de publicación y consumo de mensajes |
| LoggingBehavior | Logs de cada command/query con duración |
| Serilog | Logs estructurados en JSON, sink a Seq |

### Métricas clave para monitorear

- Latencia de endpoints (P50, P95, P99)
- Tasa de errores por endpoint
- Duración de workers (especialmente `InterestAccrualWorker`)
- Queue depth de RabbitMQ (`credit-service-payments`)
- Outbox lag (`outbox_messages WHERE status='PENDING'`)
- Webhook delivery failure rate

---

## 21. Testing

### Estrategia

El proyecto tiene tests unitarios en `CreditSystem.Tests` (xUnit).

**Herramientas:**
- `xUnit` — framework de tests
- `FluentAssertions` — assertions expresivas
- `NSubstitute` — mocking

### Cobertura por Área

| Área | Tests | Descripción |
|---|---|---|
| Amortización | Calculadoras French, German, Flat, American, InterestOnly | Verifica cuotas, totales, saldos |
| Motor de reglas | Cada `IContractRule` | Aprobación, rechazo, ajuste de tasa |
| Aggregate LoanContract | Creación, pago, interés, reestructura, default | Ciclo de vida completo |
| Aggregate RevolvingCredit | Creación, disposición, pagos, freeze/unfreeze | Ciclo de vida completo |
| Motor de documentos | `ScribanTemplateEngine`, handlers de queries | Templates inline, null cases, NotSupportedException |
| Query handlers | GetPaymentReceipt, GetBalanceLetter, GetAmortizationTable | Mapeo de datos, casos null |
| Projecciones | LoanSummaryProjector, PaymentHistoryProjector | Correctitud del UPSERT |

**Total de tests: 271 (todos passing)**

### Comandos

```bash
# Ejecutar todos los tests
dotnet test CreditBackend.sln

# Tests de un proyecto específico
dotnet test Src/Core/CreditSystem.Tests/CreditSystem.Tests.csproj

# Con output detallado
dotnet test CreditBackend.sln --verbosity normal
```

### Patrones de Test

```csharp
// Patrón Arrange-Act-Assert con NSubstitute
var repository = Substitute.For<ILoanContractRepository>();
repository.GetByIdAsync(id, default).ReturnsForAnyArgs(aggregate);

// Assert con FluentAssertions
result.Should().NotBeNull();
result.Should().BeEquivalentTo(expected);
await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*AmortizationTableData*");

// Verificar llamadas a mocks
await documentGenerator.ReceivedWithAnyArgs(1)
    .GenerateAsync(default!, default(AmortizationTableData)!, default, default);
```

---

## Apéndice — Flujo Completo: Creación y Vida de un Préstamo

```mermaid
sequenceDiagram
    participant Client as Cliente API
    participant API as POST /api/v1/loans
    participant Engine as ContractEngine
    participant Agg as LoanContractAggregate
    participant ES as EventStore
    participant Proj as ProjectionEngine
    participant Worker as Workers (nightly)

    Client->>API: CreateContractCommand { customerId, amount, rate, term }
    API->>Engine: EvaluateAsync(customerRef, loanParams)
    Engine-->>API: Approved { finalRate }
    API->>Agg: LoanContractAggregate.Create(...)
    Note over Agg: Emite ContractCreated + ContractApproved
    API->>ES: AppendEventsAsync(loanId, events)
    API->>Proj: ProjectAsync(events)
    Proj->>Proj: UPSERT rm_loan_summaries (status=Approved)
    API-->>Client: 201 Created { contractId }

    Client->>API: POST /loans/{id}/disburse
    API->>Agg: Disburse(method, account)
    Note over Agg: Emite LoanDisbursed
    API->>ES: AppendEventsAsync
    API->>Proj: UPSERT rm_loan_summaries (status=Active)
    API-->>Client: 200 OK

    loop Cada noche (02:00 UTC)
        Worker->>Agg: AccrueInterest(yesterday, today)
        Note over Agg: Emite InterestAccrued
        Worker->>ES: AppendEventsAsync
        Worker->>Proj: Update rm_loan_summaries.accrued_interest
    end

    Client->>API: POST /loans/{id}/payments { amount }
    API->>Agg: ApplyPayment(paymentId, amount, method)
    Note over Agg: Emite PaymentApplied [+ ContractPaidOff si balance=0]
    API->>ES: AppendEventsAsync
    API->>Proj: INSERT rm_payment_history + UPSERT rm_loan_summaries
    API-->>Client: 200 OK { principalPaid, interestPaid, remainingBalance }
```

---

*Documentación generada el 2026-08-10. Para proponer cambios al sistema, usar el flujo OpenSpec (`/opsx:propose`).*
