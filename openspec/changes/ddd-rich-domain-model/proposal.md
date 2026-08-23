## Why

El backend CRM tiene un modelo de dominio anémico: todas las entidades son bolsas de datos sin comportamiento, las reglas de negocio están dispersas en los Command Handlers de Application, y no existen Domain Events en el dominio. Esto provoca que las invariantes de los aggregates puedan violarse silenciosamente, que la lógica de conversión Prospect→Customer esté triplicada en distintos handlers, y que los cambios de estado (Approve, Reject, Submit) no tengan un punto único de verdad. Este cambio establece las bases del modelo de dominio rico que el sistema necesita para escalar con seguridad.

## What Changes

- Se introduce `AggregateRoot` como clase base con soporte de **Domain Events** (`IDomainEvent`, `RaiseDomainEvent()`, `ClearDomainEvents()`)
- Los aggregates `CreditApplication`, `Prospect`, `Customer`, `WorkflowDefinition` y `RiskMatrix` heredan de `AggregateRoot`
- Cada aggregate recibe métodos de comportamiento que encapsulan las transiciones de estado y verifican precondiciones:
  - `CreditApplication`: `Submit()`, `Approve()`, `Reject(reason)`, `SendToReview(workflowId)`
  - `Prospect`: `Submit()`, `Enrich(...)`, `Convert()`, `Reject()`
  - `Customer`: `Activate()`, `Deactivate()`, `UpdateProfile(...)`, `UpdateFinancials(...)`
  - `WorkflowDefinition`: `Activate()`, `Deactivate()`
  - `RiskMatrix`: `Activate()`
- Se agrega el Factory Method `Customer.FromProspect(Prospect)` centralizando la conversión que hoy está triplicada
- Las sub-entidades sin identidad de negocio propia (`CustomerAddress`, `CustomerEmail`, `CustomerPhone`, `CustomerFiscalInfo`, `CustomerWorkInfo` y equivalentes de `Prospect`) se convierten en **Value Objects** inmutables, configurados con `OwnsMany`/`OwnsOne` en EF Core
- Los Command Handlers de Application se actualizan para llamar los métodos de comportamiento y recoger los Domain Events, traduciéndolos a `OutboxEvent` en lugar de mutar propiedades directamente
- Se crea migración EF Core para reflejar los cambios de schema derivados de la conversión a Value Objects
- Los nombres de eventos de integración se unifican a partir de los Domain Events (`CreditApplicationApproved`, `CreditApplicationRejected` — elimina duplicados como `ApplicationApproved` / `ApplicationRejected`)

## Capabilities

### New Capabilities

- `aggregate-root`: Clase base `AggregateRoot` con infraestructura de Domain Events (`IDomainEvent`, colección interna, métodos de raise/clear)
- `domain-behaviors`: Métodos de comportamiento en cada aggregate con verificación de precondiciones y emisión de Domain Events

### Modified Capabilities

- `customer-management`: El aggregate `Customer` gana comportamiento (`Activate`, `Deactivate`, `UpdateProfile`, `UpdateFinancials`) y sus sub-entidades pasan a ser Value Objects
- `prospect-management`: El aggregate `Prospect` gana comportamiento (`Submit`, `Enrich`, `Convert`, `Reject`) y sus sub-entidades pasan a ser Value Objects
- `prospect-to-customer-conversion`: La conversión Prospect→Customer ahora ocurre vía `Customer.FromProspect(prospect)` — Factory Method en el dominio — y se dispara desde Domain Events en lugar de lógica duplicada en handlers
- `credit-applications`: `CreditApplication` gana comportamiento (`Submit`, `Approve`, `Reject`, `SendToReview`) con invariantes encapsuladas

## Impact

- **Crm.Domain**: Cambios en todos los aggregates principales; nuevos archivos `AggregateRoot.cs`, `IDomainEvent.cs`; sub-entidades de `Customer` y `Prospect` reescritas como Value Objects
- **Crm.Application**: Todos los Command Handlers que mutan estado de aggregates se actualizan para usar métodos de comportamiento y publicar Domain Events al outbox
- **Crm.Infrastructure**: Configuración EF Core (`CrmDbContext`) actualizada con `OwnsMany`/`OwnsOne` para Value Objects; nueva migración de base de datos requerida
- **Breaking**: Eliminación de `ApproveCreditApplicationCommandHandler.MapProspectToCustomer` (método estático movido al dominio como `Customer.FromProspect`)
- **Breaking**: Columnas de sub-entidades (`customer_addresses`, `customer_emails`, etc.) pueden cambiar de schema al pasar a `OwnsMany`
