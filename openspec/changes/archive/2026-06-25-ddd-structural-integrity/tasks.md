## 1. Validación previa — Constructor buggy de LoanContractAggregate

- [x] 1.1 Buscar en toda la solución referencias al constructor `new LoanContractAggregate(IEnumerable<IDomainEvent>)` (un solo argumento de tipo IEnumerable) — revisar Infrastructure, Tests y cualquier otro proyecto que referencie Domain
- [x] 1.2 Si se encuentran callers: migrarlos a `new LoanContractAggregate(null, events)` y confirmar que el proyecto compila sin errores
- [x] 1.3 Eliminar el constructor `public LoanContractAggregate(IEnumerable<IDomainEvent> events)` de `LoanContractAggregate.cs`
- [x] 1.4 Ejecutar `dotnet build CreditBackend.sln` y confirmar que no hay errores de compilación
- [x] 1.5 Ejecutar `dotnet test` y confirmar que los tests de `LoanContractAggregateTests` pasan sin regresión

## 2. Remover ILogger de ContractEngine

- [x] 2.1 Eliminar el parámetro `ILogger<ContractEngine> logger` del constructor de `ContractEngine` y remover todos los usos de `_logger` dentro del método `EvaluateAsync`
- [x] 2.2 Localizar `CreateContractCommandHandler` en `CreditSystem.Application/Commands/CreateContract/` e inyectar `ILogger<CreateContractCommandHandler>` si no lo tiene ya
- [x] 2.3 En `CreateContractCommandHandler`, loguear el resultado de `ContractEngine.EvaluateAsync()`: resultado aprobado/rechazado, lista de reglas evaluadas, y ajuste de tasa final
- [x] 2.4 Actualizar el registro de `ContractEngine` en el contenedor DI (si tiene registro explícito) para eliminar la dependencia del logger
- [x] 2.5 Ejecutar `dotnet build` y `dotnet test` para confirmar que no hay regresión

## 3. Mover entidades de infraestructura fuera del Domain

- [x] 3.1 Crear el directorio `Src/Core/CreditSystem.Infrastructure/Persistence/Entities/`
- [x] 3.2 Mover `Src/Core/CreditSystem.Domain/Entities/OutboxMessage.cs` a `Domain/Abstractions/Persistence/OutboxMessage.cs` y actualizar el namespace a `CreditSystem.Domain.Abstractions.Persistence`
- [x] 3.3 Mover `Src/Core/CreditSystem.Domain/Entities/WebhookSubscription.cs` a `Domain/Abstractions/Persistence/WebhookSubscription.cs` y actualizar el namespace a `CreditSystem.Domain.Abstractions.Persistence`
- [x] 3.4 Buscar todos los `using CreditSystem.Domain.Entities;` en `CreditSystem.Infrastructure` que referencien `OutboxMessage`, `WebhookSubscription`, `WebhookDelivery`, `OutboxMessageStatus`, `WebhookDeliveryStatus` o `WebhookEventTypes` y actualizarlos al nuevo namespace
- [x] 3.5 Verificar que las interfaces `IOutboxRepository`, `IWebhookSubscriptionRepository`, `IWebhookDeliveryRepository` en `Domain/Abstractions/Persistence/` aún compilan correctamente (pueden necesitar actualizar su `using` para los tipos de parámetro)
- [x] 3.6 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores

## 4. Introducir Anti-Corruption Layer: SyncCustomerFromCrm

- [x] 4.1 Revisar `CustomerUpdatedConsumer.cs` y anotar si tiene campos adicionales respecto a `CustomerCreatedConsumer` que requieran tratamiento diferente en el command
- [x] 4.2 Crear `Src/Core/CreditSystem.Application/Commands/SyncCustomerFromCrm/SyncCustomerFromCrmCommand.cs` con los campos internos ya traducidos: `ExternalId (Guid)`, `FullName`, `Email`, `Phone`, `DocumentType`, `DocumentNumber`, `CreditScore (int?)`, `MonthlyIncome (decimal?)`, `MonthlyDebt (decimal?)`
- [x] 4.3 Crear `Src/Core/CreditSystem.Domain/Abstractions/Repositories/ICustomerReferenceRepository.cs` con el método `Task UpsertAsync(...)` con parámetros individuales
- [x] 4.4 Crear `Src/Core/CreditSystem.Infrastructure/Repositories/CustomerReferenceRepository.cs` implementando `ICustomerReferenceRepository` con Dapper + Npgsql, moviendo el SQL del consumer actual (INSERT ... ON CONFLICT DO UPDATE) a esta implementación
- [x] 4.5 Crear `Src/Core/CreditSystem.Application/Commands/SyncCustomerFromCrm/SyncCustomerFromCrmCommandHandler.cs` que recibe `ICustomerReferenceRepository` y llama `UpsertAsync`
- [x] 4.6 Refactorizar `CustomerCreatedConsumer.cs`: eliminar toda la lógica SQL y de extracción de metadata; inyectar `IMediator`; construir `SyncCustomerFromCrmCommand` traduciendo los campos CRM; despachar via `IMediator.Send()`
- [x] 4.7 Refactorizar `CustomerUpdatedConsumer.cs` de la misma forma que el paso anterior
- [x] 4.8 Registrar `ICustomerReferenceRepository` → `CustomerReferenceRepository` en el contenedor DI de `CreditSystem.Infrastructure`
- [x] 4.9 Eliminar la inyección directa del connection string en los consumers (ya no la necesitan)
- [x] 4.10 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores
- [x] 4.11 Ejecutar `dotnet test` y confirmar que todos los tests pasan

## 5. Verificación final

- [x] 5.1 Confirmar que `CreditSystem.Domain` no tiene referencias directas a `Microsoft.Extensions.Logging` (salvo que sea una abstracción de dominio explícita)
- [x] 5.2 Confirmar que `CreditSystem.Domain/Entities/` no contiene `OutboxMessage.cs` ni `WebhookSubscription.cs`
- [x] 5.3 Confirmar que `CustomerCreatedConsumer` y `CustomerUpdatedConsumer` no contienen SQL ni referencias a Dapper/Npgsql
- [x] 5.4 Confirmar que `LoanContractAggregate` tiene exactamente un constructor público: `(LoanContractState? snapshot, IEnumerable<IDomainEvent> events)`
