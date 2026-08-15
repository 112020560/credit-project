## Why

La `UnderwritingPolicy` es un singleton global cargado al inicio desde `WHERE id = 'default'`, haciendo que todos los productos financieros compartan las mismas reglas de evaluación crediticia. Esto impide tener políticas diferenciadas por tipo de producto — un préstamo hipotecario, uno personal y uno empresarial no pueden tener distintos DTI máximos, multiplicadores de aportaciones ni tasas de mora.

## What Changes

- **NUEVA columna** `underwriting_policy_id VARCHAR NOT NULL DEFAULT 'default'` en `credit_products` con FK a `underwriting_policies.id`
- **CreditProduct** expone `UnderwritingPolicyId` — el producto es ahora el portador de su política de evaluación
- **`CreateContractCommandHandler`**: deja de recibir `UnderwritingPolicy` inyectada como singleton; carga la policy del producto vía `IUnderwritingPolicyRepository.GetByIdAsync(product.UnderwritingPolicyId)` al momento de evaluar
- **`IUnderwritingPolicyRepository`**: nuevo método `GetByIdAsync(string id)`
- **`UnderwritingPolicyRepository`**: implementa `GetByIdAsync`
- **`CreditProductRepository`**: agrega `underwriting_policy_id` al SELECT, INSERT y mapeo
- **DI**: `UnderwritingPolicy` singleton se elimina; `IUnderwritingPolicyRepository` se registra para inyección en el handler
- **`POST /products`**: acepta `underwritingPolicyId` (opcional, default `"default"`)
- **`GET /products` y `GET /products/{id}`**: retorna `underwritingPolicyId`
- **NUEVO endpoint** `GET /api/v1/underwriting-policies`: lista todas las políticas disponibles con sus parámetros, para que el operador sepa qué IDs puede asignar a un producto
- **NUEVA migración SQL**: columna en `credit_products` + FK

## Capabilities

### New Capabilities
- `underwriting-policy-listing`: Endpoint para listar todas las políticas de suscripción disponibles en el sistema

### Modified Capabilities
- `underwriting-policy`: La policy deja de ser un singleton global — se carga por demanda según el producto del contrato; `CreditProduct` ahora porta su `UnderwritingPolicyId`
- `loan-contract`: El handler de creación de contratos resuelve la policy desde el producto, no desde DI

## Impact

- `CreditSystem.Domain`: `CreditProduct` (nuevo campo), `DependencyInjection.cs` (eliminar singleton)
- `CreditSystem.Application`: `CreateContractCommandHandler` (inyecta repositorio en lugar de policy)
- `CreditSystem.Infrastructure`: `CreditProductRepository`, `UnderwritingPolicyRepository`, `IUnderwritingPolicyRepository`, `DependencyInjection.cs`
- `CreditSystem.Api`: `ProductEndpoints` (request/response + underwritingPolicyId), nuevo `UnderwritingPolicyEndpoints`
- **Breaking en comportamiento**: reglas del motor ahora usan la policy del producto, no la global. Productos existentes apuntan a `'default'` automáticamente — sin impacto en datos existentes.
- Sin cambios en mensajes RabbitMQ ni webhooks
