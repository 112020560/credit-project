## MODIFIED Requirements

### Requirement: Evaluación crediticia — Colateral
El sistema SHALL evaluar el colateral efectivo provisto (calculado desde las garantías del contrato) y ajustar la tasa de interés según la cobertura. El colateral es opcional; su ausencia no rechaza el préstamo.

`context.CollateralValue` contiene el valor efectivo de cobertura calculado como `Σ(AppraisalValue × CoverageRate)` de las garantías provistas. Este valor es null cuando no se proveen garantías.

Ajustes según ratio de colateral (`context.CollateralValue / monto_solicitado`):
- Sin colateral (`CollateralValue` null o cero) → préstamo no garantizado, ajuste **+1%**
- Ratio < 1.0 → garantía parcial, ajuste **+0.5%**
- Ratio >= 1.0 y < 1.2 → garantizado, descuento **-0.5%**
- Ratio >= 1.2 → bien garantizado, descuento **-1.5%**

#### Scenario: Sin garantías provistas
- **WHEN** `context.CollateralValue` es null o cero
- **THEN** la regla pasa con ajuste de tasa **+1%** (préstamo no garantizado)

#### Scenario: Colateral efectivo parcial
- **WHEN** `context.CollateralValue > 0` y `context.CollateralValue / Amount < 1.0`
- **THEN** la regla pasa con ajuste de tasa **+0.5%**

#### Scenario: Colateral efectivo suficiente
- **WHEN** `context.CollateralValue / Amount >= 1.0` y `< 1.2`
- **THEN** la regla pasa con descuento de tasa **-0.5%**

#### Scenario: Colateral efectivo excelente
- **WHEN** `context.CollateralValue / Amount >= 1.2`
- **THEN** la regla pasa con descuento de tasa **-1.5%**
