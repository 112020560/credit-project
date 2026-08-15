# Spec: Underwriting Policy (delta)

---

## MODIFIED Requirements

### Requirement: UnderwritingPolicy encapsula las políticas de evaluación crediticia
El sistema SHALL disponer de un record `UnderwritingPolicy` en el domain layer que encapsula los parámetros configurables del motor de evaluación: tasa base de interés, umbral de días para auto-default, comportamiento cuando no hay credit score disponible, y ratio máximo de deuda/ingreso (DTI).

Campos:
- `BaseInterestRate` (`decimal`): tasa anual base sobre la que se acumulan los ajustes de las reglas. Valor actual: 8.0
- `AutoDefaultThresholdDays` (`int`): días de atraso para disparar auto-default. Valor actual: 90
- `NoScoreBehavior` (`NoScoreBehavior` enum): `ApproveWithPenalty` | `Reject`. Valor actual: `ApproveWithPenalty`
- `MaxDtiRatio` (`decimal`): ratio máximo de deuda/ingreso permitido. Valor por defecto: `0.50` (50%). Las reglas `DebtToIncomeRule` y `PaymentCapacityRule` SHALL leer este valor en lugar de usar constantes internas.

#### Scenario: Política cargada correctamente desde base de datos incluyendo MaxDtiRatio
- **WHEN** la aplicación inicia y resuelve `IUnderwritingPolicyRepository`
- **THEN** retorna un `UnderwritingPolicy` con todos los valores de la tabla `underwriting_policies`, incluyendo `max_dti_ratio`

#### Scenario: MaxDtiRatio por defecto es 0.50
- **WHEN** la columna `max_dti_ratio` tiene el valor seed
- **THEN** `MaxDtiRatio = 0.50`

#### Scenario: Migración agrega columna max_dti_ratio
- **WHEN** se aplica la migración `20260812_AddMaxDtiRatio.sql`
- **THEN** la columna `max_dti_ratio NUMERIC(4,2) NOT NULL DEFAULT 0.50` existe en `underwriting_policies`
- **THEN** la fila `default` existente toma el valor `0.50` sin necesidad de UPDATE
