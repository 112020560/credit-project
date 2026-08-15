## MODIFIED Requirements

### Requirement: Submit credit application
The system SHALL allow submitting a CreditApplication in `Draft` status. The handler SHALL call `application.Submit()` to perform the transition — direct property mutation is NOT allowed. Before calling `Submit()`, the system SHALL validate that all required documents are in `Uploaded` or `Verified` status. On submit, the linked Prospect Status SHALL transition to `Submitted` via `prospect.Submit()`. After persisting the `Submitted` transition, the system SHALL automatically trigger a risk evaluation. The risk evaluation outcome SHALL determine the final next status: `Approved` via `application.Approve()` (AutoApprove), `InReview` via `application.SendToReview(workflowId)` (ManualReview), or `Rejected` via `application.Reject(reason)` (AutoReject). All state changes and the evaluation SHALL occur within a single transaction.

#### Scenario: Successful submission — manual review outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `ManualReview`
- **THEN** the handler calls `application.Submit()` then `application.SendToReview(workflowId)` and `prospect.Submit()`, CreditApplication Status transitions to `InReview`, Prospect Status transitions to `Submitted`, and `CreditApplicationSubmitted` and `RiskEvaluationCompleted` outbox events are published derived from domain events

#### Scenario: Successful submission — auto-approve outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `AutoApprove`
- **THEN** the handler calls `application.Submit()`, `application.Approve()`, `prospect.Submit()`, `prospect.Convert()`, and `Customer.FromProspect(prospect)`, and outbox events `CreditApplicationSubmitted`, `RiskEvaluationCompleted`, `CreditApplicationApproved`, and `ProspectConverted` are published

#### Scenario: Successful submission — auto-reject outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `AutoReject`
- **THEN** the handler calls `application.Submit()`, `application.Reject(reason)`, and `prospect.Reject()`, and outbox events `CreditApplicationSubmitted`, `RiskEvaluationCompleted`, and `CreditApplicationRejected` are published

#### Scenario: Submit blocked — missing documents
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called and required documents are missing
- **THEN** the system SHALL return 422 Unprocessable Entity with the list of missing document types

#### Scenario: Submit blocked — no active risk matrix
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called and no `RiskMatrix` has Status `Active`
- **THEN** the system SHALL return 422 Unprocessable Entity

### Requirement: Agent approves credit application
The system SHALL allow an agent to record an approval decision on the current pending workflow step for a CreditApplication in `InReview` status. The application SHALL transition to `Approved` via `application.Approve()` and the linked Prospect SHALL be converted via `prospect.Convert()` and `Customer.FromProspect(prospect)` only when all required workflow steps have been approved. If no active WorkflowDefinition exists, a single agent call immediately approves the application. The agent SHALL NOT be able to modify any application data before approving.

#### Scenario: Successful approval — final step in workflow
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an `InReview` application and it is the final pending workflow step
- **THEN** the handler calls `application.Approve()`, `prospect.Convert()`, and `Customer.FromProspect(prospect)`, CreditApplication Status transitions to `Approved`, and `CreditApplicationApproved` and `ProspectConverted` outbox events are published

#### Scenario: Approval recorded — intermediate step
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an `InReview` application and further workflow steps remain
- **THEN** an ApprovalDecision is recorded for the current step, the application remains in `InReview`, and an `ApprovalRequested` outbox event is published for the next step

#### Scenario: Approval blocked on wrong status
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an application not in `InReview` status
- **THEN** the system SHALL return 422 Unprocessable Entity

### Requirement: Agent rejects credit application
The system SHALL allow an agent to record a rejection decision on the current pending workflow step for a CreditApplication in `InReview` status with a mandatory rejection reason. Rejection SHALL call `application.Reject(reason)` and `prospect.Reject()`. These methods encapsulate the transition logic and raise the corresponding domain events.

#### Scenario: Successful rejection
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with a non-empty rejection reason on an `InReview` application
- **THEN** an ApprovalDecision is recorded, the handler calls `application.Reject(reason)` and `prospect.Reject()`, and a `CreditApplicationRejected` outbox event is published

#### Scenario: Rejection without reason blocked
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with an empty or missing rejection reason
- **THEN** the system SHALL return 400 Bad Request

### Requirement: Credit application status lifecycle
The system SHALL enforce the following and only the following status transitions. ALL transitions SHALL be enforced inside the `CreditApplication` aggregate's behavior methods. No handler SHALL mutate `application.Status` directly.
- `Draft` → `Submitted` (via `application.Submit()`)
- `Submitted` → `InReview` (via `application.SendToReview(workflowId)`)
- `Submitted` → `Approved` (via `application.Approve()`)
- `Submitted` → `Rejected` (via `application.Reject(reason)`)
- `InReview` → `InReview` (intermediate workflow step approved)
- `InReview` → `Approved` (via `application.Approve()` on final step)
- `InReview` → `Rejected` (via `application.Reject(reason)`)
- `Approved` and `Rejected` are terminal statuses

#### Scenario: Invalid status transition rejected
- **WHEN** any transition is attempted that is not in the defined lifecycle
- **THEN** the behavior method returns `Result.Failure` with `CreditApplicationError.InvalidTransition`

#### Scenario: Submitted status is transient
- **WHEN** a `CreditApplication` is submitted
- **THEN** the application SHALL never remain in `Submitted` status after the submit request completes; it SHALL always advance to `InReview`, `Approved`, or `Rejected` within the same transaction

#### Scenario: InReview application with intermediate workflow step stays in InReview
- **WHEN** an agent approves a non-final workflow step
- **THEN** the application remains in `InReview` until the final step is resolved
