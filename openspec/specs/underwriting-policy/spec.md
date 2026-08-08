# Spec: Underwriting Policy

Configuración de las políticas de underwriting del Credit System almacenada en base de datos y cargada como un objeto de dominio. Elimina valores hardcodeados en el código y permite ajustar las políticas sin redeployment.

---

## Requirements

### Requirement: UnderwritingPolicy encapsula las políticas de evaluación crediticia
El sistema SHALL disponer de un record `UnderwritingPolicy` en el domain layer que encapsula los parámetros configurables del motor de evaluación: tasa base de interés, umbral de días para auto-default, y comportamiento cuando no hay credit score disponible.

Campos:
- `BaseInterestRate` (`decimal`): tasa anual base sobre la que se acumulan los ajustes de las reglas. Valor actual: 8.0
- `AutoDefaultThresholdDays` (`int`): días de atraso para disparar auto-default. Valor actual: 90
- `NoScoreBehavior` (`NoScoreBehavior` enum): `ApproveWithPenalty` | `Reject`. Valor actual: `ApproveWithPenalty`

#### Scenario: Política cargada correctamente desde base de datos
- **WHEN** la aplicación inicia y resuelve `IUnderwritingPolicyRepository`
- **THEN** retorna un `UnderwritingPolicy` con los valores de la tabla `underwriting_policies`

#### Scenario: Política con valores de seed por defecto
- **WHEN** la tabla `underwriting_policies` tiene el registro seed inicial
- **THEN** `BaseInterestRate = 8.0`, `AutoDefaultThresholdDays = 90`, `NoScoreBehavior = ApproveWithPenalty`

---

### Requirement: ContractEngine usa UnderwritingPolicy en lugar de constantes
`ContractEngine` SHALL recibir `UnderwritingPolicy` como dependencia inyectable y usar `policy.BaseInterestRate` en lugar de la constante `BaseInterestRate = 8.0m`.

#### Scenario: Cálculo de tasa final con política configurada
- **WHEN** el engine aprueba un contrato con ajuste de tasa de +3.0%
- **THEN** la tasa final = `policy.BaseInterestRate + rateAdjustment`
- **THEN** si `BaseInterestRate` cambia en la política, el cálculo refleja el nuevo valor sin cambio de código

---

### Requirement: CreditScoreRule respeta la política de comportamiento sin score
`CreditScoreRule` SHALL consultar `UnderwritingPolicy.NoScoreBehavior` para determinar qué hacer cuando `CreditScore` no está disponible en el contexto.

#### Scenario: Sin score y política = ApproveWithPenalty
- **WHEN** `context.CreditScore == null` y `policy.NoScoreBehavior == ApproveWithPenalty`
- **THEN** la regla pasa con un ajuste de tasa de penalización (ej: +5.0%) y mensaje explicativo

#### Scenario: Sin score y política = Reject
- **WHEN** `context.CreditScore == null` y `policy.NoScoreBehavior == Reject`
- **THEN** la regla falla con mensaje "Credit score required — policy rejects applications without score"

---

### Requirement: LoanContractAggregate usa umbral de auto-default de la política
`LoanContractAggregate.RecordMissedPayment` SHALL recibir el umbral de días para auto-default como parámetro en lugar de usar la constante interna de 90 días.

#### Scenario: Auto-default con umbral configurable
- **WHEN** se registra un pago perdido con `daysOverdue >= policy.AutoDefaultThresholdDays`
- **THEN** el aggregate emite `ContractDefaulted` automáticamente

#### Scenario: Umbral cambiado a 60 días
- **WHEN** la política tiene `AutoDefaultThresholdDays = 60`
- **THEN** un pago con 61 días de atraso dispara auto-default sin cambio de código

---

### Requirement: Tabla underwriting_policies en PostgreSQL
El sistema SHALL tener una tabla `underwriting_policies` con un registro activo que almacena los valores de configuración. El script de migración incluye el INSERT del seed con los valores actuales del sistema.

#### Scenario: Migración aplicada exitosamente
- **WHEN** se ejecuta el script `20260624_AddUnderwritingPoliciesTable.sql`
- **THEN** existe la tabla `underwriting_policies` con columnas `id`, `base_interest_rate`, `auto_default_threshold_days`, `no_score_behavior`, `created_at`, `updated_at`
- **THEN** existe al menos un registro con `id = 'default'` y los valores seed
