## 1. Domain — UnderwritingPolicy: nuevos campos

- [x] 1.1 Agregar parámetros `GracePeriodDays: int = 5`, `PenaltyRate: decimal = 0`, `OriginationFeeRate: decimal = 0` al record `UnderwritingPolicy` en `CreditSystem.Domain/Models/`
- [x] 1.2 Eliminar `GracePeriodDays` de `LateFeeConfiguration` en `CreditSystem.Application/Configuration/` (ya lo centraliza `UnderwritingPolicy`)

## 2. Domain — CreditProduct: nuevos campos

- [x] 2.1 Agregar propiedades `PenaltyRate: decimal?` y `OriginationFeeRate: decimal?` a la entidad `CreditProduct` en `CreditSystem.Domain/Entities/`
- [x] 2.2 Actualizar el constructor de `CreditProduct` para aceptar los nuevos parámetros opcionales (nullable, sin validación obligatoria)

## 3. Domain — LoanContractAggregate: interés moratorio y comisión

- [x] 3.1 Agregar campo `AccruedPenaltyInterest: Money` a `LoanContractState` (inicializado en cero como `AccruedInterest`)
- [x] 3.2 Agregar campo `OriginationFee: Money` a `LoanContractState` (el `TotalFees` inicial al crear el contrato)
- [x] 3.3 Actualizar el evento `ContractCreated` para incluir el campo `OriginationFee: Money`
- [x] 3.4 Actualizar `LoanContractAggregate.Create()` para recibir `Money originationFee` y pasarlo al evento `ContractCreated`; el `ApplyEvent` de `ContractCreated` debe inicializar `TotalFees = e.OriginationFee` y `OriginationFee = e.OriginationFee`
- [x] 3.5 Actualizar `LoanContractAggregate.RecordMissedPayment()` para recibir `Money penaltyInterest` como parámetro adicional; acumularlo en `AccruedPenaltyInterest` vía el evento `PaymentMissed` (agregar campo `PenaltyInterestAccrued` al evento)
- [x] 3.6 Actualizar `LoanContractAggregate.ApplyPayment()`: cambiar el orden de aplicación a fees → `AccruedPenaltyInterest` → `AccruedInterest` → principal; actualizar el evento `PaymentApplied` con campo `PenaltyInterestPaid`
- [x] 3.7 Actualizar el `ApplyEvent` switch para manejar los nuevos campos de `PaymentMissed` y `PaymentApplied`

## 4. Infrastructure — Migración y repositorios

- [x] 4.1 Crear script `20260809_AddMoraYComisionesColumns.sql` con: `ALTER TABLE underwriting_policies ADD COLUMN grace_period_days INT NOT NULL DEFAULT 5`, `ALTER TABLE underwriting_policies ADD COLUMN penalty_rate NUMERIC(6,4) NOT NULL DEFAULT 0`, `ALTER TABLE underwriting_policies ADD COLUMN origination_fee_rate NUMERIC(6,4) NOT NULL DEFAULT 0`, `ALTER TABLE credit_products ADD COLUMN penalty_rate NUMERIC(6,4)`, `ALTER TABLE credit_products ADD COLUMN origination_fee_rate NUMERIC(6,4)`
- [x] 4.2 Actualizar `UnderwritingPolicyRepository` para leer y persistir los tres nuevos campos
- [x] 4.3 Actualizar `CreditProductRepository`: incluir `penalty_rate` y `origination_fee_rate` en SELECT, INSERT y `MapToEntity`

## 5. Application — PaymentMissedJob

- [x] 5.1 En `PaymentMissedJob.ProcessOverduePaymentAsync`: agregar guard `if (loan.DaysOverdue <= _policy.GracePeriodDays) return;` antes de procesar
- [x] 5.2 En `PaymentMissedJob`: calcular `penaltyInterest = overduePrincipal × (effectivePenaltyRate/100/365) × daysOverdue` usando `effectivePenaltyRate` resuelto desde el producto del contrato o la política; pasarlo a `aggregate.RecordMissedPayment()`
- [x] 5.3 Resolver `effectivePenaltyRate` en el job: necesita el `CreditProduct` del contrato (via `ICreditProductRepository`) o usar `_policy.PenaltyRate` como fallback si no hay producto asociado al `OverdueLoanInfo`

## 6. Application — CreateContractCommandHandler

- [x] 6.1 En `CreateContractCommandHandler.Handle`: calcular `effectiveOriginationFeeRate = product.OriginationFeeRate ?? _policy.OriginationFeeRate`
- [x] 6.2 Calcular `originationFee = new Money(request.Amount × effectiveOriginationFeeRate / 100, request.Currency)` y pasarlo a `LoanContractAggregate.Create()`

## 7. Infrastructure — Read Models y Proyecciones

- [x] 7.1 Agregar campos `OriginationFee: decimal` y `AccruedPenaltyInterest: decimal` a `LoanSummaryReadModel`
- [x] 7.2 Actualizar `LoanSummaryProjector`: en `HandleContractCreated` mapear `e.OriginationFee.Amount` al campo `OriginationFee` del read model; en `HandlePaymentApplied` actualizar `AccruedPenaltyInterest` restando `PenaltyInterestPaid`; en `HandlePaymentMissed` sumar `PenaltyInterestAccrued` a `AccruedPenaltyInterest`
- [x] 7.3 Actualizar la tabla de proyección `rm_loan_summaries` en la misma migración SQL (agregar columnas `origination_fee` NUMERIC DEFAULT 0 y `accrued_penalty_interest` NUMERIC DEFAULT 0)

## 8. Tests

- [x] 8.1 Agregar tests de `LoanContractAggregate`: comisión de originación inicializa `TotalFees`, pago con interés moratorio respeta el nuevo orden (fees→penalidad→interés→capital), pago perdido sin tasa moratoria no cambia `AccruedPenaltyInterest`
- [x] 8.2 Agregar tests de `PaymentMissedJob`: préstamo dentro del período de gracia es omitido, préstamo fuera del período de gracia registra `PaymentMissed` con interés moratorio correcto
