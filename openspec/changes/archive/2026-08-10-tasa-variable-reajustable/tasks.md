## 1. Migración SQL

- [x] 1.1 Crear script `20260810_AddReferenceRatesTable.sql`: tabla `reference_rates (id VARCHAR(32) PRIMARY KEY, name VARCHAR(128) NOT NULL, current_value DECIMAL(8,4) NOT NULL, effective_date DATE NOT NULL, source VARCHAR(64) NOT NULL, updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW())` con INSERT inicial de `TBP_CRC`
- [x] 1.2 Crear script `20260810_AddRateTypeColumnsToLoanSummary.sql`: agregar columnas `rate_type VARCHAR(16) DEFAULT 'Fixed'`, `spread DECIMAL(8,4) DEFAULT 0`, `reference_rate_id VARCHAR(32)` a la tabla `rm_loan_summary`

## 2. Dominio — Modelo de tasa

- [x] 2.1 Agregar enum `RateType` en `CreditSystem.Domain/Enums/`: valores `Fixed = 0`, `Variable = 1`
- [x] 2.2 Extender `InterestRate` con propiedades: `RateType RateType` (default Fixed), `decimal Spread` (default 0), `string? ReferenceRateId` (default null). Mantener constructor existente `InterestRate(decimal annualRate)` como Fixed por defecto. Agregar constructor `InterestRate(decimal annualRate, RateType rateType, decimal spread, string? referenceRateId)`
- [x] 2.3 Crear entidad `ReferenceRateEntry` en `CreditSystem.Domain/Entities/`: `Id (string)`, `Name (string)`, `CurrentValue (decimal)`, `EffectiveDate (DateTime)`, `Source (string)`, `UpdatedAt (DateTime)`

## 3. Dominio — Repositorio de tasa de referencia

- [x] 3.1 Crear interfaz `IReferenceRateRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con métodos: `Task<ReferenceRateEntry?> GetCurrentAsync(string id, CancellationToken)` y `Task UpsertAsync(ReferenceRateEntry entry, CancellationToken)`

## 4. Dominio — Evento y método AdjustRate

- [x] 4.1 Crear domain event `RateAdjusted` en `CreditSystem.Domain/Aggregates/LoanContract/Events/`: `Guid LoanId`, `decimal OldRate`, `decimal NewRate`, `decimal Spread`, `string ReferenceRateId`, `decimal ReferenceRateValue`, `DateTime AdjustedAt`, `IReadOnlyList<AmortizationEntry> NewSchedule`
- [x] 4.2 Agregar método `AdjustRate(decimal newReferenceRateValue, DateTime adjustedAt)` en `LoanContractAggregate`: si `RateType == Fixed` lanzar `DomainException`; calcular `newEffectiveRate = newReferenceRateValue + State.Spread`; si la diferencia con la tasa actual es < 0.0001 retornar sin emitir; calcular `remainingMonths` desde `State.PaymentSchedule`; recalcular schedule residual con el amortization calculator; emitir `RateAdjusted`
- [x] 4.3 Agregar `LoanContractState`: propiedades `RateType RateType`, `decimal Spread`, `string? ReferenceRateId`
- [x] 4.4 Actualizar `ApplyEvent` en `LoanContractAggregate` para manejar `RateAdjusted`: actualizar `State.InterestRate` y `State.PaymentSchedule`
- [x] 4.5 Actualizar `LoanContractAggregate.Create()` para aceptar `RateType`, `Spread`, `ReferenceRateId` y construir el `InterestRate` con el constructor extendido. Emitir `LoanContractCreated` con los nuevos campos

## 5. Infraestructura — Repositorio

- [x] 5.1 Crear `ReferenceRateRepository` en `CreditSystem.Infrastructure/Repositories/` implementando `IReferenceRateRepository` con Dapper: `GetCurrentAsync` → `SELECT ... WHERE id = @Id`; `UpsertAsync` → `INSERT ... ON CONFLICT (id) DO UPDATE SET current_value = ..., updated_at = NOW()`
- [x] 5.2 Registrar `IReferenceRateRepository → ReferenceRateRepository` (Scoped) en `DependencyInjection.cs` de Infrastructure

## 6. Application — Job de reajuste

- [x] 6.1 Crear interfaz `IRateAdjustmentJob` en `CreditSystem.Application/Job/` con método `Task ExecuteAsync(CancellationToken)`
- [x] 6.2 Crear `RateAdjustmentJob` en `CreditSystem.Application/Job/`: consultar read model de préstamos activos con `rate_type = 'Variable'`; para cada uno, rehydratar el aggregate, obtener `IReferenceRateRepository.GetCurrentAsync(referenceRateId)`, llamar `aggregate.AdjustRate()`, persistir si hay eventos uncommitted; filtrar préstamos con menos de 2 meses restantes en el schedule

## 7. Infraestructura — Worker

- [x] 7.1 Agregar `WorkerLockId.RateAdjustment = 1007L` en `CreditSystem.Infrastructure/Locking/WorkerLockId.cs`
- [x] 7.2 Crear `RateAdjustmentWorker` en `CreditSystem.Infrastructure/Workers/`: `BackgroundService` que calcula el próximo primer día del mes, espera hasta ese momento, adquiere `IDistributedLock` con `WorkerLockId.RateAdjustment`, ejecuta `IRateAdjustmentJob.ExecuteAsync()`, libera el lock
- [x] 7.3 Registrar `RateAdjustmentWorker` como `IHostedService` y `IRateAdjustmentJob → RateAdjustmentJob` (Scoped) en `DependencyInjection.cs` de Infrastructure

## 8. Proyecciones

- [x] 8.1 Actualizar `LoanSummaryProjector` para incluir `rate_type`, `spread`, `reference_rate_id` al crear/actualizar el read model `rm_loan_summary`
- [x] 8.2 Agregar manejo de `RateAdjusted` en `LoanSummaryProjector`: actualizar columnas `rate_type`, `spread`, `reference_rate_id` y el campo de tasa efectiva en `rm_loan_summary`

## 9. Application — Comando de creación

- [x] 9.1 Extender `CreateLoanContractCommand` con: `string RateType = "Fixed"`, `decimal? Spread`, `string? ReferenceRateId`
- [x] 9.2 Actualizar `CreateLoanContractCommandValidator`: si `RateType == "Variable"`, requerir `Spread >= 0` y `ReferenceRateId` no vacío; verificar que `ReferenceRateId` existe en `IReferenceRateRepository`
- [x] 9.3 Actualizar `CreateLoanContractCommandHandler`: parsear `RateType`, resolver tasa efectiva inicial si es Variable (`referenceRate.CurrentValue + Spread`), pasar al aggregate

## 10. API — Endpoints de tasa de referencia

- [x] 10.1 Crear `ReferenceRateEndpoints.cs` en `CreditSystem.Api/EndPoints/` con `MapReferenceRateEndpoints()`: `GET /api/reference-rates/{id}` y `PUT /api/reference-rates/{id}`
- [x] 10.2 Crear DTOs: `ReferenceRateResponse` y `UpdateReferenceRateRequest { decimal CurrentValue, DateTime EffectiveDate, string Source }`
- [x] 10.3 Registrar `app.MapReferenceRateEndpoints()` en `Program.cs`

## 11. API — Crear préstamo con tasa variable

- [x] 11.1 Extender `CreateLoanContractRequest` con: `string? RateType`, `decimal? Spread`, `string? ReferenceRateId`
- [x] 11.2 Actualizar el endpoint `POST /api/loans` en `LoanContractEndpoints.cs` para mapear los nuevos campos al comando

## 12. Tests

- [x] 12.1 Test unitario: `LoanContractAggregate.AdjustRate()` emite `RateAdjusted` con nueva tasa y schedule residual correcto cuando la tasa cambia
- [x] 12.2 Test unitario: `LoanContractAggregate.AdjustRate()` lanza `DomainException` si el préstamo es `RateType = Fixed`
- [x] 12.3 Test unitario: `LoanContractAggregate.AdjustRate()` no emite evento si la nueva tasa es igual a la vigente (diferencia < 0.0001)
- [x] 12.4 Test unitario: `RateAdjustmentJob` llama `AdjustRate` y persiste solo para préstamos variables con tasa cambiada; ignora préstamos fijos
