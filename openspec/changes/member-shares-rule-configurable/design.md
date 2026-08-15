## Context

`MemberSharesRule` implementa `IHardStopRule` incondicionalmente, haciendo que cualquier préstamo donde `monto > aportaciones × SharesMultiplierLimit` sea rechazado antes de evaluar ingresos o garantías. Esta regla tiene `Priority=0`, por lo que es la primera en ejecutarse.

El motor de reglas (`ContractEngine`) separa hard stops (`IHardStopRule`) de reglas normales: los hard stops abortan la evaluación inmediatamente. El resto de reglas acumulan rate adjustments y reportan resultados sin bloquear por sí solas.

Estado actual:
- `UnderwritingPolicy`: record inmutable con 8 campos
- `MemberSharesRule`: implementa `IContractRule` + `IHardStopRule`
- `UnderwritingPolicyRepository`: lee la fila `id='default'` vía Dapper

## Goals / Non-Goals

**Goals:**
- Hacer el comportamiento de hard stop de `MemberSharesRule` configurable via `UnderwritingPolicy`
- Preservar la evaluación y reporte de la regla siempre (auditoría)
- Default = `false` (permisivo) para no romper instalaciones existentes

**Non-Goals:**
- Cambiar otras reglas del motor
- Agregar UI/API para editar la policy
- Hacer configurable el `SharesMultiplierLimit` por producto (es una política global)

## Decisions

### Decisión 1: Condicional en la regla, no en el motor
`MemberSharesRule` verifica internamente `_policy.EnforceSharesCapacityLimit`. Si es `false`, retorna `RuleEvaluationResult.Pass(...)` con mensaje informativo en lugar de `Fail`. Esto evita cambiar `ContractEngine` y mantiene el patrón existente.

**Alternativa descartada**: Remover `IHardStopRule` de `MemberSharesRule` y manejar el bloqueo en el motor. Más invasivo y rompe el contrato de la interfaz.

### Decisión 2: `UnderwritingPolicy` sigue siendo un record inmutable
Se agrega `bool EnforceSharesCapacityLimit = false` como parámetro con default. Compatible con todos los constructores existentes.

### Decisión 3: Mensaje diferenciado en el resultado
Cuando `EnforceSharesCapacityLimit = false` y se supera el límite, el mensaje debe ser explícito: `"Shares limit exceeded (informative only): ..."`. Esto permite que el analista vea en `evaluationResults` que la capacidad de aportaciones fue superada aunque no bloqueó.

## Risks / Trade-offs

- [Riesgo] Una cooperativa que ya depende del comportamiento bloqueante actual tendrá que activar `enforce_shares_capacity_limit = true` explícitamente → Mitigación: documentar en migración y en `loan-lifecycle.md`
- [Trade-off] `MemberSharesRule` sigue listada en `evaluationResults` como `passed: true` aunque el límite fue superado — puede confundir si el analista no lee el mensaje → Mitigación: mensaje descriptivo con "informative only"

## Migration Plan

1. Aplicar `20260812_AddEnforceSharesCapacityLimit.sql`
2. La columna tiene `DEFAULT false` — ninguna fila existente necesita UPDATE
3. Rollback: `ALTER TABLE underwriting_policies DROP COLUMN enforce_shares_capacity_limit`
