## 1. Mover DomainEvent/IDomainEvent a namespace compartido (prerequisito)

- [ ] 1.1 Crear directorio `Src/Core/CreditSystem.Domain/Abstractions/Events/`
- [ ] 1.2 Mover `DomainEvent.cs` de `Aggregates/LoanContract/Events/Base/` a `Abstractions/Events/` y actualizar namespace a `CreditSystem.Domain.Abstractions.Events`
- [ ] 1.3 Mover `IDomainEvent.cs` de `Aggregates/LoanContract/Events/Base/` a `Abstractions/Events/` y actualizar namespace a `CreditSystem.Domain.Abstractions.Events`
- [ ] 1.4 Eliminar el directorio `Aggregates/LoanContract/Events/Base/` si queda vacío
- [ ] 1.5 Buscar todos los archivos con `using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;` en toda la solución y reemplazar por `using CreditSystem.Domain.Abstractions.Events;`
- [ ] 1.6 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores antes de continuar

## 2. Renombrar CustomerReference → CustomerCreditProfile

- [ ] 2.1 Agregar `[assembly: InternalsVisibleTo("CreditSystem.Infrastructure")]` y `[assembly: InternalsVisibleTo("CreditSystem.Tests")]` en `CreditSystem.Domain/DependencyInjection.cs` o en un archivo `AssemblyInfo.cs` nuevo
- [ ] 2.2 Renombrar el archivo `Src/Core/CreditSystem.Domain/Entities/CustomerReference.cs` a `CustomerCreditProfile.cs`
- [ ] 2.3 Renombrar la clase `CustomerReference` a `CustomerCreditProfile`, cambiar todos los `public set` a `internal set`, y agregar constructor privado `private CustomerCreditProfile() {}`
- [ ] 2.4 Agregar factory method estático `CustomerCreditProfile.Create(Guid externalId, string fullName, string documentType, string documentNumber)` que inicializa los campos requeridos
- [ ] 2.5 Buscar todas las referencias a `CustomerReference` en `CreditSystem.Application` y reemplazar por `CustomerCreditProfile`
- [ ] 2.6 Buscar todas las referencias a `CustomerReference` en `CreditSystem.Infrastructure` y reemplazar por `CustomerCreditProfile`
- [ ] 2.7 Buscar todas las referencias a `CustomerReference` en `CreditSystem.Api` y reemplazar por `CustomerCreditProfile`
- [ ] 2.8 Buscar todas las referencias a `CustomerReference` en `CreditSystem.Tests` y reemplazar por `CustomerCreditProfile`
- [ ] 2.9 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores

## 3. Renombrar ICustomerService → ICustomerReadRepository

- [ ] 3.1 Renombrar `Src/Core/CreditSystem.Domain/Abstractions/Services/ICustomerService.cs` a `Src/Core/CreditSystem.Domain/Abstractions/Repositories/ICustomerReadRepository.cs`
- [ ] 3.2 Actualizar la interfaz: nombre `ICustomerReadRepository`, namespace `CreditSystem.Domain.Abstractions.Repositories`
- [ ] 3.3 Renombrar `Src/Core/CreditSystem.Infrastructure/Services/CustomerService.cs` a `Src/Core/CreditSystem.Infrastructure/Repositories/CustomerReadRepository.cs`
- [ ] 3.4 Actualizar la clase: nombre `CustomerReadRepository`, namespace `CreditSystem.Infrastructure.Repositories`, implementa `ICustomerReadRepository`
- [ ] 3.5 Actualizar el registro DI en Infrastructure para mapear `ICustomerReadRepository` → `CustomerReadRepository`
- [ ] 3.6 Buscar todos los `ICustomerService` en Application y Api y reemplazar por `ICustomerReadRepository`
- [ ] 3.7 Ejecutar `dotnet build` y confirmar cero errores

## 4. Constructor privado en RevolvingCreditAggregate

- [ ] 4.1 Revisar `RevolvingCreditAggregateTests.cs` y anotar si algún test usa `new RevolvingCreditAggregate()` directamente
- [ ] 4.2 Si hay tests que usan el constructor vacío: migrarlos a `RevolvingCreditAggregate.Create(...)` con parámetros válidos de prueba
- [ ] 4.3 Cambiar `public RevolvingCreditAggregate()` a `private RevolvingCreditAggregate()` en `RevolvingCreditAggregate.cs`
- [ ] 4.4 Ejecutar `dotnet build` y `dotnet test` para confirmar que no hay regresión

## 5. ContractEvaluationContext con Money

- [ ] 5.1 Actualizar `ContractEvaluationContext.cs`: cambiar `RequestedAmount` de `decimal` a `Money`, `CollateralValue` de `decimal?` a `Money?`, `MonthlyIncome` de `decimal?` a `Money?`, `MonthlyDebt` de `decimal?` a `Money?`
- [ ] 5.2 Actualizar `MaxLoanAmountRule.cs`: acceder a `context.RequestedAmount.Amount` en lugar de `context.RequestedAmount`
- [ ] 5.3 Actualizar `CollateralRule.cs`: acceder a `context.CollateralValue?.Amount` y `context.RequestedAmount.Amount`
- [ ] 5.4 Actualizar `DebtToIncomeRule.cs`: acceder a `context.MonthlyIncome?.Amount`, `context.MonthlyDebt?.Amount`, `context.RequestedAmount.Amount`
- [ ] 5.5 Actualizar `CreateContractCommandHandler.cs`: construir `RequestedAmount = new Money(command.Amount, command.Currency)` al armar el `ContractEvaluationContext`
- [ ] 5.6 Ejecutar `dotnet build` y `dotnet test` para confirmar que las reglas de evaluación funcionan correctamente

## 6. Migración PostgreSQL

- [ ] 6.1 Crear script `Src/Core/CreditSystem.Infrastructure/Migrations/20260624_CustomerCreditProfileRename.sql` con comentario de tabla actualizado y cualquier objeto dependiente (vistas, funciones) que referencie el nombre anterior
- [ ] 6.2 Verificar que el script es idempotente (puede ejecutarse múltiples veces sin error)

## 7. Verificación final

- [ ] 7.1 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores
- [ ] 7.2 Ejecutar `dotnet test` y confirmar que todos los tests pasan
- [ ] 7.3 Confirmar que no existe ninguna referencia a `CustomerReference`, `ICustomerService` ni `LoanContract.Events.Base` en el código fuente
- [ ] 7.4 Confirmar que `RevolvingCreditAggregate` no tiene constructor público vacío
- [ ] 7.5 Confirmar que `ContractEvaluationContext` no tiene campos de tipo `decimal` para valores financieros
