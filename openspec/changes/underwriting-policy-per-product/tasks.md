## 1. Migración de base de datos

- [x] 1.1 Crear `Src/Core/CreditSystem.Infrastructure/Migrations/20260812_AddUnderwritingPolicyIdToProducts.sql` con ALTER TABLE para agregar columna `underwriting_policy_id VARCHAR NOT NULL DEFAULT 'default'` y FK a `underwriting_policies.id`

## 2. Domain — CreditProduct y ContractEvaluationContext

- [x] 2.1 Agregar propiedad `UnderwritingPolicyId` (string) a `CreditProduct` en `Src/Core/CreditSystem.Domain/Models/CreditProduct.cs`
- [x] 2.2 Agregar campo `UnderwritingPolicy Policy` al record `ContractEvaluationContext` en `Src/Core/CreditSystem.Domain/Rules/ContractEvaluationContext.cs`
- [x] 2.3 Actualizar las reglas que leen policy por constructor (`DebtToIncomeRule`, `PaymentCapacityRule`, `CreditScoreRule`, `MemberSharesRule`) para leer `context.Policy` en lugar del campo `_policy` inyectado

## 3. Repositorio — IUnderwritingPolicyRepository y UnderwritingPolicyRepository

- [x] 3.1 Agregar método `GetByIdAsync(string id, CancellationToken ct)` a `IUnderwritingPolicyRepository` en `Src/Core/CreditSystem.Application/Interfaces/IUnderwritingPolicyRepository.cs`
- [x] 3.2 Implementar `GetByIdAsync` en `UnderwritingPolicyRepository` en `Src/Core/CreditSystem.Infrastructure/Repositories/UnderwritingPolicyRepository.cs` — SELECT por `id`, retorna `null` si no encontrado

## 4. Repositorio — CreditProductRepository

- [x] 4.1 Actualizar SELECT en `CreditProductRepository` para incluir `underwriting_policy_id` y mapearlo a `CreditProduct.UnderwritingPolicyId`
- [x] 4.2 Actualizar INSERT/UPDATE en `CreditProductRepository` para persistir `underwriting_policy_id`

## 5. Application — CreateContractCommandHandler

- [x] 5.1 Reemplazar la dependencia `UnderwritingPolicy` inyectada por `IUnderwritingPolicyRepository` en `CreateContractCommandHandler`
- [x] 5.2 En el método `Handle`: cargar `policy = await _policyRepo.GetByIdAsync(product.UnderwritingPolicyId)` y retornar error si `null`
- [x] 5.3 Asignar `policy` al `ContractEvaluationContext` antes de invocar `ContractEngine.EvaluateAsync`

## 6. DI — Eliminar singleton de UnderwritingPolicy

- [x] 6.1 Eliminar el registro `AddSingleton<UnderwritingPolicy>` de `Src/Core/CreditSystem.Infrastructure/DependencyInjection.cs`
- [x] 6.2 Verificar que `IUnderwritingPolicyRepository` ya está registrado en DI (si no, registrarlo como scoped)

## 7. API — ProductEndpoints y UnderwritingPolicyEndpoints

- [x] 7.1 Actualizar request DTO de `POST /products` para aceptar `underwritingPolicyId` (opcional, default `"default"`)
- [x] 7.2 Actualizar response DTO de `GET /products` y `GET /products/{id}` para incluir `underwritingPolicyId`
- [x] 7.3 Crear `UnderwritingPolicyEndpoints.cs` en `Src/Core/CreditSystem.Api/Endpoints/` con `GET /api/v1/underwriting-policies`
- [x] 7.4 Registrar `app.MapUnderwritingPolicyEndpoints()` en `Program.cs`
