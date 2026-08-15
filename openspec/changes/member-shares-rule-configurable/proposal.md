## Why

La regla `MemberSharesRule` actualmente implementa `IHardStopRule` con `Priority=0`, bloqueando cualquier préstamo cuyo monto supere `aportaciones × SharesMultiplierLimit`, sin considerar ingresos ni garantías del solicitante. Esto hace imposible otorgar crédito a socios con aportaciones bajas pero capacidad de pago real, lo que no refleja la política de ninguna cooperativa real.

## What Changes

- Nuevo campo `EnforceSharesCapacityLimit` (bool) en `UnderwritingPolicy` — controla si la regla de aportaciones actúa como hard stop o como evaluación informativa
- `MemberSharesRule` deja de implementar `IHardStopRule` incondicionalmente: solo bloquea si `EnforceSharesCapacityLimit = true`; si es `false`, evalúa y reporta el resultado (warning para auditoría) pero no rechaza el préstamo
- Migration SQL: nueva columna `enforce_shares_capacity_limit BOOLEAN NOT NULL DEFAULT false` en `underwriting_policies`
- `UnderwritingPolicyRepository` actualizado para leer el nuevo campo

## Capabilities

### New Capabilities
- `shares-capacity-enforcement`: Política configurable que determina si el límite de crédito basado en aportaciones del socio es obligatorio (hard stop) o informativo (evaluación de auditoría)

### Modified Capabilities
- `underwriting-policy`: Se agrega un nuevo parámetro de política (`EnforceSharesCapacityLimit`) que cambia el comportamiento de evaluación de contratos

## Impact

- `CreditSystem.Domain`: `UnderwritingPolicy` record, `MemberSharesRule`
- `CreditSystem.Infrastructure`: `UnderwritingPolicyRepository`, nueva migración SQL
- Comportamiento: con `enforce_shares_capacity_limit = false` (default), préstamos que antes eran rechazados por aportaciones insuficientes ahora pueden ser aprobados si el resto de reglas pasan
- Sin cambios en API ni contratos externos
