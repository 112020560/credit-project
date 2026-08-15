## Why

El CRM necesita almacenar y comunicar datos financieros clave del cliente (score crediticio, ingresos y deuda mensual) para que servicios downstream como el motor de riesgo puedan consumirlos desde el primer evento `CustomerCreated`. Adicionalmente, estos valores deben poder actualizarse parcialmente en cualquier momento sin pisar datos existentes que el evento no incluya.

## What Changes

- El evento `CustomerCreated` poblará el campo `Metadata` con `CreditScore`, `MonthlyIncome` y `MonthlyDebt` cuando estén presentes en el request de creación. Si no se envían, los valores son `null` sin error.
- Se agregan tres columnas nullable (`CreditScore`, `MonthlyIncome`, `MonthlyDebt`) a la tabla `Customers` con su correspondiente migration de EF Core.
- Nuevo endpoint `PATCH /api/v1/customers/{id}/financials` que acepta un body con solo los campos a actualizar (`Changes`).
- El handler del nuevo endpoint publica un evento `CustomerUpdated` cuyo `Changes` contiene únicamente los campos presentes en el request.
- La lógica de persistencia usa `COALESCE` para garantizar que los campos ausentes en `Changes` conservan su valor actual en BD.

## Capabilities

### New Capabilities

- `customer-financial-profile`: Gestión del perfil financiero del cliente — almacenamiento de `CreditScore`, `MonthlyIncome` y `MonthlyDebt`; endpoint de actualización parcial `PATCH /customers/{id}/financials`; publicación de `CustomerUpdated` con `Changes`; semántica COALESCE en persistencia.

### Modified Capabilities

- `customer-management`: El evento `CustomerCreated` ahora puede llevar datos financieros en `Metadata`. El campo ya existe en el contrato pero siempre era `null`; ahora se popula condicionalmente desde el DTO de creación.

## Impact

- **DB**: Migration que agrega `CreditScore NUMERIC`, `MonthlyIncome NUMERIC`, `MonthlyDebt NUMERIC` (nullable) a la tabla `Customers`.
- **Domain**: Tres nuevas propiedades en la entidad `Customer`.
- **Application**: `CreateCustomerCommand` popula `Metadata` en `CreateCustomerContract`; nuevo `UpdateCustomerFinancialsCommand` + `UpdateCustomerFinancialsDto`.
- **Infrastructure**: `CustomersRepository` actualiza los campos financieros con lógica COALESCE.
- **WebApi**: Nuevo endpoint `PATCH /api/v1/customers/{id}/financials`.
- **Contratos MQ**: `CustomerCreated.Metadata` se popula; `CustomerUpdated.Changes` contiene solo los campos enviados.
