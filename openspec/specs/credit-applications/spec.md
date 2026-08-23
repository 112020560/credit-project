## Purpose

Defines the lifecycle and business rules for credit applications, from resumable draft creation through agent review and final approval or rejection.

## Requirements

### Requirement: Create credit application linked to a prospect
The system SHALL allow creating a `CreditApplication` linked to an existing Prospect in `Draft` status. The application SHALL be created with Status `Draft`. A Prospect in `Converted` status SHALL NOT be allowed to create new applications.

#### Scenario: Successful credit application creation
- **WHEN** a valid `POST /api/v1/credit-applications` is received with a valid ProspectId referencing a Prospect in `Draft` status
- **THEN** a CreditApplication is persisted with Status `Draft` and a `CreditApplicationCreated` event is published

#### Scenario: Application creation blocked for converted prospect
- **WHEN** `POST /api/v1/credit-applications` references a Prospect with Status `Converted`
- **THEN** the system SHALL return 422 Unprocessable Entity

#### Scenario: Multiple draft applications allowed per prospect
- **WHEN** a Prospect in `Draft` status already has an existing CreditApplication in any non-terminal status
- **THEN** the system SHALL allow creation of an additional CreditApplication (no uniqueness constraint on active applications per prospect)

---

### Requirement: Draft application is resumable
The system SHALL persist a CreditApplication in `Draft` status indefinitely with no expiration. The prospect SHALL be able to retrieve and continue an existing draft at any time.

#### Scenario: Retrieve existing draft
- **WHEN** `GET /api/v1/credit-applications/{id}` is called for a CreditApplication in `Draft` status
- **THEN** the system returns the full current state of the application including all uploaded documents

---

### Requirement: Submit credit application
The system SHALL allow submitting a CreditApplication in `Draft` status. Before transitioning to `Submitted`, the system SHALL validate that all required documents are in `Uploaded` or `Verified` status. On submit, the linked Prospect Status SHALL transition to `Submitted`. After persisting the `Submitted` transition, the system SHALL automatically trigger a risk evaluation. The risk evaluation outcome SHALL determine the final next status: `Approved` (AutoApprove), `InReview` (ManualReview), or `Rejected` (AutoReject). All state changes and the evaluation SHALL occur within a single transaction.

#### Scenario: Successful submission — manual review outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `ManualReview`
- **THEN** CreditApplication Status transitions to `InReview`, Prospect Status transitions to `Submitted`, `CreditApplicationSubmitted` and `RiskEvaluationCompleted` events are published

#### Scenario: Successful submission — auto-approve outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `AutoApprove`
- **THEN** CreditApplication Status transitions to `Approved`, Prospect is converted to Customer, and `CreditApplicationSubmitted`, `RiskEvaluationCompleted`, `CreditApplicationApproved`, and `ProspectConvertedToCustomer` events are published

#### Scenario: Successful submission — auto-reject outcome
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present and the risk evaluation returns `AutoReject`
- **THEN** CreditApplication Status transitions to `Rejected`, Prospect Status returns to `Draft`, and `CreditApplicationSubmitted`, `RiskEvaluationCompleted`, and `CreditApplicationRejected` events are published

#### Scenario: Submit blocked — missing documents
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called and required documents are missing
- **THEN** the system SHALL return 422 Unprocessable Entity with the list of missing document types

#### Scenario: Submit blocked — no active risk matrix
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called and no `RiskMatrix` has Status `Active`
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Agent approves credit application
The system SHALL allow an agent to record an approval decision on the current pending workflow step for a CreditApplication in `InReview` status. The application SHALL transition to `Approved` and the linked Prospect SHALL be converted to a Customer only when all required workflow steps have been approved. If no active WorkflowDefinition exists, a single agent call immediately approves the application. The agent SHALL NOT be able to modify any application data before approving.

#### Scenario: Successful approval — final step in workflow
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an `InReview` application and it is the final pending workflow step
- **THEN** CreditApplication Status transitions to `Approved`, Prospect is converted to Customer, and `ApplicationApproved` and `ProspectConvertedToCustomer` events are published

#### Scenario: Approval recorded — intermediate step
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an `InReview` application and further workflow steps remain
- **THEN** an ApprovalDecision is recorded for the current step, the application remains in `InReview`, and an `ApprovalRequested` event is published for the next step

#### Scenario: Approval blocked on wrong status
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an application not in `InReview` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Agent rejects credit application
The system SHALL allow an agent to record a rejection decision on the current pending workflow step for a CreditApplication in `InReview` status with a mandatory rejection reason. Rejection at any step SHALL immediately terminate the workflow chain, transition the application to `Rejected`, and return the linked Prospect to `Draft` status.

#### Scenario: Successful rejection
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with a non-empty rejection reason on an `InReview` application
- **THEN** an ApprovalDecision is recorded with outcome `Rejected`, CreditApplication Status transitions to `Rejected`, Prospect Status returns to `Draft`, and an `ApplicationRejected` event is published

#### Scenario: Rejection without reason blocked
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with an empty or missing rejection reason
- **THEN** the system SHALL return 400 Bad Request

---

### Requirement: Credit application status lifecycle
The system SHALL enforce the following and only the following status transitions:
- `Draft` → `Submitted` (internal, transient — immediately followed by risk evaluation)
- `Submitted` → `InReview` (risk evaluation outcome: ManualReview — triggers ApprovalRequested for first workflow step)
- `Submitted` → `Approved` (risk evaluation outcome: AutoApprove — triggers conversion, bypasses workflow)
- `Submitted` → `Rejected` (risk evaluation outcome: AutoReject)
- `InReview` → `InReview` (intermediate workflow step approved — application stays in review pending further steps)
- `InReview` → `Approved` (all workflow steps approved — triggers conversion)
- `InReview` → `Rejected` (any workflow step rejected)
- `Approved` and `Rejected` are terminal statuses

#### Scenario: Invalid status transition rejected
- **WHEN** any transition is attempted that is not in the defined lifecycle
- **THEN** the system SHALL return 422 Unprocessable Entity

#### Scenario: Submitted status is transient
- **WHEN** a `CreditApplication` is submitted
- **THEN** the application SHALL never remain in `Submitted` status after the submit request completes; it SHALL always advance to `InReview`, `Approved`, or `Rejected` within the same transaction

#### Scenario: InReview application with intermediate workflow step stays in InReview
- **WHEN** an agent approves a non-final workflow step
- **THEN** the application remains in `InReview` until the final step is resolved
