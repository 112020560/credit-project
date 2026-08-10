## Why

Las cooperativas costarricenses ofrecen préstamos a tasa variable vinculada a la Tasa Básica Pasiva (TBP) del BCCR, que se actualiza mensualmente. El sistema actual solo soporta tasa fija — un contrato de préstamo tiene `AnnualRate` fijo desde la creación y nunca se actualiza salvo reestructuración. Esto impide ofrecer productos crediticios variables, que representan una parte importante de la cartera real de una cooperativa.

## What Changes

- Nuevo enum `RateType` (Fixed | Variable) en el dominio
- `InterestRate` se extiende con `RateType`, `Spread` y `ReferenceRateId`
- Nueva entidad de dominio `ReferenceRateEntry` para almacenar tasas de referencia externas (TBP, PRIME)
- Nueva interfaz `IReferenceRateRepository` y su implementación con Dapper
- Nuevo método `LoanContractAggregate.AdjustRate()` que recalcula el schedule residual y emite `RateAdjusted`
- Nuevo domain event `RateAdjusted` con el schedule recalculado embebido
- Nuevo `RateAdjustmentWorker` (BackgroundService) que corre el primer día de cada mes
- Nuevo `RateAdjustmentJob` en Application que detecta préstamos variables y dispara el reajuste
- Nuevos endpoints `GET /api/reference-rates/{id}` y `PUT /api/reference-rates/{id}`
- `CreateLoanContractRequest` acepta `RateType`, `Spread` y `ReferenceRateId`
- Migraciones SQL: tabla `reference_rates`, columnas `rate_type`/`spread`/`reference_rate_id` en read models
- **BREAKING**: `CreateLoanContractCommand` agrega campos opcionales — compatibilidad hacia atrás mantenida con defaults (`RateType = "Fixed"`)

## Capabilities

### New Capabilities

- `variable-rate-loan`: soporte de préstamos con tasa de interés variable vinculada a tasa de referencia externa, incluyendo reajuste mensual automático y recálculo de schedule residual
- `reference-rate-management`: gestión de tasas de referencia externas (TBP, PRIME) con endpoints de consulta y actualización manual por el administrador

### Modified Capabilities

- `loan-contract`: el contrato de préstamo extiende su modelo de tasa para soportar tipo Fixed/Variable, spread y referencia; y acepta el evento `RateAdjusted` en su ciclo de vida

## Impact

- **Domain**: `InterestRate` (value object), `LoanContractAggregate`, `LoanContractState`, nuevos eventos
- **Application**: nuevo `RateAdjustmentJob`, extensión de `CreateLoanContractCommand`
- **Infrastructure**: nuevo `ReferenceRateRepository`, nuevo `RateAdjustmentWorker`, `LoanSummaryProjector`
- **API**: nuevo `ReferenceRateEndpoints`, extensión de `LoanContractEndpoints`
- **SQL**: 2 migraciones (tabla `reference_rates`, columnas en `rm_loan_summary`)
- **WorkerLockId**: nuevo valor `RateAdjustment = 1007`
