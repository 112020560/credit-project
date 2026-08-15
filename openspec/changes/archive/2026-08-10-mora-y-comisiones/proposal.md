## Why

El sistema aplica mora inmediatamente al día siguiente del vencimiento, no distingue entre el cargo fijo de mora y el interés moratorio sobre saldo vencido, y no cobra comisión de originación al formalizar el préstamo. Las tres omisiones son incumplimientos regulatorios para una cooperativa supervisada por SUGEF y generan pérdida de ingresos financieros reales.

## What Changes

- **Período de gracia**: mover `GracePeriodDays` de `LateFeeConfiguration` a `UnderwritingPolicy`; el `PaymentMissedJob` SHALL respetar el período de gracia antes de registrar un `PaymentMissed`; si `loan.DaysOverdue <= GracePeriodDays` el préstamo se omite en esa ejecución
- **Tasa moratoria**: agregar `PenaltyRate` (decimal, tasa anual en %) a `UnderwritingPolicy` y a `CreditProduct` (con fallback a política global); acumular interés moratorio en `AccruedPenaltyInterest` en el agregado y en la tabla de estado; actualizar `ApplyPayment` para aplicar en orden: fees → interés moratorio → interés corriente → capital
- **Comisión de originación**: agregar `OriginationFeeRate` (decimal, %) a `UnderwritingPolicy` y a `CreditProduct`; calcular `OriginationFee = Amount × effectiveOriginationFeeRate` al crear el contrato; registrar como `TotalFees` inicial en el agregado; incluir en `ContractCreated` y en `LoanSummaryReadModel`
- **BREAKING**: orden de aplicación de pagos cambia de `fees → interés → capital` a `fees → interés moratorio → interés corriente → capital`
- Migración SQL: columnas `grace_period_days`, `penalty_rate`, `origination_fee_rate` en `underwriting_policies`; columnas `penalty_rate`, `origination_fee_rate` en `credit_products`

## Capabilities

### New Capabilities

_(ninguna — todas son modificaciones a capacidades existentes)_

### Modified Capabilities

- `underwriting-policy`: agregar `GracePeriodDays`, `PenaltyRate`, `OriginationFeeRate` al record; actualizar tabla SQL; mover `GracePeriodDays` desde `LateFeeConfiguration`
- `loan-contract`: período de gracia en detección de mora; `AccruedPenaltyInterest` en el agregado; nuevo orden de aplicación de pagos; comisión de originación en `ContractCreated` y read model

## Impact

- **Domain**: `UnderwritingPolicy` (3 nuevos campos), `LoanContractState` (+`AccruedPenaltyInterest`, +`OriginationFee`), `LoanContractAggregate.ApplyPayment` (nuevo orden), `ContractCreated` event (+`OriginationFee`)
- **Application**: `PaymentMissedJob` (grace period check), `CreateContractCommandHandler` (calcular y pasar `OriginationFee`)
- **Infrastructure**: `CreditProduct` entity (+2 campos), `CreditProductRepository`, `UnderwritingPolicyRepository`, 1 migración SQL ALTER TABLE
- **Read Models**: `LoanSummaryReadModel` (+`OriginationFee`, +`AccruedPenaltyInterest`)
- **Tests**: `LoanContractAggregateTests` (nuevo orden de pagos, penalidad), `PaymentMissedJobTests` (grace period)
