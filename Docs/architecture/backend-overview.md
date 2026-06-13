# Backend Architecture Overview

## Stack

| Capa | Tecnología |
|------|-----------|
| Runtime | .NET 9 |
| API | ASP.NET Core Minimal APIs |
| ORM | Entity Framework Core 9 + Npgsql (PostgreSQL) |
| CQRS / Mediator | MediatR 13 |
| Mensajería | MassTransit 8 sobre RabbitMQ |
| Observabilidad | OpenTelemetry + Serilog |
| Versionado de API | Asp.Versioning (URL segment) |

---

## Estructura de proyectos

```
Src/
├── Core/
│   ├── Credit.Domain          — Modelo de dominio (entidades, agregados, servicios, eventos)
│   ├── Credit.Application     — Casos de uso: comandos, queries y sus handlers
│   ├── Credit.Infrastructure  — Persistencia (EF Core), mensajería, repositorios
│   └── Credit.WebApi          — Endpoints HTTP, configuración de la app
└── Shared/
    ├── SharedKernel           — Tipos compartidos: Result<T>, Error, contratos de integración
    └── SmartCore.Telemetry    — Configuración centralizada de OpenTelemetry y Serilog
```

**Dependencias entre capas:**

```
WebApi → Application + Infrastructure
Application → Domain + SharedKernel
Infrastructure → Domain + Application + SharedKernel
Domain (sin dependencias externas)
```

---

## Clean Architecture + DDD

El proyecto sigue Clean Architecture. El dominio no depende de ningún framework externo; toda la lógica de negocio vive en `Credit.Domain`.

### Modelo de dominio

#### Entidades EF Core (`Credit.Domain/Entities/`)
Son las entidades mapeadas directamente a la base de datos por EF Core. Se usan en operaciones de lectura y persistencia transaccional directa.

| Entidad | Tabla | Descripción |
|---------|-------|-------------|
| `Customer` | `customers` | Cliente en el sistema de crédito. Se sincroniza desde el microservicio CRM vía eventos |
| `CreditProduct` | `credit_products` | Producto crediticio con tasa, método de amortización, montos y plazo |
| `CreditApplication` | `credit_applications` | Solicitud de crédito de un cliente para un producto |
| `CreditLine` | `credit_lines` | Línea de crédito activa creada al aprobar una aplicación. Almacena el schedule de amortización como `jsonb` |
| `Installment` | `installments` | Cuotas individuales de una línea de crédito |
| `CreditPayment` | `credit_payments` | Pagos registrados contra una línea de crédito |
| `DomainEvent` | `domain_events` | Outbox de eventos de dominio para publicación asíncrona confiable |

#### Agregado `CreditAgreement` (`Credit.Domain/CreditAgreement.cs`)
Agregado de dominio que encapsula el ciclo de vida completo de un crédito con una máquina de estados explícita. Acumula `DomainEvent`s en `UncommittedEvents` y soporta rehidratación desde historial (Event Sourcing).

**Estados del crédito:**

```
Created → Approved → Funded → Active ──→ Late
                                  └────→ Closed
                                  Late ──→ Closed
```

Operaciones disponibles: `Approve()`, `Fund()`, `RegisterPayment()`, `MarkAsLate()`, `Close()`.

#### Agregado `CreditContract` (`Credit.Domain/Aggregates/CreditContract.cs`)
Agregado alternativo más liviano orientado a la gestión de saldo (principal + interés outstanding) y aplicación de pagos con breakdown detallado (`PaymentBreakdown`). Hereda de `AggregateRoot`.

#### Value Objects (`Credit.Domain/ValueObjects/`)
- `Money` — monto + moneda, previene mezcla de divisas
- `CreditId` — identificador tipado del crédito
- `DateRange` — rango de fechas con validación
- `PaymentBreakdown` — descomposición de un pago en penalidad, interés y capital
- `ApplyPayment` — parámetros de aplicación de pago

---

## CQRS

Todos los casos de uso están en `Credit.Application` y siguen el patrón CQRS implementado con MediatR.

```
ICommand              → IRequest<Result>
ICommand<TResponse>   → IRequest<Result<TResponse>>
IQuery<TResponse>     → IRequest<Result<TResponse>>
```

Los handlers se registran automáticamente en `Program.cs` escaneando el assembly de `Credit.Application`:

```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetCustomerCreditStatusQuery).Assembly));
```

### Casos de uso existentes

| Área | Comando / Query |
|------|----------------|
| Aplicaciones | `CreateCreditApplicationCommand`, `ApproveCreditApplicationCommand`, `RejectCreditApplicationCommand`, `GetCreditApplicationByIdQuery` |
| Líneas de crédito | `CloseCreditLineCommand`, `GetCreditLineByIdQuery`, `GetCreditLineBalanceByIdQuery`, `ListCustomerCreditLinesQuery`, `GetCreditLineInstallmentsByIdQuery`, `GetLinePaymentScheduleByIdQuery`, `GetLinePaymentsByIdQuery` |
| Pagos | `RegisterPaymentCommand`, `GetAmortizationScheduleQuery`, `GetInstallmentsDueQuery`, `GetLineInstallmentsByIdQuery` |
| Contratos (event-sourced) | `CreateCreditContract`, `DisburseLoan`, `ApplyPayment` |

---

## Result Pattern

Todas las operaciones retornan `Result` o `Result<T>` definidos en `SharedKernel`. Nunca se lanzan excepciones para fallos esperados.

```csharp
// Éxito con valor
Result<MiDto>.Success(dto)

// Fallo con error tipado
Result.Failure(new Error("Credit.NotFound", "Línea de crédito no encontrada", ErrorType.NotFound))

// En endpoints
result.Match(
    onSuccess: () => Results.Ok(),
    onFailure: error => CustomResults.Problem(error)
)
```

`Error` contiene `Code`, `Description` y `ErrorType` (que mapea a status HTTP en `CustomResults`).

---

## Endpoints HTTP

Los endpoints usan el patrón **Minimal API + IEndpoint**. Cada grupo funcional implementa la interfaz `IEndpoint`:

```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

En `Program.cs` se auto-descubren todas las implementaciones por reflexión y se registran bajo el prefijo `api/v{version}`. Para agregar endpoints, basta crear una clase que implemente `IEndpoint` en `Credit.WebApi/Endpoints/`.

**Versionado:** URL segment — `api/v1/credit/applications`. La versión por defecto es `1`.

---

## Motor de Amortización

Vive en `Credit.Domain` como servicio de dominio. Usa el patrón Strategy:

```
AmortizationEngine / AmortizationScheduleService
    ├── FrenchAmortizationStrategy    (cuotas iguales, sistema francés)
    ├── GermanAmortizationStrategy    (capital constante)
    ├── AmericanAmortizationStrategy  (bullet / solo interés + capital al final)
    ├── FlatAmortizationStrategy      (interés plano sobre capital original)
    └── RevolvingAmortizationStrategy (crédito revolvente)
```

La estrategia se selecciona por `AmortizationMethod` (enum) configurado en el `CreditProduct`. Las estrategias se registran como singletons en `Infrastructure/DependencyInjection.cs`.

El resultado es un `AmortizationSchedule` con la lista de `Installment` (número, fecha de vencimiento, capital, interés, saldo restante).

---

## Infraestructura

### Persistencia

- **Base de datos:** PostgreSQL con extensión `uuid-ossp` para generación de UUIDs.
- **ORM:** EF Core. El `CreditDbContext` está scaffoldeado; las configuraciones de entidades están en `OnModelCreating`.
- **Convención:** todas las columnas en `snake_case`, PKs con `uuid_generate_v4()` como valor por defecto.
- **Connection string:** clave `DefaultConnection` en `appsettings.json`.
- **Campos JSON:** `AmortizationSchedule`, `Score`, `Documents`, `Metadata`, `Address` se almacenan como `jsonb`.

### Mensajería (Integración entre microservicios)

MassTransit sobre RabbitMQ. Configuración en `RabbitMqSettings:Uri`.

**Eventos consumidos:**

| Evento | Consumer | Acción |
|--------|----------|--------|
| `SaleInvoiceConfirmedEvent` | `SaleInvoiceConfirmedConsumer` | Crea automáticamente una `CreditLine` con schedule de amortización cuando se confirma una factura de venta a crédito. Incluye idempotencia por `InvoiceNumber`. |

**Contratos compartidos (`SharedKernel/Contracts/`):**

| Contrato | Dirección |
|----------|-----------|
| `CustomerCreated` | Recibido desde CRM |
| `CustomerUpdated` | Recibido desde CRM |
| `ProcessPaymentCommand` | Publicado para procesar pagos async |
| `ProcessRevolvingPaymentCommand` | Publicado para créditos revolventes |

### Observabilidad

`SmartCore.Telemetry` configura en un solo lugar:
- **Trazas:** ASP.NET Core, EF Core, HTTP client, MassTransit
- **Métricas:** Runtime, ASP.NET Core
- **Logs:** Serilog con sink a OpenTelemetry Protocol (OTLP)
- **Exportador:** OTLP (configurable vía `TelemetryOptions`)

---

## Flujo típico: Factura → Crédito

```
[Microservicio Ventas]
        │
        │ publica SaleInvoiceConfirmedEvent (RabbitMQ)
        ▼
[SaleInvoiceConfirmedConsumer]
        │
        ├── Valida idempotencia (InvoiceNumber)
        ├── Busca Customer por ExternalId
        ├── Carga CreditProduct
        ├── Calcula AmortizationSchedule
        └── Persiste CreditLine + Installments → PostgreSQL
```

## Flujo típico: Solicitud manual de crédito

```
POST /api/v1/credit/applications
        │ CreateCreditApplicationCommand (MediatR)
        ▼
POST /api/v1/credit/applications/{id}/approve
        │ ApproveCreditApplicationCommand
        ▼
[CreditLine creada con schedule]
        │
POST /api/v1/credit/lines/{id}/payments  (futuro)
        │ RegisterPaymentCommand
        ▼
[Installments actualizadas, CreditLine Outstanding reducido]
```
