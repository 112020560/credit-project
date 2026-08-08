## ADDED Requirements

### Requirement: MemberSharesRule evalúa el límite de crédito por aportaciones
El sistema SHALL disponer de una regla de underwriting `MemberSharesRule` en `CreditSystem.Domain/Rules/Implementations/` que evalúa si el monto solicitado supera el límite máximo de crédito derivado de las aportaciones del socio.

`MemberSharesRule` SHALL implementar `IContractRule` e `IHardStopRule`. Si el monto solicitado supera `TotalSharesAmount × SharesMultiplierLimit`, la evaluación se detiene inmediatamente.

Si `MemberSharesAmount` es `null` o `0` en el contexto (socio sin aportaciones registradas), la regla SHALL omitirse sin bloquear — para no afectar cooperativas en proceso de migración de datos.

Si `IsActiveMember` es `false` o `null` en el contexto y `RequireActiveMembership = true` en la política, la regla SHALL fallar con hard stop indicando que la membresía no está activa.

#### Scenario: Monto dentro del límite por aportaciones
- **WHEN** `RequestedAmount.Amount <= context.MemberSharesAmount.Amount × policy.SharesMultiplierLimit`
- **THEN** la regla pasa sin ajuste de tasa
- **THEN** los metadatos incluyen `SharesAmount`, `MultiplierLimit` y `MaxAllowedAmount`

#### Scenario: Monto supera el límite por aportaciones — hard stop
- **WHEN** `RequestedAmount.Amount > context.MemberSharesAmount.Amount × policy.SharesMultiplierLimit`
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica el monto máximo permitido y las aportaciones actuales del socio

#### Scenario: Socio sin aportaciones registradas — regla omitida
- **WHEN** `context.MemberSharesAmount` es `null` o su monto es `0`
- **THEN** la regla pasa con metadato `Skipped = true` y mensaje explicativo
- **THEN** no se aplica ajuste de tasa ni hard stop

#### Scenario: Membresía no activa con política restrictiva — hard stop
- **WHEN** `context.IsActiveMember == false` y `policy.RequireActiveMembership == true`
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica "Active cooperative membership required to apply for credit"

#### Scenario: Membresía no activa con política permisiva — regla omitida
- **WHEN** `context.IsActiveMember == false` y `policy.RequireActiveMembership == false`
- **THEN** la regla pasa con metadato `Skipped = true`

---

### Requirement: MemberSharesRule tiene prioridad sobre las demás reglas
`MemberSharesRule` SHALL tener `Priority = 0` para evaluarse **antes** que cualquier otra regla del motor. Si el solicitante no es socio activo o supera su límite de aportaciones, no tiene sentido evaluar score, DTI ni colateral.

#### Scenario: MemberSharesRule se ejecuta primero
- **WHEN** el motor evalúa un contrato con todas las reglas registradas
- **THEN** `MemberSharesRule` se evalúa antes de `CreditScoreRule` (Priority 1), `MaxLoanAmountRule`, `DebtToIncomeRule`, `CollateralRule` y `ActiveLoansRule`
