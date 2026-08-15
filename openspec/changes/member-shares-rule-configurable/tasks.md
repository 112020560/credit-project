## 1. Base de datos

- [x] 1.1 Crear migración `20260812_AddEnforceSharesCapacityLimit.sql`: `ALTER TABLE underwriting_policies ADD COLUMN IF NOT EXISTS enforce_shares_capacity_limit BOOLEAN NOT NULL DEFAULT false`

## 2. Dominio

- [x] 2.1 Agregar `bool EnforceSharesCapacityLimit = false` como parámetro con default al record `UnderwritingPolicy`
- [x] 2.2 Modificar `MemberSharesRule`: si `_policy.EnforceSharesCapacityLimit = false` y el monto supera el límite, retornar `Pass` con mensaje `"Shares limit exceeded (informative only): ..."` en lugar de `Fail`

## 3. Infraestructura

- [x] 3.1 Actualizar `UnderwritingPolicyRepository.GetActiveAsync()`: agregar `enforce_shares_capacity_limit` al SELECT y al mapeo del record `UnderwritingPolicy`

## 4. Documentación

- [x] 4.1 Actualizar `loan-lifecycle.md`: documentar `EnforceSharesCapacityLimit` en la sección de configuración de `UnderwritingPolicy` y explicar el comportamiento informativo vs hard stop
