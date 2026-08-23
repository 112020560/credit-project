## 1. Base de datos y dominio

- [x] 1.1 Agregar propiedades `CreditScore (decimal?)`, `MonthlyIncome (decimal?)`, `MonthlyDebt (decimal?)` a la entidad `Customer` en `Crm.Domain`
- [x] 1.2 Crear migration EF Core `AddFinancialColumnsToCustomers` con las tres columnas nullable (`NUMERIC`) en la tabla `Customers`
- [x] 1.3 Configurar las columnas en `CrmDbContext` (columnas nullable, tipo `numeric`)
- [x] 1.4 Ejecutar `dotnet ef database update` y verificar que la migration se aplica correctamente

## 2. DTO de creación — poblar Metadata en CustomerCreated

- [x] 2.1 Agregar campo opcional `Metadata: Dictionary<string, object>?` a `CreateCustomerDto` (o campos tipados `CreditScore?`, `MonthlyIncome?`, `MonthlyDebt?` según decisión de diseño)
- [x] 2.2 Mapear los valores financieros desde el DTO a las columnas del `Customer` en `CreateCustomerCommand.MapToDto`
- [x] 2.3 Poblar `Metadata` en `CreateCustomerContract` con las claves `"CreditScore"`, `"MonthlyIncome"`, `"MonthlyDebt"` usando los valores del DTO (null si no se envían)

## 3. Comando de actualización financiera parcial

- [x] 3.1 Crear `UpdateCustomerFinancialsDto` con propiedades `CreditScore (decimal?)`, `MonthlyIncome (decimal?)`, `MonthlyDebt (decimal?)` (todas opcionales)
- [x] 3.2 Agregar validación FluentValidation: al menos un campo debe ser no-null
- [x] 3.3 Crear `UpdateCustomerFinancialsCommand(Guid CustomerId, UpdateCustomerFinancialsDto Dto) : ICommand<Unit>` con su handler en `Crm.Application`
- [x] 3.4 En el handler: cargar el `Customer` con `AsTracking()`, aplicar solo los campos presentes en el DTO, llamar `SaveChangesAsync`
- [x] 3.5 En el handler: publicar `CustomerUpdated` via `IMqProducerService.PublishEvent` con `Changes` conteniendo solo las claves enviadas

## 4. Endpoint PATCH /financials

- [x] 4.1 Crear `Crm.WebApi/Endpoints/Customers/UpdateFinancials.cs` implementando `IEndpoint`
- [x] 4.2 Mapear `PATCH /api/v1/customers/{id}/financials` → `IMediator.Send(UpdateCustomerFinancialsCommand)`
- [x] 4.3 Manejar `Result` con `.Match()`: 204 en éxito, 404 si customer no existe, 422 en validación

## 5. Repositorio — soporte de actualización parcial

- [x] 5.1 Verificar que `GetByIdAsync` en `CustomersRepository` tiene overload o parámetro `asTracking` (ver patrón en `bugs.md` BUG-003)
- [x] 5.2 Confirmar que `UpdateAsync` es no-op y que EF change tracking detecta los cambios en `SaveChangesAsync`

## 6. Verificación end-to-end

- [x] 6.1 Crear un customer con `metadata.CreditScore = 720` y verificar que el evento `CustomerCreated` llega con `Metadata["CreditScore"] = 720`
- [x] 6.2 Llamar `PATCH /api/v1/customers/{id}/financials` con `{ "CreditScore": 780 }` y verificar que solo `CreditScore` cambia en BD y el evento `CustomerUpdated` solo contiene esa clave en `Changes`
- [x] 6.3 Crear un customer sin metadata y verificar que `Metadata` es null en el evento y las columnas quedan NULL en BD sin error
