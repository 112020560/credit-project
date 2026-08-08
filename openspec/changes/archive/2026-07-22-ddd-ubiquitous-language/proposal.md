## Why

El análisis DDD identificó 5 problemas de Lenguaje Ubicuo y modelo de dominio de prioridad media: nombres que no comunican el concepto de negocio, modelos anémicos con setters públicos, constructores que permiten crear aggregates en estado inválido, una clase base de eventos acoplada al namespace incorrecto, y valores financieros representados como primitivos en lugar de Value Objects. Estos problemas dificultan la comunicación entre el equipo técnico y el negocio, y debilitan las invariantes del modelo.

Este cambio depende de `ddd-structural-integrity` ya que `CustomerReference` se renombra a `CustomerCreditProfile` y el `ICustomerReferenceRepository` introducido allí también se actualizará.

## What Changes

- **BREAKING** Renombrar `CustomerReference` → `CustomerCreditProfile` en todo el código y hacer la migración de columna/tabla en PostgreSQL (`customer_references` mantiene el nombre de tabla pero la clase cambia)
- **BREAKING** Hacer `CustomerCreditProfile` no-anémico: convertir todos los setters públicos a `private set` e introducir un factory method estático `CustomerCreditProfile.Create(...)` para construcción válida
- Renombrar `ICustomerService` → `ICustomerReferenceRepository` (de lectura) en `Domain/Abstractions/` y su implementación `CustomerService` → `CustomerReadRepository` en Infrastructure
- Hacer el constructor vacío de `RevolvingCreditAggregate` `private` para forzar el uso del factory method `Create(...)`
- Mover `DomainEvent` e `IDomainEvent` de `CreditSystem.Domain.Aggregates.LoanContract.Events.Base` a `CreditSystem.Domain.Abstractions.Events`
- Cambiar `ContractEvaluationContext` para usar `Money` en lugar de `decimal`/`decimal?` para los campos financieros: `RequestedAmount`, `CollateralValue`, `MonthlyIncome`, `MonthlyDebt`

## Capabilities

### New Capabilities

_(ninguna — este cambio refactoriza modelo existente sin agregar nueva funcionalidad)_

### Modified Capabilities

- `loan-contract`: `ContractEvaluationContext` cambia el tipo de `RequestedAmount` de `decimal` a `Money` — los handlers que construyen el contexto deben actualizarse
- `customer-crm-sync`: `CustomerReference` se renombra a `CustomerCreditProfile`; el repositorio de lectura se renombra de `ICustomerService` a `ICustomerReadRepository`

## Impact

- `CreditSystem.Domain` — `Entities/CustomerReference.cs` (renombrado y encapsulado), `Rules/ContractEvaluationContext.cs` (tipos financieros → Money), `Aggregates/LoanContract/Events/Base/` (namespace movido), `Abstractions/Services/ICustomerService.cs` (renombrado)
- `CreditSystem.Application` — handlers que construyen `ContractEvaluationContext` deben usar `Money` para el monto solicitado
- `CreditSystem.Infrastructure` — `Services/CustomerService.cs` (renombrado a `CustomerReadRepository`), `Aggregates/RevolvingCredit/RevolvingCreditAggregate.cs` (constructor privado), actualización de `using` para nuevo namespace de `DomainEvent`
- `CreditSystem.Api` — sin cambios en endpoints (la API no expone `CustomerCreditProfile` directamente)
- **PostgreSQL**: la tabla `customer_references` mantiene su nombre; se agrega migración SQL para renombrar cualquier vista o comentario de columna si aplica. No hay cambio de esquema de columnas.
- Requiere actualizar todos los archivos que referencien `CreditSystem.Domain.Aggregates.LoanContract.Events.Base` para el nuevo namespace de `DomainEvent`
