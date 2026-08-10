## ADDED Requirements

### Requirement: ProductEligibilityRule valida que el contrato cumpla los parámetros del producto
El sistema SHALL evaluar cada solicitud de contrato contra los parámetros del `CreditProduct` seleccionado antes de ejecutar las reglas de riesgo generales. Esta regla es un **hard stop** con `Priority = 1` (después de `MemberSharesRule` con Priority 0 y antes de las reglas de riesgo con Priority 2+).

Validaciones en orden:
1. Monto solicitado dentro del rango `[ProductLimits.MinAmount, ProductLimits.MaxAmount]`
2. Plazo solicitado dentro del rango `[ProductLimits.MinTermMonths, ProductLimits.MaxTermMonths]`
3. Si `RequiresCollateral = true`: el contrato MUST proveer `CollateralValue > 0`
4. Si `RequiresCollateral = true` y `MaxLtv` está definido: `CollateralValue / RequestedAmount >= (1 / MaxLtv)` — equivalente a `LTV = Amount / Collateral <= MaxLtv`

#### Scenario: Contrato dentro del rango del producto — pasa la regla
- **WHEN** el monto y plazo están dentro de los límites del producto y el colateral cumple los requisitos
- **THEN** la regla pasa sin ajuste de tasa
- **THEN** la evaluación continúa con las reglas de riesgo generales

#### Scenario: Monto fuera del rango del producto — hard stop
- **WHEN** `RequestedAmount < ProductLimits.MinAmount` o `RequestedAmount > ProductLimits.MaxAmount`
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica el rango permitido por el producto
- **THEN** no se ejecutan reglas adicionales

#### Scenario: Plazo fuera del rango del producto — hard stop
- **WHEN** `TermMonths < ProductLimits.MinTermMonths` o `TermMonths > ProductLimits.MaxTermMonths`
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica el rango de plazo permitido

#### Scenario: Producto requiere colateral pero el contrato no lo provee — hard stop
- **WHEN** `CreditProduct.RequiresCollateral = true` y `CollateralValue` es null o cero
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica que el producto requiere colateral

#### Scenario: LTV excede el máximo permitido por el producto — hard stop
- **WHEN** `RequiresCollateral = true`, `MaxLtv` está definido, y `Amount / CollateralValue > MaxLtv`
- **THEN** la regla falla con hard stop
- **THEN** el mensaje indica el LTV calculado y el máximo permitido

#### Scenario: Producto no requiere colateral — colateral opcional
- **WHEN** `CreditProduct.RequiresCollateral = false`
- **THEN** la ausencia de `CollateralValue` no genera fallo en `ProductEligibilityRule`
- **THEN** el colateral (si se provee) sigue siendo evaluado por `CollateralRule` para ajuste de tasa

---

### Requirement: ProductEligibilityRule lee el CreditProduct desde el contexto de evaluación
`ProductEligibilityRule` SHALL obtener el `CreditProduct` desde `ContractEvaluationContext.Product`, siguiendo el mismo patrón que `MemberSharesRule` lee `IsActiveMember` y `MemberSharesAmount`. El handler enriquece el contexto antes de pasar al engine; la regla solo consume el contexto.

#### Scenario: Contexto sin CreditProduct — regla se omite
- **WHEN** `ContractEvaluationContext.Product` es null
- **THEN** la regla se omite (pasa sin bloqueo)
- **THEN** se registra metadata `{ "Skipped": true }` en el resultado de la regla

#### Scenario: Priority es 1
- **WHEN** se instancia `ProductEligibilityRule`
- **THEN** `rule.Priority == 1`
- **THEN** se ejecuta después de `MemberSharesRule` (Priority 0) y antes de `MaxLoanAmountRule` y demás reglas de riesgo
