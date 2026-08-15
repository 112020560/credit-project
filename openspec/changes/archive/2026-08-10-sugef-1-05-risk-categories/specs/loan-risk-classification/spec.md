## ADDED Requirements

### Requirement: Tabla de categorías de riesgo SUGEF 1-05
El sistema SHALL definir las categorías de riesgo según la normativa SUGEF 1-05 Tabla 1 como constantes inmutables del dominio. Las categorías y sus porcentajes mínimos de estimación son:

| Categoría | Días de mora | Estimación mínima |
|-----------|-------------|-------------------|
| A1        | 0           | 0%                |
| A2        | 1–30        | 0.5%              |
| B1        | 31–60       | 5%                |
| B2        | 61–90       | 10%               |
| C1        | 91–120      | 25%               |
| C2        | 121–180     | 50%               |
| D         | 181–360     | 75%               |
| E         | > 360       | 100%              |

#### Scenario: Categoría A1 para préstamo al día
- **WHEN** un préstamo tiene 0 días de mora
- **THEN** su categoría es A1 y la estimación mínima es 0%

#### Scenario: Categoría A2 para mora de 1 a 30 días
- **WHEN** un préstamo tiene entre 1 y 30 días de mora (inclusive)
- **THEN** su categoría es A2 y la estimación mínima es 0.5%

#### Scenario: Categoría E para mora mayor a 360 días
- **WHEN** un préstamo tiene más de 360 días de mora
- **THEN** su categoría es E y la estimación mínima es 100%

---

### Requirement: Cálculo de estimación de provisión
El sistema SHALL calcular la estimación mínima de provisión de cada préstamo como:

`EstimatedProvision = CurrentBalance × (ProvisionPercentage / 100)`

El cálculo usa el saldo actual (`current_balance`) del préstamo en el momento de la clasificación.

#### Scenario: Provisión proporcional al saldo
- **WHEN** un préstamo tiene saldo de 10,000 y categoría B1 (5%)
- **THEN** la estimación es 500

#### Scenario: Provisión cero para categoría A1
- **WHEN** un préstamo está en categoría A1 (0%)
- **THEN** la estimación es 0 independientemente del saldo

#### Scenario: Provisión total para categoría E
- **WHEN** un préstamo está en categoría E (100%)
- **THEN** la estimación es igual al saldo actual (provisión total)

---

### Requirement: Clasificación automática diaria
El sistema SHALL reclasificar automáticamente todos los préstamos con estado `Active`, `Delinquent` o `Default` cada noche, calculando los días de mora como `NOW() - next_payment_date` directamente en la consulta.

Si la categoría cambia respecto a la almacenada, el sistema SHALL emitir el evento `LoanRiskCategoryChanged` y actualizar `rm_loan_summaries`.

#### Scenario: Reclasificación detecta cambio de categoría
- **WHEN** el job corre y un préstamo tiene 32 días de mora (categoría B1) pero tenía A2 almacenado
- **THEN** se actualiza la categoría a B1, se recalcula la estimación y se emite `LoanRiskCategoryChanged`

#### Scenario: Sin cambio no emite evento
- **WHEN** el job corre y la categoría calculada coincide con la almacenada
- **THEN** no se emite `LoanRiskCategoryChanged` ni se escribe en la tabla

#### Scenario: Préstamos pagados o aprobados sin desembolsar no se reclasifican
- **WHEN** el job corre sobre préstamos con status `PaidOff` o `Approved`
- **THEN** estos préstamos son ignorados; su categoría no se actualiza

---

### Requirement: Reclasificación manual por analista (solo degradación)
El sistema SHALL permitir que un analista asigne manualmente una categoría de riesgo a un préstamo, con la restricción de que solo se permite asignar una categoría igual o peor (más riesgosa) que la categoría automática actual.

#### Scenario: Degradación manual permitida
- **WHEN** un préstamo está en categoría A2 automáticamente y el analista solicita asignar B1
- **THEN** la categoría se actualiza a B1 y se registra como clasificación manual

#### Scenario: Mejora manual rechazada
- **WHEN** un préstamo está en categoría C1 y el analista solicita asignar A2
- **THEN** el sistema rechaza la operación con error 422 indicando que la mejora manual no está permitida

#### Scenario: Misma categoría manual permitida
- **WHEN** el analista asigna la misma categoría que ya tiene el préstamo
- **THEN** la operación se acepta sin emitir evento de cambio

---

### Requirement: Evento LoanRiskCategoryChanged
El sistema SHALL emitir el evento `LoanRiskCategoryChanged` cada vez que la categoría de riesgo de un préstamo cambie (ya sea por clasificación automática o manual). El evento SHALL contener: `LoanId`, `PreviousCategory`, `NewCategory`, `DaysOverdue`, `EstimatedProvision`, `ClassifiedAt`, `IsManual`.

#### Scenario: Evento contiene categoría anterior y nueva
- **WHEN** la categoría de un préstamo cambia de A2 a B1
- **THEN** el evento `LoanRiskCategoryChanged` contiene `PreviousCategory = A2`, `NewCategory = B1`

#### Scenario: Clasificación inicial emite evento con categoría anterior nula
- **WHEN** un préstamo se clasifica por primera vez (sin categoría previa)
- **THEN** el evento `LoanRiskCategoryChanged` contiene `PreviousCategory = null`
