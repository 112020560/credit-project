## 1. Domain — Risk Rules Aggregate

- [x] 1.1 Create `RiskRule` entity in `Crm.Domain/RiskEngine/` with properties: Id, Name, RuleType, TargetField, Parameters (JSON-serializable), Weight, CreatedAt
- [x] 1.2 Create `RuleType` enum/constants: `RangeCheck`, `ThresholdCheck`, `EnumCheck`
- [x] 1.3 Create `RiskMatrix` entity with properties: Id, Name, Version, Status, AutoApproveThreshold, AutoRejectThreshold, PricingBands (optional), CreatedAt
- [x] 1.4 Create `RiskMatrixRule` join entity (RiskMatrixId, RiskRuleId, Order)
- [x] 1.5 Create `RiskMatrixStatus` enum/constants: `Draft`, `Active`, `Superseded`
- [x] 1.6 Create `RiskRuleError` and `RiskMatrixError` static classes with domain errors (NotFound, InvalidWeight, OverlappingThresholds, NoRules, AlreadyActive, NotEditable)

## 2. Domain — Risk Evaluation Aggregate

- [x] 2.1 Create `RiskEvaluation` entity in `Crm.Domain/RiskEngine/` with properties: Id, CreditApplicationId, RiskMatrixId, RiskMatrixVersion, TotalScore, Outcome, SuggestedInterestRate, SuggestedMaxAmount, EvaluatedAt
- [x] 2.2 Create `ScoreCardEntry` entity with properties: Id, RiskEvaluationId, RuleId, RuleName, TargetField, ObservedValue, Passed, WeightedContribution
- [x] 2.3 Create `RiskEvaluationOutcome` enum/constants: `AutoApprove`, `ManualReview`, `AutoReject`
- [x] 2.4 Create `RiskEvaluationError` static class with domain errors (NotFound, NoActiveMatrix, ApplicationNotEligible)

## 3. Domain — RiskEngine Domain Service

- [x] 3.1 Create `IRiskEngine` interface in `Crm.Domain/RiskEngine/` with method `Evaluate(RiskMatrix matrix, CreditApplicationData data): RiskEvaluation`
- [x] 3.2 Implement `RiskEngine` domain service: applies each `RiskRule` in the matrix against `CreditApplicationData`, accumulates weighted score, compares against thresholds to determine outcome
- [x] 3.3 Implement `RangeCheck`, `ThresholdCheck`, and `EnumCheck` rule evaluators as private strategies within `RiskEngine`
- [x] 3.4 Implement pricing band lookup: given total score and matrix pricing bands, set `SuggestedInterestRate` and `SuggestedMaxAmount` on the result
- [x] 3.5 Create `CreditApplicationData` value object (a snapshot of application fields needed for rule evaluation: ProspectId, income, age, requested amount, etc.)

## 4. Domain — Persistence Abstractions

- [x] 4.1 Create `IRiskRulesRepository` interface in `Crm.Domain/Abstractions/Persistence/` with: `AddAsync`, `GetByIdAsync`, `ListAllAsync`
- [x] 4.2 Create `IRiskMatrixRepository` interface with: `AddAsync`, `GetByIdAsync`, `GetActiveAsync`, `UpdateAsync`, `ListAsync`
- [x] 4.3 Create `IRiskEvaluationsRepository` interface with: `AddAsync`, `GetByIdAsync`, `GetByCreditApplicationIdAsync`

## 5. Infrastructure — Database

- [x] 5.1 Add `DbSet<RiskRule>`, `DbSet<RiskMatrix>`, `DbSet<RiskMatrixRule>` to `CrmDbContext`
- [x] 5.2 Add `DbSet<RiskEvaluation>`, `DbSet<ScoreCardEntry>` to `CrmDbContext`
- [x] 5.3 Configure EF mappings for all new entities in `CrmDbContext.OnModelCreating` (table names: `risk_rules`, `risk_matrices`, `risk_matrix_rules`, `risk_evaluations`, `score_card_entries`; FKs; indexes on `CreditApplicationId`, `RiskMatrixId`)
- [x] 5.4 Configure `Parameters` on `RiskRule` as a JSON column (Npgsql JSONB or owned entity)
- [x] 5.5 Run `dotnet ef migrations add AddRiskEngine` and verify generated SQL
- [x] 5.6 Seed a default `RiskMatrix` (Status `Active`, thresholds set so all scores produce `ManualReview`) in the migration or via `DbInitializer`

## 6. Infrastructure — Repositories

- [x] 6.1 Implement `RiskRulesRepository : IRiskRulesRepository`
- [x] 6.2 Implement `RiskMatrixRepository : IRiskMatrixRepository` including `GetActiveAsync` that filters by `Status = Active`
- [x] 6.3 Implement `RiskEvaluationsRepository : IRiskEvaluationsRepository`
- [x] 6.4 Register all three repositories and `IRiskEngine` / `RiskEngine` in `Crm.Infrastructure/DependencyInjection.cs`

## 7. Application — Risk Rules Commands

- [x] 7.1 Create `CreateRiskRuleCommand` + handler: validates weight > 0 and type is known, persists `RiskRule`
- [x] 7.2 Create `CreateRiskMatrixCommand` + handler: validates thresholds (approve > reject), at least one rule, persists `RiskMatrix` with Status `Draft` and Version 1
- [x] 7.3 Create `ActivateRiskMatrixCommand` + handler: loads matrix, validates Status is `Draft`, sets to `Active`, deactivates any existing `Active` matrix (sets to `Superseded`), saves via `UnitOfWork`
- [x] 7.4 Add FluentValidation validators for all commands
- [x] 7.5 Create `RiskRuleDto`, `RiskMatrixDto`, `CreateRiskRuleDto`, `CreateRiskMatrixDto`

## 8. Application — Risk Evaluation Service & Command

- [x] 8.1 Create `RiskEvaluationService` in `Crm.Application/RiskEngine/`: loads active matrix, builds `CreditApplicationData` from application, calls `IRiskEngine.Evaluate`, persists result
- [x] 8.2 Create `TriggerRiskEvaluationCommand` + handler (for on-demand evaluation): validates application is `InReview`, calls `RiskEvaluationService`, publishes `RiskEvaluationStarted` and `RiskEvaluationCompleted`
- [x] 8.3 Create `GetRiskEvaluationByIdQuery` + handler
- [x] 8.4 Add FluentValidation for `TriggerRiskEvaluationCommand`
- [x] 8.5 Create `RiskEvaluationDto`, `ScoreCardEntryDto`, `TriggerRiskEvaluationDto`

## 9. Application — Update SubmitCreditApplicationCommand

- [x] 9.1 Inject `RiskEvaluationService` into `SubmitCreditApplicationCommandHandler`
- [x] 9.2 After validating documents and persisting the `Submitted` transition, call `RiskEvaluationService` within the same `UnitOfWork` scope
- [x] 9.3 Branch on outcome:
  - `AutoApprove`: call `ConvertProspectToCustomerCommand` handler inline, set application to `Approved`, publish `CreditApplicationApproved`
  - `ManualReview`: set application to `InReview`, publish `CreditApplicationSubmitted`
  - `AutoReject`: set application to `Rejected`, set Prospect to `Draft`, publish `CreditApplicationRejected`
- [x] 9.4 Publish `RiskEvaluationCompleted` in all success branches
- [x] 9.5 Ensure no events are published if the transaction rolls back (use outbox or post-commit hook if needed)

## 10. Event Contracts

- [x] 10.1 Add `RiskEvaluationStarted` contract: `{ RiskEvaluationId, CreditApplicationId, RiskMatrixId, RiskMatrixVersion, StartedAt }`
- [x] 10.2 Add `RiskEvaluationCompleted` contract: `{ RiskEvaluationId, CreditApplicationId, RiskMatrixId, RiskMatrixVersion, TotalScore, Outcome, CompletedAt }`

## 11. WebApi — Endpoints

- [x] 11.1 Create `Crm.WebApi/Endpoints/RiskEngine/TriggerEvaluation.cs` — `POST /api/v1/risk-evaluations` → `TriggerRiskEvaluationCommand`
- [x] 11.2 Create `Crm.WebApi/Endpoints/RiskEngine/CreateRule.cs` — `POST /api/v1/risk-rules` → `CreateRiskRuleCommand`
- [x] 11.3 Create `Crm.WebApi/Endpoints/RiskEngine/CreateMatrix.cs` — `POST /api/v1/risk-matrices` → `CreateRiskMatrixCommand`
- [x] 11.4 Create `Crm.WebApi/Endpoints/RiskEngine/ActivateMatrix.cs` — `POST /api/v1/risk-matrices/{id}/activate` → `ActivateRiskMatrixCommand`
- [x] 11.5 Add `WithTags("RiskEngine")`, `WithOpenApi`, `MapToApiVersion(v1)`, and correct `Produces` to all new endpoints
