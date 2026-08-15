# Spec: Underwriting Policy (delta)

---

## MODIFIED Requirements

### Requirement: Parámetros de política de suscripción
La política de suscripción SHALL almacenarse en la tabla `underwriting_policies` con `id = 'default'` y SHALL exponer los siguientes campos configurables: `BaseInterestRate`, `AutoDefaultThresholdDays`, `NoScoreBehavior`, `SharesMultiplierLimit`, `RequireActiveMembership`, `GracePeriodDays`, `PenaltyRate`, `OriginationFeeRate`, y `EnforceSharesCapacityLimit`. El campo `EnforceSharesCapacityLimit` MUST tener valor por defecto `false` en la base de datos.

#### Scenario: Política leída con todos sus campos incluyendo enforce_shares_capacity_limit
- **WHEN** el sistema inicia y ejecuta `GetActiveAsync()`
- **THEN** retorna un `UnderwritingPolicy` con todos los campos incluido `EnforceSharesCapacityLimit` mapeado desde la columna `enforce_shares_capacity_limit` de la BD

#### Scenario: Migración agrega columna con default false
- **WHEN** se aplica la migración `20260812_AddEnforceSharesCapacityLimit.sql`
- **THEN** la columna `enforce_shares_capacity_limit BOOLEAN NOT NULL DEFAULT false` existe en `underwriting_policies` y la fila `default` existente toma el valor `false`
