## MODIFIED Requirements

### Requirement: Aplicación de pagos
El sistema SHALL aplicar pagos a préstamos en estado `Active` o `Delinquent`, distribuyendo el monto en orden: **fees pendientes → interés moratorio acumulado → interés corriente acumulado → principal**.

Si el pago cubre completamente el saldo restante (principal + todos los intereses y fees), el sistema emite automáticamente `ContractPaidOff`.

#### Scenario: Pago con interés moratorio pendiente
- **WHEN** se aplica un pago y `AccruedPenaltyInterest > 0`
- **THEN** el pago cubre primero `TotalFees`, luego `AccruedPenaltyInterest`, luego `AccruedInterest`, luego reduce el principal
- **THEN** se emite `PaymentApplied` con `PenaltyInterestPaid` en el breakdown

#### Scenario: Pago sin interés moratorio — orden sin cambio efectivo
- **WHEN** `AccruedPenaltyInterest = 0`
- **THEN** el orden es equivalente al anterior: fees → interés corriente → capital

#### Scenario: Pago cubre saldo completo — payoff automático
- **WHEN** el pago cubre `TotalFees + AccruedPenaltyInterest + AccruedInterest + CurrentBalance`
- **THEN** se emite `PaymentApplied` seguido automáticamente de `ContractPaidOff`
- **THEN** el contrato transita a estado `PaidOff` con todos los saldos en cero

#### Scenario: Moneda incorrecta rechazada
- **WHEN** el pago se realiza en moneda distinta a la del contrato
- **THEN** el sistema lanza `DomainException` indicando la moneda esperada

#### Scenario: Pago en contrato no pagable rechazado
- **WHEN** el contrato está en estado `Default` o `PaidOff`
- **THEN** el sistema lanza `DomainException` indicando que el contrato no está en estado pagable

---

### Requirement: Creación del contrato y generación del schedule
El sistema SHALL crear el `LoanContractAggregate` con el schedule de amortización, la comisión de originación calculada, emitir el evento `ContractCreated` y persistirlo en el event store.

La comisión de originación se calcula como: `OriginationFee = Amount × (effectiveOriginationFeeRate / 100)`, donde `effectiveOriginationFeeRate = product.OriginationFeeRate ?? policy.OriginationFeeRate`.

La comisión se registra como `TotalFees` inicial del contrato. El contrato se crea en estado **Approved**.

#### Scenario: Creación con comisión de originación configurada
- **WHEN** se aprueba un contrato para un producto con `OriginationFeeRate = 1.5` y `Amount = 1,000,000`
- **THEN** `OriginationFee = 15,000`
- **THEN** el evento `ContractCreated` incluye el campo `OriginationFee = 15,000`
- **THEN** `TotalFees` arranca en `15,000` en el estado del agregado

#### Scenario: Creación sin comisión de originación
- **WHEN** `effectiveOriginationFeeRate = 0`
- **THEN** `OriginationFee = 0`, `TotalFees` arranca en 0 (comportamiento actual sin regresión)

#### Scenario: Schedule persistido como parte del evento
- **WHEN** se crea el contrato
- **THEN** el `PaymentSchedule` completo con todas las `AmortizationEntry` queda registrado en el evento `ContractCreated`

---

## ADDED Requirements

### Requirement: Acumulación de interés moratorio
El sistema SHALL acumular interés moratorio (`AccruedPenaltyInterest`) en el agregado cuando se registra un pago perdido. El interés moratorio se calcula sobre el capital de la cuota vencida por el número de días de mora.

Fórmula: `penaltyInterest = overduePrincipal × (effectivePenaltyRate / 100 / 365) × daysOverdue`

`AccruedPenaltyInterest` es un campo separado de `AccruedInterest` en `LoanContractState`. Se acumula con cada `PaymentMissed` adicional. Se resetea a cero tras reestructuración o payoff.

#### Scenario: Pago perdido con tasa moratoria configurada
- **WHEN** se registra `PaymentMissed` con `penaltyRate = 15.0`, capital vencido de `50,000` y `daysOverdue = 30`
- **THEN** `penaltyInterest = 50,000 × (15.0/100/365) × 30 ≈ 616.44`
- **THEN** `AccruedPenaltyInterest` se incrementa en ese monto

#### Scenario: Pago perdido sin tasa moratoria configurada
- **WHEN** `effectivePenaltyRate = 0`
- **THEN** `penaltyInterest = 0`, `AccruedPenaltyInterest` no cambia

#### Scenario: AccruedPenaltyInterest visible en read model
- **WHEN** se consulta el resumen del préstamo
- **THEN** `LoanSummaryReadModel` incluye `AccruedPenaltyInterest` actualizado
