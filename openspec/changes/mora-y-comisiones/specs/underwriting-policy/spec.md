## MODIFIED Requirements

### Requirement: UnderwritingPolicy encapsula las políticas de evaluación crediticia
El sistema SHALL disponer de un record `UnderwritingPolicy` en el domain layer que encapsula los parámetros configurables del motor de evaluación: tasa base de interés, umbral de días para auto-default, comportamiento cuando no hay credit score, límite de aportaciones de socio, membresía requerida, período de gracia, tasa moratoria y comisión de originación.

Campos:
- `BaseInterestRate` (`decimal`): tasa anual base sobre la que se acumulan los ajustes de las reglas. Valor actual: 8.0
- `AutoDefaultThresholdDays` (`int`): días de atraso para disparar auto-default. Valor actual: 90
- `NoScoreBehavior` (`NoScoreBehavior` enum): `ApproveWithPenalty` | `Reject`. Valor actual: `ApproveWithPenalty`
- `SharesMultiplierLimit` (`int`): multiplicador de aportaciones para límite de préstamo. Valor actual: 5
- `RequireActiveMembership` (`bool`): si se requiere membresía activa para solicitar préstamo. Valor actual: false
- `GracePeriodDays` (`int`): días de tolerancia después del vencimiento antes de registrar mora. Valor actual: 5. **Reemplaza** el campo homónimo de `LateFeeConfiguration`
- `PenaltyRate` (`decimal`): tasa moratoria anual en porcentaje (ej: 15.0 = 15%) que se aplica sobre el saldo vencido por día durante la mora. Valor actual: 0.0 (sin interés moratorio)
- `OriginationFeeRate` (`decimal`): comisión de originación como porcentaje del monto del préstamo (ej: 1.5 = 1.5%). Valor actual: 0.0 (sin comisión)

#### Scenario: Política cargada correctamente desde base de datos
- **WHEN** la aplicación inicia y resuelve `IUnderwritingPolicyRepository`
- **THEN** retorna un `UnderwritingPolicy` con los valores de la tabla `underwriting_policies` incluyendo `GracePeriodDays`, `PenaltyRate` y `OriginationFeeRate`

#### Scenario: Política con valores de seed por defecto
- **WHEN** la tabla `underwriting_policies` tiene el registro seed inicial
- **THEN** `BaseInterestRate = 8.0`, `AutoDefaultThresholdDays = 90`, `NoScoreBehavior = ApproveWithPenalty`, `GracePeriodDays = 5`, `PenaltyRate = 0.0`, `OriginationFeeRate = 0.0`

---

### Requirement: Tabla underwriting_policies en PostgreSQL
El sistema SHALL tener una tabla `underwriting_policies` con un registro activo que almacena los valores de configuración incluyendo los nuevos campos de mora y comisiones.

#### Scenario: Migración aplicada exitosamente
- **WHEN** se ejecuta el script `20260809_AddMoraYComisionesColumns.sql`
- **THEN** la tabla `underwriting_policies` incluye columnas `grace_period_days` (INT DEFAULT 5), `penalty_rate` (NUMERIC DEFAULT 0), `origination_fee_rate` (NUMERIC DEFAULT 0)
- **THEN** la tabla `credit_products` incluye columnas `penalty_rate` (NUMERIC, nullable) y `origination_fee_rate` (NUMERIC, nullable)

---

## ADDED Requirements

### Requirement: CreditProduct soporta configuración de mora y comisión propias
El sistema SHALL permitir configurar `PenaltyRate` y `OriginationFeeRate` por producto de crédito. Si el valor del producto es null, se usa el valor de `UnderwritingPolicy` como fallback.

#### Scenario: Producto con tasa moratoria propia
- **WHEN** `product.PenaltyRate = 18.0` y `policy.PenaltyRate = 15.0`
- **THEN** el sistema usa `18.0` como tasa moratoria para ese contrato

#### Scenario: Producto sin tasa moratoria — fallback a política
- **WHEN** `product.PenaltyRate = null` y `policy.PenaltyRate = 15.0`
- **THEN** el sistema usa `15.0` como tasa moratoria para ese contrato

#### Scenario: Producto con comisión de originación propia
- **WHEN** `product.OriginationFeeRate = 2.0` y `policy.OriginationFeeRate = 1.0`
- **THEN** el sistema usa `2.0` para calcular la comisión al crear el contrato

### Requirement: PaymentMissedJob respeta el período de gracia
El sistema SHALL omitir el registro de pago perdido para préstamos cuyo retraso no supera `policy.GracePeriodDays`. Los préstamos dentro del período de gracia NO reciben `PaymentMissed` ni cargo por mora.

#### Scenario: Préstamo dentro del período de gracia — omitido
- **WHEN** `loan.DaysOverdue <= policy.GracePeriodDays`
- **THEN** el job omite ese préstamo sin emitir `PaymentMissed`

#### Scenario: Préstamo fuera del período de gracia — procesado
- **WHEN** `loan.DaysOverdue > policy.GracePeriodDays`
- **THEN** el job procesa el préstamo y emite `PaymentMissed` con el cargo correspondiente
