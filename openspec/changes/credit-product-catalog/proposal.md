## Why

El sistema actual no distingue entre tipos de crédito: cualquier monto, plazo o método de amortización puede combinarse libremente. En una cooperativa, cada producto crediticio tiene parámetros propios (tasas, plazos, montos, colateral) que definen qué se puede otorgar y bajo qué condiciones. Sin un catálogo de productos, es imposible aplicar políticas diferenciadas por tipo de crédito, cumplir con la segmentación de cartera exigida por SUGEF, ni ofrecer al socio opciones estructuradas de financiamiento.

## What Changes

- Se introduce la entidad `CreditProduct` en el domain layer que encapsula los parámetros de un producto de crédito: nombre, tasa base propia, límites de monto y plazo, método de amortización por defecto, requerimiento de colateral, LTV máximo y estado.
- Se agrega el value object `ProductLimits` (monto mín/máx, plazo mín/máx) y `ProductRates` (tasa base, tasa moratoria referencial) como parte de `CreditProduct`.
- Se expone `ICreditProductRepository` con operaciones de lectura y escritura en Infrastructure.
- `CreateContractCommand` incluye un `ProductId` obligatorio. El handler resuelve el producto antes de construir el contexto de evaluación.
- Se agrega la regla de underwriting `ProductEligibilityRule` (Priority 1, hard stop) que valida monto, plazo, colateral y LTV contra los parámetros del producto.
- La tasa base del `CreditProduct` sobreescribe `UnderwritingPolicy.BaseInterestRate` para ese contrato, pasando como parámetro al `ContractEngine`.
- Se exponen endpoints de administración del catálogo: `POST /products`, `PUT /products/{id}/status`, `GET /products`, `GET /products/{id}`.
- Migración SQL con tabla `credit_products` y seed de 5 productos típicos de cooperativa costarricense.

## Capabilities

### New Capabilities

- `credit-product`: Entidad `CreditProduct` con value objects `ProductLimits` y `ProductRates`, `ICreditProductRepository`, repositorio en Infrastructure y endpoints de administración del catálogo.
- `product-eligibility-rule`: Regla de underwriting `ProductEligibilityRule` que valida monto, plazo, colateral y LTV del contrato contra el producto seleccionado.

### Modified Capabilities

- `loan-contract`: `CreateContractCommand` requiere `ProductId`. El handler resuelve el producto, valida existencia y estado, y usa su tasa base en lugar de la de la política global. `ContractEvaluationContext` incorpora `CreditProduct` y la tasa base efectiva se pasa como parámetro al engine.
- `underwriting-policy`: El `ContractEngine` recibe la tasa base como parámetro por contrato en lugar de leerla siempre desde la política, permitiendo que el producto la sobreescriba.

## Impact

- **Domain**: nueva entidad `CreditProduct`, value objects `ProductLimits` y `ProductRates`, enum `ProductStatus`, interfaz `ICreditProductRepository`, nueva regla `ProductEligibilityRule`, actualización de `ContractEvaluationContext` con `CreditProduct`.
- **Application**: `CreateContractCommand` y su validator actualizados, `CreateContractCommandHandler` resuelve el producto, `ContractEngine.EvaluateAsync` recibe tasa base como parámetro.
- **Infrastructure**: `CreditProductRepository` (Dapper), migración SQL con seed de 5 productos, registro en DI.
- **API**: nuevo grupo de endpoints `/products` para administración del catálogo.
- **Breaking**: `CreateContractCommand` ahora requiere `ProductId` — los callers deben incluirlo.
