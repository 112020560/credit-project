## Why

Financial institutions require configurable, auditable approval chains for credit applications. The current single-agent approve/reject is insufficient for organizations that need multi-step sign-off (e.g., junior agent recommends → senior agent approves) or policy-driven routing.

## What Changes

- Introduce a `WorkflowDefinition` entity that configures an ordered list of `WorkflowStep` records, each requiring a specific role or agent assignment.
- When a CreditApplication enters `InReview`, an `ApprovalRequested` event is raised and the active workflow is instantiated against the application.
- Agents act on their assigned step via `POST /api/v1/applications/{id}/approve` or `POST /api/v1/applications/{id}/reject`, creating an `ApprovalDecision` record.
- When all required steps are completed with approval decisions, the application transitions to `Approved`. Any rejection at any step terminates the chain and transitions the application to `Rejected`.
- Add `POST /api/v1/workflows` to define workflow templates.
- Events `ApplicationApproved` and `ApplicationRejected` are emitted when the workflow reaches a terminal outcome (replacing the direct approve/reject events currently published by the application handlers).

## Capabilities

### New Capabilities
- `approval-workflows`: Configurable multi-step approval chain engine — `WorkflowDefinition`, `WorkflowStep`, `ApprovalDecision` entities, workflow instantiation on InReview entry, step-by-step decision recording.

### Modified Capabilities
- `credit-applications`: The existing approve and reject endpoints now route decisions through the workflow engine. A single agent acting does not immediately approve/reject the application unless all workflow steps are satisfied. The status lifecycle acquires a new `PendingApproval` transient state while the workflow chain is in progress.

## Impact

- New DB tables: `workflow_definitions`, `workflow_steps`, `approval_decisions`
- New API endpoint: `POST /api/v1/workflows`
- Modified behavior: `POST /api/v1/credit-applications/{id}/approve` and `/reject` delegate to the workflow engine
- New events: `ApprovalRequested`, `ApplicationApproved`, `ApplicationRejected`
- `ApproveCreditApplicationCommandHandler` and `RejectCreditApplicationCommandHandler` are refactored to integrate the workflow engine
