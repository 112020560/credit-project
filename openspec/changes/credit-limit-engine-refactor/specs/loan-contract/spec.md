# Spec: Loan Contract (delta)

---

## REMOVED Requirements

### Requirement: Evaluación crediticia — Límite máximo de préstamo
**Reason**: La regla `MaxLoanAmountRule` contenía un límite absoluto de ₡500,000 hardcodeado sin justificación financiera, bloqueando préstamos legítimos en cualquier sistema real. El techo de monto por producto ya está cubierto por `ProductEligibilityRule` (via `product.maxAmount`). El techo financiero real basado en ingreso es responsabilidad de la nueva `PaymentCapacityRule`. No debe existir ningún límite absoluto hardcodeado en el código.
**Migration**: Ninguna acción requerida. Los límites de monto se configuran por producto en `credit_products.max_amount`. El techo basado en ingreso lo calcula `PaymentCapacityRule`.

---

## MODIFIED Requirements

### Requirement: Evaluación crediticia — Relación Deuda/Ingreso (DTI)
El sistema SHALL calcular el DTI del cliente incluyendo la cuota estimada del nuevo préstamo y rechazar como **hard stop** si supera `policy.MaxDtiRatio`. Sin datos de ingreso, la regla se omite.

Fórmula: `DTI = (deuda_mensual_existente + cuota_estimada_nueva) / ingreso_mensual`

Cuota estimada: calculada con la tasa real del producto (`product.BaseInterestRate ?? policy.BaseInterestRate`) y el plazo solicitado, usando la fórmula de cuota de préstamo francés. No se usa ninguna tasa fija hardcodeada.

Umbrales (basados en `policy.MaxDtiRatio`, default 0.50):
- DTI > `MaxDtiRatio` → **rechazado (hard stop)**
- DTI > `MaxDtiRatio × 0.80` (zona de advertencia, default >40%) → **aprobado con ajuste +2% a la tasa**
- DTI <= zona de advertencia → **aprobado sin ajuste**

#### Scenario: DTI supera el máximo — hard stop
- **WHEN** DTI calculado > `policy.MaxDtiRatio`
- **THEN** la regla falla como hard stop
- **THEN** el resultado indica DTI calculado, ingreso mensual, deuda total y el límite configurado

#### Scenario: DTI en zona de advertencia
- **WHEN** DTI > `policy.MaxDtiRatio × 0.80` y <= `policy.MaxDtiRatio`
- **THEN** la regla pasa con ajuste de tasa **+2%**

#### Scenario: DTI aceptable
- **WHEN** DTI <= `policy.MaxDtiRatio × 0.80`
- **THEN** la regla pasa sin ajuste de tasa

#### Scenario: Sin datos de ingreso
- **WHEN** `MonthlyIncome` es null o cero
- **THEN** la regla se omite sin bloquear ni ajustar la tasa

---

## ADDED Requirements

### Requirement: Evaluación crediticia — Capacidad de pago (PaymentCapacityRule)
El sistema SHALL evaluar si el monto solicitado es financiable desde la cuota máxima posible del solicitante, calculada con valor presente usando la tasa real del producto. Esta regla es un **hard stop** con `Priority = 2`, ejecutada después de `DebtToIncomeRule`.

Ver especificación completa en `openspec/specs/payment-capacity-rule/spec.md`.

#### Scenario: Monto solicitado supera capacidad de pago — hard stop
- **WHEN** el monto solicitado supera el PV calculado desde `cuota_max = ingreso × MaxDtiRatio − deuda_existente`
- **THEN** la regla falla como hard stop indicando el monto máximo financiable

#### Scenario: Sin ingreso — regla omitida
- **WHEN** `MonthlyIncome` es null o cero
- **THEN** la regla se omite sin bloquear
