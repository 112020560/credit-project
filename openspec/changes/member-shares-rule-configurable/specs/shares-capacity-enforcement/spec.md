# Spec: Shares Capacity Enforcement

Política configurable que determina si el límite de crédito basado en aportaciones cooperativas del socio actúa como restricción obligatoria (hard stop) o como evaluación informativa que no bloquea la aprobación.

---

## ADDED Requirements

### Requirement: EnforceSharesCapacityLimit como política configurable
El sistema SHALL exponer un campo `EnforceSharesCapacityLimit` (boolean) en `UnderwritingPolicy`. Cuando sea `false` (default), la evaluación de aportaciones SHALL reportar su resultado pero NO SHALL rechazar el préstamo. Cuando sea `true`, la evaluación SHALL mantener el comportamiento de hard stop actual.

#### Scenario: Préstamo aprobado cuando enforce es false y aportaciones insuficientes
- **WHEN** `EnforceSharesCapacityLimit = false` y el monto solicitado supera `aportaciones × SharesMultiplierLimit`
- **THEN** la evaluación de `MemberSharesRule` reporta el exceso en su mensaje pero retorna `Pass` con metadata indicando que el límite fue superado, y el préstamo continúa siendo evaluado por las demás reglas

#### Scenario: Préstamo rechazado cuando enforce es true y aportaciones insuficientes
- **WHEN** `EnforceSharesCapacityLimit = true` y el monto solicitado supera `aportaciones × SharesMultiplierLimit`
- **THEN** `MemberSharesRule` actúa como hard stop y retorna `Fail`, rechazando el préstamo independientemente de ingresos o garantías

#### Scenario: Valor por defecto es false
- **WHEN** se crea la fila `default` en `underwriting_policies` sin especificar `enforce_shares_capacity_limit`
- **THEN** el campo toma el valor `false`, preservando el comportamiento permisivo por defecto

### Requirement: Auditoría siempre presente
El sistema SHALL incluir el resultado de `MemberSharesRule` en `evaluationResults` de la respuesta del contrato, independientemente del valor de `EnforceSharesCapacityLimit`. La evaluación MUST siempre ejecutarse y reportarse.

#### Scenario: Resultado informativo visible en respuesta aunque no bloquee
- **WHEN** `EnforceSharesCapacityLimit = false` y las aportaciones son insuficientes para el monto solicitado
- **THEN** `evaluationResults` incluye la entrada de `MemberSharesEvaluation` con `passed: true` y un mensaje que indica el exceso sobre el límite de aportaciones (`"Shares limit exceeded (informative only): ..."`)
