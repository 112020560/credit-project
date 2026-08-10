## 1. Domain — Enum y domain service

- [x] 1.1 Crear enum `LoanRiskCategory` en `CreditSystem.Domain/Enums/` con valores: `A1, A2, B1, B2, C1, C2, D, E`
- [x] 1.2 Crear clase estática `RiskCategoryTable` en `CreditSystem.Domain/Services/` con dos métodos: `GetCategory(int daysOverdue): LoanRiskCategory` (tabla de días según SUGEF 1-05) y `GetProvisionRate(LoanRiskCategory): decimal` (A1=0, A2=0.005, B1=0.05, B2=0.10, C1=0.25, C2=0.50, D=0.75, E=1.00)
- [x] 1.3 Crear domain service `RiskClassificationService` en `CreditSystem.Domain/Services/` con método `Classify(int daysOverdue, decimal currentBalance): (LoanRiskCategory Category, decimal EstimatedProvision)` que delega en `RiskCategoryTable`

## 2. Domain — Evento de dominio

- [x] 2.1 Crear record `LoanRiskCategoryChanged` en `CreditSystem.Domain/Events/` (no hereda de `DomainEvent` del aggregate — es un evento de notificación standalone) con propiedades: `Guid LoanId`, `LoanRiskCategory? PreviousCategory`, `LoanRiskCategory NewCategory`, `int DaysOverdue`, `decimal EstimatedProvision`, `bool IsManual`, `DateTime ClassifiedAt`

## 3. Infrastructure — Read Model y migración SQL

- [x] 3.1 Agregar propiedades `RiskCategory: string?` y `EstimatedProvision: decimal` a `LoanSummaryReadModel` en `CreditSystem.Domain/Models/ReadModels/`
- [x] 3.2 Crear script `20260809_AddRiskCategoryColumns.sql` con: `ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS risk_category VARCHAR(3)`, `ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS estimated_provision NUMERIC NOT NULL DEFAULT 0`
- [x] 3.3 Actualizar la query `GetLoanSummaryAsync` y `GetCustomerLoansAsync` en `LoanQueryService` para incluir `risk_category AS RiskCategory`, `estimated_provision AS EstimatedProvision` en el SELECT

## 4. Application — RiskClassificationJob

- [x] 4.1 Crear interfaz `IRiskClassificationJob` en `CreditSystem.Application/Job/` con método `ExecuteAsync(CancellationToken)`
- [x] 4.2 Crear `RiskClassificationJob` en `CreditSystem.Application/Job/` que: (a) consulta todos los préstamos activos/morosos/default con `ILoanQueryService.GetLoansForRiskClassificationAsync`, (b) para cada uno llama `RiskClassificationService.Classify(daysOverdue, currentBalance)`, (c) si la categoría difiere de la almacenada, actualiza `rm_loan_summaries` vía `IRiskClassificationRepository.UpdateRiskCategoryAsync` y emite `LoanRiskCategoryChanged`
- [x] 4.3 Crear método `GetLoansForRiskClassificationAsync(): Task<IReadOnlyList<LoanRiskInfo>>` en `ILoanQueryService` y en `LoanQueryService`; el record `LoanRiskInfo` contiene: `Guid LoanId`, `decimal CurrentBalance`, `int DaysOverdue`, `string? CurrentRiskCategory`; la query calcula `EXTRACT(DAY FROM NOW() - next_payment_date)::INT` para préstamos con `status IN ('Active','Delinquent','Default')` y `next_payment_date IS NOT NULL`

## 5. Infrastructure — Repositorio y worker

- [x] 5.1 Crear interfaz `IRiskClassificationRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con método `UpdateRiskCategoryAsync(Guid loanId, string category, decimal provision, CancellationToken)`
- [x] 5.2 Crear `RiskClassificationRepository` en `CreditSystem.Infrastructure/Repositories/` que ejecuta `UPDATE rm_loan_summaries SET risk_category = @Category, estimated_provision = @Provision, updated_at = NOW() WHERE loan_id = @LoanId` vía Dapper
- [x] 5.3 Crear `RiskClassificationWorker` en `CreditSystem.Infrastructure/Workers/` (BackgroundService) con el mismo patrón que `PaymentMissedWorker`: leer hora de ejecución de configuración (`Jobs:RiskClassification:RunTime`, default `02:00:00`), crear scope, resolver `IRiskClassificationJob`, ejecutar
- [x] 5.4 Registrar `IRiskClassificationJob → RiskClassificationJob` (Scoped), `IRiskClassificationRepository → RiskClassificationRepository` (Scoped) y `RiskClassificationWorker` (Hosted Service) en `DependencyInjection.cs`

## 6. API — Endpoints

- [x] 6.1 Crear `RiskEndpoints.cs` en `CreditSystem.Api/EndPoints/` con grupo de rutas bajo `/api/loans`
- [x] 6.2 Implementar `GET /api/loans/risk-summary`: consultar `rm_loan_summaries` agrupado por `risk_category` (excluir NULL) con `COUNT`, `SUM(current_balance)`, `SUM(estimated_provision)` para status in ('Active','Delinquent','Default'); incluir totales globales; mapearlo al DTO `RiskSummaryResponse { List<RiskCategoryRow> Categories, int TotalLoans, decimal TotalPortfolioBalance, decimal TotalRequiredProvision }`; `RiskCategoryRow { string Category, decimal ProvisionPercentage, int LoanCount, decimal TotalBalance, decimal TotalProvision }`
- [x] 6.3 Implementar `GET /api/loans/risk-summary/{category}`: validar que `category` sea un valor válido del enum; consultar `rm_loan_summaries WHERE risk_category = @Category AND status IN ('Active','Delinquent','Default')`; retornar lista de `LoanRiskDetailRow { Guid LoanId, Guid CustomerId, string? CustomerName, decimal CurrentBalance, decimal EstimatedProvision, int DaysOverdue, string RiskCategory }`
- [x] 6.4 Implementar `PUT /api/loans/{loanId}/risk-category`: recibir body `{ "category": "B1" }`; validar que sea categoría válida y que sea >= categoría actual (no permite mejorar); llamar a `IRiskClassificationRepository.UpdateRiskCategoryAsync`; retornar 204; retornar 422 si se intenta mejorar
- [x] 6.5 Registrar `app.MapRiskEndpoints()` en `Program.cs`

## 7. Tests

- [x] 7.1 Agregar tests unitarios de `RiskCategoryTable`: verificar los 8 umbrales de días (0, 1, 30, 31, 60, 61, 90, 91, 120, 121, 180, 181, 360, 361) y los 8 porcentajes de estimación
- [x] 7.2 Agregar tests de `RiskClassificationService`: provisión correcta para cada categoría, provisión = 0 para A1, provisión = saldo para E
- [x] 7.3 Agregar tests de `RiskClassificationJob`: préstamo sin cambio de categoría no llama `UpdateRiskCategoryAsync`, préstamo con cambio actualiza y el resultado contiene `LoanRiskCategoryChanged`
