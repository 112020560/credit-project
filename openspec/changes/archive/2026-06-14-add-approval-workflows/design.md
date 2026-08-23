## Context

The current approve/reject flow in `ApproveCreditApplicationCommandHandler` and `RejectCreditApplicationCommandHandler` performs a direct status transition with no intermediary steps. The domain model has no concept of ordered approvers or multi-step sign-off chains. This change introduces a lightweight workflow engine embedded in the application layer, without introducing a separate workflow service or external dependency.

Existing endpoints (`POST /api/v1/credit-applications/{id}/approve` and `/reject`) are preserved — only their handler logic changes. The new `POST /api/v1/workflows` endpoint allows ops/admin to configure approval chains without code changes.

## Goals / Non-Goals

**Goals:**
- Configurable ordered approval steps via `WorkflowDefinition` + `WorkflowStep`
- Per-step `ApprovalDecision` recording with full audit trail
- Single active workflow at a time; automatic deactivation of previous
- Single-agent fallback when no workflow is active (preserves current behavior on deploy)
- Reuse existing approve/reject endpoint contracts — no client-side changes required

**Non-Goals:**
- Role-based step assignment enforcement (field exists, but not validated against JWT claims in this slice)
- Parallel / split approval paths
- Workflow versioning on in-flight applications (applications capture the WorkflowDefinitionId at InReview entry; subsequent definition changes don't affect in-flight apps)
- UI for workflow configuration

## Decisions

### 1. Embed workflow engine in Application layer, not a separate service
Rather than a dedicated workflow microservice, the engine lives in `Crm.Application` as `ApprovalWorkflowService`. It is called by the refactored approve/reject command handlers. This avoids distributed-transaction complexity and keeps the footprint small.

*Alternatives considered*: External workflow engine (Elsa, Workflow Core) — overkill for linear ordered chains; adds NuGet dependencies and migration complexity.

### 2. Track workflow progress via ApprovalDecision records
Progress is derived by counting `ApprovalDecision` records for an application against the step count of its associated `WorkflowDefinition`. No separate "WorkflowInstance" entity is needed — step completion is implicit.

The pending step = the first `WorkflowStep` (by `Order`) with no matching `ApprovalDecision` for the application.

*Alternatives considered*: A `WorkflowInstance` entity with a `CurrentStepId` pointer — adds write contention and a third entity with no new information.

### 3. Capture WorkflowDefinitionId on CreditApplication at InReview entry
When a `CreditApplication` transitions to `InReview`, the Id of the currently `Active` `WorkflowDefinition` is stored on the application (nullable FK). This snapshots the workflow contract for that application and makes progress queries self-contained.

If no workflow is active, `WorkflowDefinitionId` is null → fallback to single-step approval.

### 4. Auto-deactivate previous workflow on activation
`ActivateWorkflowDefinitionCommand` sets the new definition to `Active` and sets the previous active one to `Superseded`. No cascade needed at the DB level — done in the handler.

### 5. Reuse `ApproveCreditApplicationCommandHandler.MapProspectToCustomer`
The customer conversion logic (already `internal static`) is called by `ApprovalWorkflowService` when the final step is approved, rather than duplicating the code.

## Risks / Trade-offs

- **Concurrent step submissions** → Two agents calling `/approve` simultaneously for the same pending step could create two `ApprovalDecision` records for the same step. Mitigation: query pending step within the same transaction and use optimistic concurrency (EF `RowVersion` or select-then-check under transaction).
- **No active workflow blocks approval** → Current behavior preserved via single-agent fallback. Teams that don't configure a workflow see no change.
- **Migration of in-flight InReview applications** → Applications already in `InReview` at deploy time will have `WorkflowDefinitionId = null` and will use the fallback single-step approval. This is safe.
- **WorkflowStep.RequiredRole not enforced** → Field is stored but not validated against the caller's identity in this slice. Enforcement can be added later without schema changes.

## Migration Plan

1. Deploy schema migration: adds `workflow_definitions`, `workflow_steps`, `approval_decisions` tables and `workflow_definition_id` nullable FK on `credit_applications`.
2. No seed data required — the single-agent fallback handles existing behavior until a workflow is configured via the API.
3. Rollback: remove the three new tables and the FK column. No data loss for existing applications (FK is nullable).

## Open Questions

- Should `WorkflowStep.RequiredRole` be enforced against JWT claims in this slice or deferred?
- Should there be a `GET /api/v1/workflows/active` endpoint to inspect the current active definition?
