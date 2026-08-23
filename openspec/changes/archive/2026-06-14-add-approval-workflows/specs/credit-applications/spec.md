## MODIFIED Requirements

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
