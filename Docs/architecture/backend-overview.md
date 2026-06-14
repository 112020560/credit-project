# Backend Architecture Overview

## Stack

| Capa | Tecnología |
|------|-----------|
| Runtime | .NET 9 |
| API | ASP.NET Core Minimal APIs |
| Persistencia | Dapper + Npgsql (PostgreSQL) — sin EF Core |
| CQRS / Mediator | MediatR 13 |
| Validación | FluentValidation 11 |
| Mapeo de objetos | Mapster 7 |
| Mensajería | MassTransit 8 sobre RabbitMQ |
| Observabilidad | OpenTelemetry + Serilog (Seq) |
| Documentación API | Swashbuckle / Swagger |

---

## Estructura de proyectos

```
Src/
├── Core/
│   ├── CreditSystem.Domain          — Modelo de dominio: agregados, eventos, VO, motor de reglas, read models, abstracciones
│   ├── CreditSystem.Application     — Casos de uso: comandos, queries, handlers, behaviors, jobs
│   ├── CreditSystem.Infrastructure  — Event store, projection store, repos, workers, mensajería, webhooks
│   ├── CreditSystem.Api             — Endpoints HTTP, Program.cs, manejo global de excepciones
│   └── CreditSystem.Tests           — Tests unitarios e integración
└── Shared/
    ├── SharedKernel                 — Contratos de integración (eventos de integración, mensajes de pago)
    └── SmartCore.Telemetry          — Configuración centralizada de OpenTelemetry y Serilog
```

**Dependencias entre capas:**

```
CreditSystem.Api → CreditSystem.Application + CreditSystem.Infrastructure + SmartCore.Telemetry
CreditSystem.Application → CreditSystem.Domain
CreditSystem.Infrastructure → CreditSystem.Domain + CreditSystem.Application + SharedKernel
CreditSystem.Domain (sin dependencias de frameworks externos)
```

---

## Clean Architecture + DDD + Event Sourcing

### Agregados

El sistema tiene dos agregados de dominio con Event Sourcing puro. El estado de cada agregado es un `record` inmutable (`*State`) actualizado mediante una función pura `ApplyEvent`. Los eventos se acumulan en `UncommittedEvents` y se persisten en el event store al completar el comando.

#### `LoanContractAggregate` (`CreditSystem.Domain/Aggregates/LoanContract/`)

Gestiona el ciclo de vida completo de un préstamo a plazo fijo.

**Máquina de estados:**

```
Approved → Active ──────→ Delinquent ──→ Default
                 └──────→ PaidOff        └──→ PaidOff (via Restructure → Active)
```

**Operaciones:** `Create()` (factory), `Disburse()`, `ApplyPayment()`, `AccrueInterest()`, `RecordMissedPayment()`, `MarkAsDefault()`, `Restructure()`.

**Eventos de dominio:**
- `ContractCreated`, `LoanDisbursed`, `PaymentApplied`, `InterestAccrued`
- `PaymentMissed`, `ContractDefaulted`, `ContractRestructured`, `ContractPaidOff`

**Auto-default:** al registrar un pago perdido con `daysOverdue >= 90`, el agregado lanza automáticamente `ContractDefaulted`.

#### `RevolvingCreditAggregate` (`CreditSystem.Domain/Aggregates/RevolvingCredit/`)

Gestiona líneas de crédito revolventes (tipo tarjeta de crédito).

**Máquina de estados:**

```
Pending → Active ⇄ Frozen → Closed
```

**Operaciones:** `Create()`, `Activate()`, `DrawFunds()`, `ApplyPayment()`, `AccrueInterest()`, `GenerateStatement()`, `Freeze()`, `Unfreeze()`, `ChangeCreditLimit()`, `Close()`.

**Eventos de dominio:**
- `CreditLineCreated`, `CreditLineActivated`, `FundsDrawn`
- `RevolvingPaymentApplied`, `RevolvingInterestAccrued`, `StatementGenerated`
- `CreditLineFrozen`, `CreditLineUnfrozen`, `CreditLimitChanged`, `CreditLineClosed`

**Auto-unfreeze:** si la línea está `Frozen` y el pago cubre el mínimo, se aplica `CreditLineUnfrozen` automáticamente.

### Value Objects (`CreditSystem.Domain/ValueObjects/`)

| Value Object | Descripción |
|---|---|
| `Money` | Monto + moneda. Previene mezcla de divisas. Soporta operadores `+`, `-`, `>`. |
| `InterestRate` | Tasa de interés. Expone `CalculateDailyInterest(Money balance)`. |
| `AmortizationEntry` | Una cuota individual: número, fecha de vencimiento, capital, interés, saldo. |
| `PaymentSchedule` | Lista de `AmortizationEntry`. Tiene `Calculate()` estático para amortización francesa básica. |

### Entidades (`CreditSystem.Domain/Entities/`)

| Entidad | Descripción |
|---|---|
| `CustomerReference` | Referencia local al cliente (sincronizado desde CRM). |
| `OutboxMessage` | Mensaje en espera de publicación asíncrona (patrón Outbox). |
| `WebhookSubscription` | Suscripción a eventos via webhook HTTP. |

### Read Models (`CreditSystem.Domain/Models/ReadModels/`)

Proyecciones desnormalizadas para queries rápidas sin rehidratación de eventos:
`LoanSummaryReadModel`, `DelinquentLoanReadModel`, `PaymentHistoryReadModel`, `LoanPortfolioReadModel`, `ActiveLoanForAccrual`, `OverdueLoanInfo`, `DefaultedLoanReadModel`, `PaidOffLoanReadModel`, `CustomerLoansReadModel`, `UpcomingPaymentReadModel`, `RevolvingCreditSummaryReadModel`, `RevolvingTransactionReadModel`, `RevolvingStatementReadModel`, `PaymentTrackingReadModel`.

---

## Motor de Reglas de Crédito

`ContractEngine` (`CreditSystem.Domain/Rules/`) evalúa un conjunto priorizado de reglas antes de crear un contrato. Puede aprobar o rechazar la solicitud y ajustar la tasa de interés.

**Reglas disponibles:**

| Regla | Tipo | Acción |
|---|---|---|
| `CreditScoreRule` | Hard stop | Rechaza si el score es insuficiente |
| `DebtToIncomeRule` | Hard stop | Rechaza si DTI supera el umbral |
| `CollateralRule` | Soft | Ajusta tasa si hay colateral |
| `MaxLoanAmountRule` | Hard stop | Rechaza si el monto supera el máximo permitido |
| `ActiveLoansRule` | Soft | Penaliza tasa por préstamos activos existentes |

**Flujo:** reglas ordenadas por `Priority` → si falla un `IHardStopRule`, se detiene la evaluación → la tasa final = `BaseRate (8%)` + suma de ajustes de reglas.

---

## Motor de Amortización

Vive en `CreditSystem.Domain/Services/Amortization/`. Usa el patrón Factory:

```
AmortizationCalculatorFactory (IAmortizationCalculatorFactory)
    ├── FrenchAmortizationCalculator    (cuotas iguales, sistema francés)
    ├── GermanAmortizationCalculator    (capital constante)
    ├── FlatAmortizationCalculator      (interés plano sobre capital original)
    ├── AmericanAmortizationCalculator  (bullet / solo interés + capital al final)
    └── InterestOnlyAmortizationCalculator
```

La calculadora se selecciona por `AmortizationMethod` (enum) al crear el `LoanContractAggregate`. El resultado es un `PaymentSchedule` con la lista de `AmortizationEntry`.

Para agregar un método: implementar `IAmortizationCalculator` y registrarlo en `AmortizationCalculatorFactory`.

---

## CQRS

Todos los casos de uso están en `CreditSystem.Application` y siguen CQRS con MediatR.

**Pipeline behaviors (en orden):**
1. `ValidationBehavior` — ejecuta los validators de FluentValidation y lanza excepción si falla
2. `LoggingBehavior` — registra duración y resultado del handler

**Handlers de comandos:**

| Área | Comando |
|---|---|
| Préstamo | `CreateContractCommand`, `DisburseLoanCommand`, `ApplyPaymentCommand`, `DefaultContractCommand`, `RestructureContractCommand`, `PayoffContractCommand` |
| Crédito revolvente | `CreateCreditLineCommand`, `ActivateCreditLineCommand`, `DrawFundsCommand`, `ApplyRevolvingPaymentCommand`, `FreezeCreditLineCommand`, `UnfreezeCreditLineCommand`, `ChangeCreditLimitCommand`, `CloseCreditLineCommand` |

**Handlers de queries:**

| Query | Resultado |
|---|---|
| `GetLoanSummaryQuery` | `LoanSummaryResponse` |
| `GetPaymentHistoryQuery` | `IList<PaymentHistoryResponse>` |
| `GetDelinquentLoansQuery` | `IList<DelinquentLoanResponse>` |
| `GetDefaultedLoansQuery` | `IList<DefaultedLoanResponse>` |
| `GetRestructureHistoryQuery` | `IList<RestructureHistoryResponse>` |
| `GetPayoffAmountQuery` | `PayoffAmountResponse` |
| `GetPaidOffLoansQuery` | `IList<PaidOffLoanResponse>` |
| `GetRevolvingCreditSummaryQuery` | `RevolvingCreditSummaryResponse` |
| `GetRevolvingTransactionsQuery` | `IList<RevolvingTransactionResponse>` |

---

## Endpoints HTTP

Sin versionado de URL. Grupos registrados como métodos de extensión estáticos en `Program.cs`:

### `/api/loans` — Loan Contracts

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/loans` | Crear contrato (evalúa reglas + calcula amortización) |
| POST | `/api/loans/{id}/disburse` | Desembolsar préstamo aprobado |
| GET | `/api/loans/{id}` | Resumen del préstamo |
| GET | `/api/loans/customer/{externalCustomerId}` | Préstamos de un cliente |
| POST | `/api/loans/{id}/payments` | Aplicar pago |
| GET | `/api/loans/{id}/payments` | Historial de pagos |
| POST | `/api/loans/{id}/default` | Marcar como default |
| GET | `/api/loans/defaulted` | Listar préstamos en default |
| POST | `/api/loans/{id}/restructure` | Reestructurar préstamo |
| GET | `/api/loans/{id}/restructure-history` | Historial de reestructuraciones |
| GET | `/api/loans/{id}/payoff-amount` | Monto de payoff |
| POST | `/api/loans/{id}/payoff` | Pagar préstamo completo |
| GET | `/api/loans/paid-off` | Listar préstamos pagados |

### `/api/delinquent-loans` — Préstamos morosos

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/delinquent-loans` | Listar morosos (filtros: `minDaysOverdue`, `collectionStatus`) |
| GET | `/api/delinquent-loans/{id}` | Detalle de préstamo moroso |

### `/api/revolving-credits` — Crédito Revolvente

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/revolving-credits` | Crear línea de crédito |
| POST | `/api/revolving-credits/{id}/activate` | Activar línea |
| POST | `/api/revolving-credits/{id}/draw` | Disponer fondos |
| POST | `/api/revolving-credits/{id}/payments` | Aplicar pago |
| POST | `/api/revolving-credits/{id}/freeze` | Congelar línea |
| POST | `/api/revolving-credits/{id}/unfreeze` | Descongelar línea |
| POST | `/api/revolving-credits/{id}/change-limit` | Cambiar límite de crédito |
| POST | `/api/revolving-credits/{id}/close` | Cerrar línea |
| GET | `/api/revolving-credits/{id}` | Resumen de la línea |
| GET | `/api/revolving-credits/{id}/transactions` | Transacciones de la línea |

### `/api/admin` — Administración

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/admin/jobs/interest-accrual` | Disparar acumulación de interés manualmente |
| POST | `/api/admin/loans/{id}/accrue-interest` | Acumular interés a un préstamo específico |
| POST | `/api/admin/jobs/payment-missed` | Disparar detección de pagos perdidos |
| POST | `/api/admin/jobs/revolving-interest-accrual` | Disparar acumulación de interés revolvente |

---

## Infraestructura

### Event Store

`PostgresEventStore` persiste eventos en PostgreSQL usando Dapper. Modelos de datos:
- `EventStream` — stream por agregado (id + tipo + versión)
- `StoredEvent` — evento individual serializado como JSON
- `EventSnapshot` — snapshot del estado para optimizar rehidratación
- `EventMetadata` — metadatos de correlación

Serialización: `JsonEventSerializer`. Integridad: `Sha256HashGenerator`.

### Sistema de Proyecciones

Tras guardar eventos, `ProjectionEngine` ejecuta todos los `IProjection` registrados:

| Projector | Read Model actualizado |
|---|---|
| `LoanSummaryProjector` | `loan_summaries` |
| `DelinquentLoansProjector` | `delinquent_loans` |
| `PaymentHistoryProjector` | `payment_history` |
| `LoanPortfolioProjector` | `loan_portfolio` |
| `RevolvingCreditSummaryProjector` | `revolving_credit_summaries` |
| `PaymentTrackingProjector` | `payment_tracking` |

`PostgresProjectionStore` (Dapper) gestiona el upsert de las proyecciones.

### Background Workers

| Worker | Función |
|---|---|
| `InterestAccrualWorker` | Acumula interés diario en préstamos activos |
| `PaymentMissedWorker` | Detecta cuotas vencidas y registra `PaymentMissed` |
| `RevolvingInterestAccrualWorker` | Acumula interés en líneas revolventes activas |
| `StatementGenerationWorker` | Genera estados de cuenta revolventes en la fecha de ciclo |
| `RevolvingPaymentMissedWorker` | Detecta pagos mínimos no realizados en crédito revolvente |
| `OutboxPublisherWorker` | Publica mensajes pendientes del outbox vía MassTransit |
| `WebhookDeliveryWorker` | Entrega notificaciones webhook a suscriptores HTTP |

### Mensajería (MassTransit / RabbitMQ)

**Consumers (mensajes recibidos):**

| Cola | Consumer | Acción |
|---|---|---|
| `credit-service-customer-events` | `CustomerCreatedConsumer` | Sincroniza cliente desde CRM |
| `credit-service-customer-events` | `CustomerUpdatedConsumer` | Actualiza cliente desde CRM |
| `credit-service-payments` | `ProcessPaymentConsumer` | Procesa pagos de préstamos de forma asíncrona |
| `credit-service-payments` | `ProcessRevolvingPaymentConsumer` | Procesa pagos revolventes de forma asíncrona |

La cola de pagos tiene retry automático: 5s → 15s → 30s.

**Contratos compartidos (`SharedKernel/Contracts/`):** `CustomerCreated`, `CustomerUpdated`, mensajes de pago (`PaymentMessages`).

### Webhooks

El sistema permite suscripciones a eventos de dominio vía HTTP callbacks:
- `WebhookSubscriptionRepository` — gestiona suscripciones
- `WebhookDeliveryRepository` — registra intentos de entrega
- `WebhookNotifier` — crea los registros de entrega pendientes
- `WebhookDeliveryWorker` — envía las notificaciones con `HttpClient` (timeout 30s)

### Observabilidad

`SmartCore.Telemetry` configura en un solo lugar:
- **Trazas:** ASP.NET Core, HTTP client, MassTransit
- **Logs:** Serilog con sink a Seq y OTLP
- **Configuración:** `Telemetry:OtlpEndpoint` en `appsettings.json`

---

## Flujo típico: Crear y desembolsar un préstamo

```
POST /api/loans
        │ CreateContractCommand (MediatR)
        │
        ├── ValidationBehavior (FluentValidation)
        ├── ContractEngine evalúa reglas (CreditScore, DTI, Collateral, etc.)
        ├── AmortizationCalculatorFactory selecciona calculadora por AmortizationMethod
        ├── LoanContractAggregate.Create() → evento ContractCreated
        ├── PostgresEventStore.AppendAsync() → persiste eventos
        ├── ProjectionEngine → actualiza loan_summaries, loan_portfolio
        └── Responde 201 Created con ContractId

POST /api/loans/{id}/disburse
        │ DisburseLoanCommand
        │
        ├── LoanContractRepository.LoadAsync() → rehidrata agregado desde eventos
        ├── aggregate.Disburse() → evento LoanDisbursed
        ├── PostgresEventStore.AppendAsync() → persiste
        └── ProjectionEngine → actualiza loan_summaries (status: Active)
```

## Flujo típico: Aplicar un pago

```
POST /api/loans/{id}/payments
        │ ApplyPaymentCommand
        │
        ├── Rehidrata LoanContractAggregate
        ├── aggregate.ApplyPayment() — aplica en orden: fees → interés → principal
        │       └── Si balance = 0: emite ContractPaidOff automáticamente
        ├── Persiste eventos
        └── Proyecciones actualizadas (payment_history, loan_summaries)
```

## Flujo típico: Worker de interés

```
InterestAccrualWorker (IHostedService, periódico)
        │
        ├── ILoanQueryService.GetActiveLoansForAccrualAsync()
        ├── Por cada préstamo activo:
        │   ├── Rehidrata LoanContractAggregate
        │   ├── aggregate.AccrueInterest(periodStart, periodEnd)
        │   └── Persiste evento InterestAccrued
        └── ProjectionEngine → actualiza loan_summaries
```
