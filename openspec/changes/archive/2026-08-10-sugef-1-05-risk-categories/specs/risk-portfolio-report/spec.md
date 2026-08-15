## ADDED Requirements

### Requirement: Reporte de cartera por categoría de riesgo
El sistema SHALL exponer un endpoint `GET /api/loans/risk-summary` que devuelva el resumen de la cartera de préstamos agrupado por categoría de riesgo SUGEF 1-05.

La respuesta SHALL incluir por cada categoría presente en la cartera:
- `category`: nombre de la categoría (A1, A2, B1, B2, C1, C2, D, E)
- `loanCount`: cantidad de préstamos en esa categoría
- `totalBalance`: suma de saldos actuales
- `totalProvision`: suma de estimaciones calculadas
- `provisionPercentage`: porcentaje de estimación de la categoría según SUGEF

La respuesta SHALL también incluir totales globales: `totalLoans`, `totalPortfolioBalance`, `totalRequiredProvision`.

Solo se incluyen préstamos con status `Active`, `Delinquent` o `Default` (cartera vigente y en mora).

#### Scenario: Resumen muestra solo categorías con préstamos
- **WHEN** no hay préstamos en categoría E
- **THEN** la categoría E no aparece en la respuesta

#### Scenario: Totales globales cuadran con suma de categorías
- **WHEN** se solicita el resumen
- **THEN** `totalLoans` es igual a la suma de `loanCount` de todas las categorías

#### Scenario: Cartera vacía devuelve totales en cero
- **WHEN** no hay préstamos activos en el sistema
- **THEN** el endpoint retorna 200 con `totalLoans = 0`, `totalPortfolioBalance = 0`, `totalRequiredProvision = 0` y lista de categorías vacía

---

### Requirement: Detalle de préstamos por categoría
El sistema SHALL exponer un endpoint `GET /api/loans/risk-summary/{category}` que devuelva la lista de préstamos clasificados en una categoría específica.

La respuesta SHALL incluir por cada préstamo: `loanId`, `customerId`, `customerName`, `currentBalance`, `estimatedProvision`, `daysOverdue`, `riskCategory`.

#### Scenario: Categoría inválida devuelve 400
- **WHEN** se solicita `/api/loans/risk-summary/Z99`
- **THEN** el sistema retorna 400 indicando que la categoría no es válida

#### Scenario: Categoría válida sin préstamos devuelve lista vacía
- **WHEN** se solicita `/api/loans/risk-summary/E` y no hay préstamos en categoría E
- **THEN** el sistema retorna 200 con lista vacía
