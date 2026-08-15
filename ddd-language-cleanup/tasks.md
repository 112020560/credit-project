## 1. Eliminar artefactos sin propósito

- [x] 1.1 Eliminar `Backend/Src/Crm.Domain/Class1.cs`
- [x] 1.2 Eliminar `Backend/Src/Crm.Domain/Customers/CustomerModel.cs`
- [x] 1.3 Eliminar el método `ConvertToModel()` de `Customer.cs` (que retornaba `CustomerModel`)
- [x] 1.4 Verificar que no existe ninguna referencia a `CustomerModel` ni a `ConvertToModel()` en el codebase — el compilador lo confirmará

## 2. Crear CustomerStatus

- [x] 2.1 Crear `Backend/Src/Crm.Domain/Customers/CustomerStatus.cs` con constantes `Active` e `Inactive` siguiendo el patrón de `CreditApplicationStatus`
- [x] 2.2 Reemplazar el magic string `"Active"` en `CreateCustomerCommand.cs` con `CustomerStatus.Active`
- [x] 2.3 Buscar cualquier otra ocurrencia de `"Active"` usada como Customer status en el codebase y reemplazar con `CustomerStatus.Active`

## 3. Documentar CustomersRef

- [x] 3.1 Agregar comentario XML `<summary>` en `CustomersRef.cs` explicando que representa un snapshot de referencia de un cliente proveniente de un sistema externo, y que el nombre definitivo está pendiente de confirmar con el equipo
- [x] 3.2 Registrar en el backlog la tarea de renombrar `CustomersRef` una vez confirmado su propósito con el equipo de negocio

## 4. Verificación

- [x] 4.1 Ejecutar `dotnet build CrmProject.sln` y confirmar que compila sin errores
- [x] 4.2 Confirmar que no quedan magic strings de status del Customer en el codebase (buscar: `Status = "Active"`)
