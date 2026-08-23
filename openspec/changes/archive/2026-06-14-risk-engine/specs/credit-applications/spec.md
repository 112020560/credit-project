## MODIFIED Requirements

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

### Requirement: Credit application status lifecycle
The system SHALL enforce the following and only the following status transitions:
- `Draft` → `Submitted` (internal, transient — immediately followed by risk evaluation)
- `Submitted` → `InReview` (risk evaluation outcome: ManualReview)
- `Submitted` → `Approved` (risk evaluation outcome: AutoApprove — triggers conversion)
- `Submitted` → `Rejected` (risk evaluation outcome: AutoReject)
- `InReview` → `Approved` (via /approve — agent decision)
- `InReview` → `Rejected` (via /reject — agent decision)
- `Approved` and `Rejected` are terminal statuses

#### Scenario: Invalid status transition rejected
- **WHEN** any transition is attempted that is not in the defined lifecycle
- **THEN** the system SHALL return 422 Unprocessable Entity

#### Scenario: Submitted status is transient
- **WHEN** a `CreditApplication` is submitted
- **THEN** the application SHALL never remain in `Submitted` status after the submit request completes; it SHALL always advance to `InReview`, `Approved`, or `Rejected` within the same transaction
