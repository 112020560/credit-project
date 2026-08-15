## 1. Dominio — Estado y eventos

- [x] 1.1 Agregar `Disbursing` al enum `ContractStatus` en `CreditSystem.Domain/Enums/ContractStatus.cs`
- [x] 1.2 Crear `DisbursementConfirmed` en `CreditSystem.Domain/Aggregates/LoanContract/Events/`: `AggregateId`, `ConfirmedBy` (string), `DisbursedAt` (DateTime)
- [x] 1.3 Crear `DisbursementFailed` en `CreditSystem.Domain/Aggregates/LoanContract/Events/`: `AggregateId`, `Reason` (string), `FailedAt` (DateTime)
- [x] 1.4 Modificar `LoanContractState`: actualizar `ApplyEvent` para `LoanDisbursed` → `Status = Disbursing` (en lugar de `Active`)
- [x] 1.5 Agregar casos en `ApplyEvent` de `LoanContractState` para `DisbursementConfirmed` → `Status = Active` y `DisbursementFailed` → `Status = Approved`
- [x] 1.6 Agregar método `ConfirmDisbursement(string confirmedBy)` en `LoanContractAggregate`: `EnsureStatus(Disbursing)` → aplica `DisbursementConfirmed`
- [x] 1.7 Agregar método `FailDisbursement(string reason)` en `LoanContractAggregate`: `EnsureStatus(Disbursing)` → aplica `DisbursementFailed`

## 2. Migración de base de datos

- [x] 2.1 Crear `Src/Core/CreditSystem.Infrastructure/Migrations/20260813_DisbursementLifecycle.sql`:
  - `CREATE TABLE rm_pending_disbursements` con columnas: `loan_id UUID PK`, `customer_id UUID NOT NULL`, `customer_name VARCHAR(200)`, `principal NUMERIC(18,2) NOT NULL`, `currency VARCHAR(3) NOT NULL`, `disbursement_method VARCHAR(50)`, `destination_account VARCHAR(100)`, `approved_at TIMESTAMPTZ NOT NULL`, `disbursement_instructed_at TIMESTAMPTZ`, `status VARCHAR(20) NOT NULL DEFAULT 'Approved'`, `updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
  - `CREATE INDEX idx_pending_disbursements_status ON rm_pending_disbursements(status, disbursement_instructed_at ASC)`
  - Script de inserción de eventos `DisbursementConfirmed` sintéticos para préstamos existentes con `LoanDisbursed` ya persistido (ver design.md — Migration Plan)

## 3. Read model y proyector

- [x] 3.1 Crear `PendingDisbursementReadModel` en `CreditSystem.Domain/Models/ReadModels/` con todos los campos de `rm_pending_disbursements`
- [x] 3.2 Crear `PendingDisbursementsProjector : IProjection` en `CreditSystem.Infrastructure/Projectors/`:
  - `ContractCreated` → INSERT en `rm_pending_disbursements` (status = Approved, método/cuenta null)
  - `LoanDisbursed` → UPDATE status = Disbursing, disbursement_method, destination_account, disbursement_instructed_at
  - `DisbursementConfirmed` → DELETE WHERE loan_id
  - `DisbursementFailed` → UPDATE status = Approved, limpiar método/cuenta/instructed_at
  - `ContractDefaulted` / `ContractPaidOff` → DELETE WHERE loan_id
- [x] 3.3 Registrar `PendingDisbursementsProjector` en `DependencyInjection.cs` como `AddScoped<IProjection, PendingDisbursementsProjector>()`
- [x] 3.4 Actualizar `LoanSummaryProjector.HandleLoanDisbursed`: cambiar `status = 'Active'` → `status = 'Disbursing'`
- [x] 3.5 Agregar `HandleDisbursementConfirmed` en `LoanSummaryProjector`: `UPDATE rm_loan_summaries SET status = 'Active', updated_at WHERE loan_id`
- [x] 3.6 Agregar `HandleDisbursementFailed` en `LoanSummaryProjector`: `UPDATE rm_loan_summaries SET status = 'Approved', updated_at WHERE loan_id`
- [x] 3.7 Agregar los casos `DisbursementConfirmed` y `DisbursementFailed` al switch de `LoanSummaryProjector.ProjectAsync`

## 4. Comandos y handlers

- [x] 4.1 Crear `ConfirmDisbursementCommand` en `CreditSystem.Application/Commands/ConfirmDisbursement/`: `LoanId` (Guid), `ConfirmedBy` (string)
- [x] 4.2 Crear `ConfirmDisbursementCommandValidator`: `LoanId` not empty, `ConfirmedBy` not empty
- [x] 4.3 Crear `ConfirmDisbursementResponse`: `LoanId`, `ConfirmedBy`, `DisbursedAt`
- [x] 4.4 Crear `ConfirmDisbursementCommandHandler`: carga aggregate, llama `ConfirmDisbursement(request.ConfirmedBy)`, `SaveAsync`, retorna response
- [x] 4.5 Crear `FailDisbursementCommand` en `CreditSystem.Application/Commands/FailDisbursement/`: `LoanId` (Guid), `Reason` (string)
- [x] 4.6 Crear `FailDisbursementCommandValidator`: `LoanId` not empty, `Reason` not empty
- [x] 4.7 Crear `FailDisbursementResponse`: `LoanId`, `Reason`, `FailedAt`
- [x] 4.8 Crear `FailDisbursementCommandHandler`: carga aggregate, llama `FailDisbursement(request.Reason)`, `SaveAsync`, retorna response

## 5. Query service y endpoint

- [x] 5.1 Agregar `GetPendingDisbursementsAsync(CancellationToken) → Task<IEnumerable<PendingDisbursementReadModel>>` a `ILoanQueryService` y a `LoanQueryService` (SELECT * FROM rm_pending_disbursements ORDER BY disbursement_instructed_at ASC NULLS LAST)
- [x] 5.2 Crear DTOs en `CreditSystem.Api/EndPoints/Dtos/`:
  - `ConfirmDisbursementRequest`: `ConfirmedBy` (string)
  - `FailDisbursementRequest`: `Reason` (string)
  - `PendingDisbursementResponse`: todos los campos de `PendingDisbursementReadModel` en camelCase
- [x] 5.3 Agregar en `LoanContractEndpoints.cs`:
  - `GET /loans/pending-disbursement` → llama `ILoanQueryService.GetPendingDisbursementsAsync`, retorna lista de `PendingDisbursementResponse`
  - `POST /loans/{id}/confirm-disbursement` → despacha `ConfirmDisbursementCommand`, retorna 200 o error
  - `POST /loans/{id}/fail-disbursement` → despacha `FailDisbursementCommand`, retorna 200 o error

## 6. Tests

- [x] 6.1 Actualizar tests de dominio existentes que llamen `Disburse()` y esperen estado `Active`: agregar llamada a `ConfirmDisbursement("test")` donde corresponda
- [x] 6.2 Crear `DisbursementLifecycleTests.cs` en `CreditSystem.Tests/Domain/`:
  - Test: `Disburse()` desde `Approved` → estado `Disbursing`, evento `LoanDisbursed`
  - Test: `ConfirmDisbursement()` desde `Disbursing` → estado `Active`, evento `DisbursementConfirmed`
  - Test: `FailDisbursement()` desde `Disbursing` → estado `Approved`, evento `DisbursementFailed`
  - Test: `Disburse()` desde `Disbursing` lanza `DomainException`
  - Test: `ConfirmDisbursement()` desde `Active` lanza `DomainException`
  - Test: `FailDisbursement()` desde `Approved` lanza `DomainException`
  - Test: flujo completo con reintento: `Disburse → Fail → Disburse → Confirm`
- [x] 6.3 Actualizar `DisburseLoanCommandHandlerTests` si existen: el handler ahora deja el préstamo en `Disbursing`
- [x] 6.4 Crear `ConfirmDisbursementCommandHandlerTests.cs`: handler exitoso, loan no encontrado, domain exception propagada
- [x] 6.5 Crear `FailDisbursementCommandHandlerTests.cs`: handler exitoso, loan no encontrado, domain exception propagada
