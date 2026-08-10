## 1. Domain — Enums y Value Objects

- [x] 1.1 Crear enum `ProductStatus` (`Active = 1`, `Inactive = 0`) en `CreditSystem.Domain/Enums/`
- [x] 1.2 Crear value object `ProductLimits` (`MinAmount: decimal`, `MaxAmount: decimal`, `MinTermMonths: int`, `MaxTermMonths: int`) con validación en constructor en `CreditSystem.Domain/ValueObjects/`
- [x] 1.3 Crear value object `ProductRates` (`BaseInterestRate: decimal?`, `MaxLtv: decimal?`) en `CreditSystem.Domain/ValueObjects/`

## 2. Domain — Entidad CreditProduct

- [x] 2.1 Crear la entidad `CreditProduct` en `CreditSystem.Domain/Entities/` con campos: `Id`, `Name`, `Limits`, `Rates`, `DefaultAmortizationMethod`, `RequiresCollateral`, `Status`
- [x] 2.2 Implementar constructor de `CreditProduct` con validaciones básicas (nombre no vacío, `Limits` y `Rates` no nulos)
- [x] 2.3 Crear interfaz `ICreditProductRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con métodos `GetByIdAsync`, `GetAllActiveAsync`, `GetAllAsync`, `InsertAsync`, `UpdateStatusAsync`

## 3. Domain — Regla de underwriting ProductEligibilityRule

- [x] 3.1 Agregar campo `Product: CreditProduct?` al record `ContractEvaluationContext`
- [x] 3.2 Crear `ProductEligibilityRule` en `CreditSystem.Domain/Rules/Implementations/` implementando `IContractRule` e `IHardStopRule` con `Priority = 1`
- [x] 3.3 Implementar lógica de `ProductEligibilityRule`: omitir si `context.Product` es null, validar monto dentro del rango del producto, validar plazo dentro del rango, validar colateral si `RequiresCollateral = true`, validar LTV si `MaxLtv` está definido

## 4. Domain — Actualización ContractEngine

- [x] 4.1 Actualizar firma de `ContractEngine.EvaluateAsync` para recibir `decimal? baseInterestRate = null` como parámetro adicional
- [x] 4.2 Cambiar la lógica interna de `ContractEngine` para calcular `effectiveBaseRate = baseInterestRate ?? _policy.BaseInterestRate`

## 5. Infrastructure — Repositorio y migración

- [x] 5.1 Crear script de migración `20260808_AddCreditProductsTable.sql` con tabla `credit_products` e índice único en `name`, incluyendo seed de 5 productos (Personal 14%, Consumo 18%, Vehicular 12%, Hipotecario 9%, Microcrédito 20%)
- [x] 5.2 Implementar `CreditProductRepository` en `CreditSystem.Infrastructure/Repositories/` usando Dapper para todos los métodos de `ICreditProductRepository`
- [x] 5.3 Registrar `ICreditProductRepository → CreditProductRepository` en `DependencyInjection.cs`
- [x] 5.4 Registrar `ProductEligibilityRule` en el contenedor DI como `IContractRule`

## 6. Application — Handler de creación de contrato

- [x] 6.1 Agregar campo `ProductId: Guid` (requerido) a `CreateContractCommand`
- [x] 6.2 Actualizar `CreateContractCommandValidator` para validar que `ProductId != Guid.Empty`
- [x] 6.3 Actualizar `CreateContractCommandHandler`: inyectar `ICreditProductRepository`, resolver el `CreditProduct` por `ProductId`, validar que exista y esté `Active`, poblar `context.Product`
- [x] 6.4 Calcular la tasa base efectiva en el handler (`product.Rates.BaseInterestRate ?? policy.BaseInterestRate`) y pasarla como parámetro a `ContractEngine.EvaluateAsync`

## 7. API — Endpoints del catálogo de productos

- [x] 7.1 Crear `ProductEndpoints.cs` en `CreditSystem.Api/EndPoints/` con `POST /api/products`, `GET /api/products`, `GET /api/products/{id}` y `PUT /api/products/{id}/status`
- [x] 7.2 Crear DTOs de request/response: `CreateProductRequest`, `UpdateProductStatusRequest`, `ProductResponse`
- [x] 7.3 Registrar `app.MapProductEndpoints()` en `Program.cs`

## 8. Tests

- [x] 8.1 Agregar tests unitarios de `ProductEligibilityRule`: monto fuera de rango, plazo fuera de rango, colateral requerido ausente, LTV excedido, producto null omite la regla, prioridad = 1
- [x] 8.2 Agregar tests unitarios de `ContractEngine`: tasa base del producto sobreescribe la de la política, fallback a tasa de política cuando producto no define tasa
- [x] 8.3 Actualizar tests de `CreateContractCommandHandler` para cubrir: producto no encontrado, producto inactivo, contrato con producto activo y tasa propia
