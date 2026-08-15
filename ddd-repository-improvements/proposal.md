## Why

Las interfaces de repositorios en `Crm.Domain` tienen dos problemas: exponen conceptos de persistencia (como `AsTracking` de EF Core) y tienen métodos peligrosos en producción (como `GetAllCustomersAsync` sin límite). Esto viola el principio de que las interfaces del dominio deben hablar el lenguaje del negocio, no de la infraestructura.

## What Changes

- Renombrar `GetCustomerByIdTrackingAsync` → `GetForUpdateAsync` (semántica de negocio, no de EF)
- Eliminar o restringir `GetAllCustomersAsync` — reemplazar los usos con `SearchAsync` paginado o queries específicas
- Encapsular los parámetros de búsqueda de clientes en un objeto `CustomerSearchCriteria` para separar criterios de dominio de parámetros de UI

## Capabilities

### New Capabilities

- `customer-search-criteria`: Value Object `CustomerSearchCriteria` que encapsula los criterios de búsqueda de clientes (query string, paginación)

### Modified Capabilities

- `customer-management`: La búsqueda de clientes usa `CustomerSearchCriteria` en lugar de parámetros sueltos

## Impact

- **Crm.Domain**: Modificar `ICustomersRepository` — renombrar método, eliminar `GetAllCustomersAsync`, cambiar firma de `SearchAsync`
- **Crm.Infrastructure**: Actualizar `CustomersRepository` para implementar la nueva interfaz
- **Crm.Application**: Actualizar los handlers/queries que usan los métodos modificados
- Sin breaking changes en API REST
