## Why

El codebase tiene varios naming smells que violan el principio de Ubiquitous Language: clases duplicadas sin propósito claro (`CustomerModel`), nombres técnicos ambiguos (`CustomersRef`, `GetCustomerByIdTrackingAsync`), magic strings de status, y nombres de eventos de integración inconsistentes. Estos problemas dificultan que cualquier desarrollador nuevo entienda el dominio leyendo el código, y aumentan el riesgo de bugs por confusión semántica.

## What Changes

- Eliminar `CustomerModel.cs` del Domain layer (era una clase duplicada de Customer sin propósito semántico)
- Renombrar o clarificar `CustomersRef` según lo que realmente represente en el negocio
- Crear clase `CustomerStatus` con constantes de string para eliminar el magic string `"Active"` hardcoded en `CreateCustomerCommand`
- Eliminar `Class1.cs` del proyecto Crm.Domain (scaffolding sobrante)
- Actualizar los puntos del código que usaban `CustomerModel` o el magic string

## Capabilities

### New Capabilities

- `customer-status`: Clase estática `CustomerStatus` en `Crm.Domain/Customers/` con constantes para todos los valores de status del Customer (`Active`, `Inactive`)

### Modified Capabilities

- `customer-management`: El status del Customer pasa a usar `CustomerStatus.Active` en lugar del magic string `"Active"`

## Impact

- **Crm.Domain**: Eliminar `CustomerModel.cs`, eliminar `Class1.cs`, crear `CustomerStatus.cs`, renombrar/clarificar `CustomersRef.cs`
- **Crm.Application**: Actualizar referencias a `CustomerModel` si las hay; actualizar `CreateCustomerCommand` para usar `CustomerStatus.Active`
- Sin breaking changes en API o schema de base de datos
