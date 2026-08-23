## 1. Renombrar GetCustomerByIdTrackingAsync

- [x] 1.1 Renombrar `GetCustomerByIdTrackingAsync` → `GetForUpdateAsync` en `ICustomersRepository.cs`
- [x] 1.2 Actualizar la implementación en `CustomersRepository.cs` (renombrar el método, mantener `AsTracking()` internamente)
- [x] 1.3 Actualizar todos los llamadores: buscar `GetCustomerByIdTrackingAsync` en el codebase y reemplazar con `GetForUpdateAsync`

## 2. Eliminar GetAllCustomersAsync

- [x] 2.1 Buscar todos los usos de `GetAllCustomersAsync` en el codebase
- [x] 2.2 Si existen usos, reemplazarlos con `SearchAsync` con criterios adecuados
- [x] 2.3 Eliminar `GetAllCustomersAsync` de `ICustomersRepository` y de `CustomersRepository`

## 3. Crear CustomerSearchCriteria

- [x] 3.1 Crear `Backend/Src/Crm.Domain/Customers/CustomerSearchCriteria.cs` como record con propiedades `Query`, `Page`, `PageSize`
- [x] 3.2 Actualizar `ICustomersRepository.SearchAsync` para aceptar `CustomerSearchCriteria` en lugar de `string? query, int page, int pageSize`
- [x] 3.3 Actualizar `CustomersRepository.SearchAsync` en Infrastructure para usar `CustomerSearchCriteria`
- [x] 3.4 Actualizar `SearchCustomersQuery` en Application para construir y pasar `CustomerSearchCriteria`

## 4. Verificación

- [x] 4.1 Ejecutar `dotnet build CrmProject.sln` y confirmar que compila sin errores
- [x] 4.2 Confirmar que no existe ninguna referencia a `GetCustomerByIdTrackingAsync` en el codebase
- [x] 4.3 Confirmar que no existe `GetAllCustomersAsync` en ninguna interfaz ni implementación
