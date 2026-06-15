## 1. Domain — Workflow Entities

- [x] 1.1 Create `WorkflowDefinition` entity in `Crm.Domain/ApprovalWorkflows/` with properties: Id, Name, Status, Steps (ICollection<WorkflowStep>), CreatedAt
- [x] 1.2 Create `WorkflowStep` entity with properties: Id, WorkflowDefinitionId, StepName, Order, RequiredRole (nullable), CreatedAt
- [x] 1.3 Create `ApprovalDecision` entity with properties: Id, CreditApplicationId, WorkflowDefinitionId, WorkflowStepId, Decision, RejectionReason (nullable), DecidedBy (nullable), DecidedAt
- [x] 1.4 Create `WorkflowStatus` static class with constants: `Draft`, `Active`, `Superseded`
- [x] 1.5 Create `ApprovalDecisionOutcome` static class with constants: `Approved`, `Rejected`
- [x] 1.6 Create `WorkflowError` static class with domain errors: `NotFound`, `AlreadyActive`, `NoSteps`
- [x] 1.7 Create `ApprovalError` static class with domain errors: `ApplicationNotInReview`, `RejectionReasonRequired`
- [x] 1.8 Add nullable `WorkflowDefinitionId` property to `CreditApplication` entity

## 2. Domain — Persistence Abstractions

- [x] 2.1 Create `IWorkflowDefinitionsRepository` in `Crm.Domain/Abstractions/Persistence/` with: `AddAsync`, `GetByIdAsync`, `UpdateAsync`, `GetActiveAsync`, `GetAllActiveAsync` (to deactivate previous on activation)
- [x] 2.2 Create `IApprovalDecisionsRepository` with: `AddAsync`, `GetByApplicationIdAsync`
- [x] 2.3 Add `IWorkflowDefinitionsRepository` and `IApprovalDecisionsRepository` to `IUnitOfWork`

## 3. Infrastructure — Database

- [x] 3.1 Add `DbSet<WorkflowDefinition>`, `DbSet<WorkflowStep>`, `DbSet<ApprovalDecision>` to `CrmDbContext`
- [x] 3.2 Configure EF mappings for `WorkflowDefinition` (table: `workflow_definitions`; index on `Status`; snake_case columns)
- [x] 3.3 Configure EF mappings for `WorkflowStep` (table: `workflow_steps`; FK to `workflow_definitions`; index on `WorkflowDefinitionId`; ordered by `Order`)
- [x] 3.4 Configure EF mappings for `ApprovalDecision` (table: `approval_decisions`; FK to `credit_applications`; index on `CreditApplicationId`)
- [x] 3.5 Add nullable `workflow_definition_id` column mapping to `CreditApplication` EF config in `CrmDbContext`
- [x] 3.6 Run `dotnet ef migrations add AddApprovalWorkflows` and verify generated SQL

## 4. Infrastructure — Repositories

- [x] 4.1 Implement `WorkflowDefinitionsRepository : IWorkflowDefinitionsRepository` (include `Steps` ordered by `Order` on `GetByIdAsync` and `GetActiveAsync`)
- [x] 4.2 Implement `ApprovalDecisionsRepository : IApprovalDecisionsRepository`
- [x] 4.3 Add lazy-loaded `WorkflowDefinitionsRepository` and `ApprovalDecisionsRepository` properties to `UnitOfWork`
- [x] 4.4 Register both repositories in `Crm.Infrastructure/DependencyInjection.cs`

## 5. Application — DTOs

- [x] 5.1 Create `CreateWorkflowDefinitionDto` with: Name, Steps (list of `WorkflowStepInputDto`)
- [x] 5.2 Create `WorkflowStepInputDto` with: StepName, Order, RequiredRole (nullable)
- [x] 5.3 Create `RecordApprovalDecisionDto` with: RejectionReason (nullable), DecidedBy (nullable)
- [x] 5.4 Create `WorkflowDefinitionDto` with: Id, Name, Status, Steps (list of `WorkflowStepDto`), CreatedAt
- [x] 5.5 Create `WorkflowStepDto` with: Id, StepName, Order, RequiredRole
- [x] 5.6 Create `ApprovalDecisionDto` with: Id, WorkflowStepId, Decision, RejectionReason, DecidedBy, DecidedAt

## 6. Event Contracts

- [x] 6.1 Create `ApprovalRequestedContract` with: `{ CreditApplicationId, WorkflowDefinitionId, WorkflowStepId, StepName, StepOrder, RequestedAt }`
- [x] 6.2 Create `ApplicationApprovedContract` with: `{ CreditApplicationId, ProspectId, WorkflowDefinitionId, ApprovedAt }`
- [x] 6.3 Create `ApplicationRejectedContract` with: `{ CreditApplicationId, ProspectId, WorkflowDefinitionId, RejectionReason, RejectedAt }`

## 7. Application — Workflow Service

- [x] 7.1 Create `ApprovalWorkflowService` (scoped) with method `RecordDecisionAsync(CreditApplication, string decision, string? reason, string? decidedBy, CancellationToken)` that: loads active WorkflowDefinition, finds pending step, records ApprovalDecision, determines if chain is complete or terminated, updates CreditApplication status, triggers customer conversion on final approval, publishes appropriate events
- [x] 7.2 Register `ApprovalWorkflowService` as scoped in `Crm.Application/DependencyInjection.cs`

## 8. Application — Commands

- [x] 8.1 Create `CreateWorkflowDefinitionCommand(CreateWorkflowDefinitionDto Dto)` + handler: validates at least one step, persists `WorkflowDefinition` in `Draft` status with ordered steps, returns `WorkflowDefinitionDto`
- [x] 8.2 Create `ActivateWorkflowDefinitionCommand(Guid WorkflowId)` + handler: loads definition, sets previous `Active` definition to `Superseded`, sets this one to `Active`, saves
- [x] 8.3 Refactor `ApproveCreditApplicationCommandHandler`: delegate approval logic to `ApprovalWorkflowService.RecordDecisionAsync` (remove direct status transition and customer conversion inline code)
- [x] 8.4 Refactor `RejectCreditApplicationCommandHandler`: delegate rejection logic to `ApprovalWorkflowService.RecordDecisionAsync` (remove direct status transition inline code)
- [x] 8.5 Modify `SubmitCreditApplicationCommandHandler` (InReview branch): after transitioning to `InReview`, capture `WorkflowDefinitionId` from active workflow on the `CreditApplication`, publish `ApprovalRequested` for the first step
- [x] 8.6 Add FluentValidation validators: `CreateWorkflowDefinitionCommand` (name required, steps non-empty); `ActivateWorkflowDefinitionCommand` (id non-empty)

## 9. WebApi — Endpoints

- [x] 9.1 Create `Crm.WebApi/Endpoints/ApprovalWorkflows/CreateWorkflow.cs` — `POST /api/v1/workflows` → `CreateWorkflowDefinitionCommand` → 201 Created with `WorkflowDefinitionDto`
- [x] 9.2 Create `Crm.WebApi/Endpoints/ApprovalWorkflows/ActivateWorkflow.cs` — `POST /api/v1/workflows/{id}/activate` → `ActivateWorkflowDefinitionCommand` → 204 No Content
- [x] 9.3 Add `WithTags("ApprovalWorkflows")`, `WithOpenApi`, `MapToApiVersion(v1)`, and correct `Produces` to both endpoints
