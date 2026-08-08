# Spec: Domain Model Encapsulation

Correcciones de encapsulación en aggregates, eventos de dominio y contextos de evaluación para garantizar que los invariantes del modelo no puedan ser violados desde fuera del dominio.

---

## Requirements

### Requirement: RevolvingCreditAggregate solo se crea via factory method
`RevolvingCreditAggregate` SHALL tener su constructor vacío como `private` para que ningún código externo pueda instanciar el aggregate directamente sin pasar por el factory method `RevolvingCreditAggregate.Create(...)`.

#### Scenario: Constructor directo bloqueado
- **WHEN** código externo al aggregate intenta `new RevolvingCreditAggregate()`
- **THEN** el compilador rechaza la llamada

#### Scenario: Creación valida via factory method
- **WHEN** se llama a `RevolvingCreditAggregate.Create(customerId, creditLimit, rate, ...)`
- **THEN** se retorna una instancia en estado `Pending` con todos los invariantes satisfechos

---

### Requirement: IDomainEvent y DomainEvent en namespace compartido
`IDomainEvent` y `DomainEvent` SHALL residir en `CreditSystem.Domain.Abstractions.Events` en lugar de `CreditSystem.Domain.Aggregates.LoanContract.Events.Base`. Ambos aggregates (`LoanContractAggregate` y `RevolvingCreditAggregate`) y todos los eventos de dominio referencian el namespace compartido.

#### Scenario: Namespace correcto en eventos de LoanContract
- **WHEN** se examina cualquier archivo en `LoanContract/Events/`
- **THEN** el `using` referencia `CreditSystem.Domain.Abstractions.Events`
- **THEN** no existe referencia a `CreditSystem.Domain.Aggregates.LoanContract.Events.Base`

#### Scenario: Namespace correcto en eventos de RevolvingCredit
- **WHEN** se examina cualquier archivo en `RevolvingCredit/Events/`
- **THEN** el `using` referencia `CreditSystem.Domain.Abstractions.Events`

---

### Requirement: ContractEvaluationContext usa Money para valores financieros
Los campos financieros de `ContractEvaluationContext` SHALL usar el value object `Money` en lugar de `decimal`/`decimal?` para garantizar validación de moneda, redondeo y comportamiento encapsulado.

Campos afectados:
- `RequestedAmount`: `decimal` → `Money`
- `CollateralValue`: `decimal?` → `Money?`
- `MonthlyIncome`: `decimal?` → `Money?`
- `MonthlyDebt`: `decimal?` → `Money?`

#### Scenario: Construcción de contexto con Money
- **WHEN** el handler construye un `ContractEvaluationContext`
- **THEN** `RequestedAmount` es un `Money` con currency y monto validados
- **THEN** las reglas de `ContractEngine` acceden a `.Amount` vía el value object

#### Scenario: Reglas de evaluación compatibles con Money
- **WHEN** `DebtToIncomeRule`, `MaxLoanAmountRule` y `CollateralRule` leen los campos del contexto
- **THEN** acceden correctamente a `context.RequestedAmount.Amount` en lugar de `context.RequestedAmount` directamente
