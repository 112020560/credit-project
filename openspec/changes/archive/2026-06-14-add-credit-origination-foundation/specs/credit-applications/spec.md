## ADDED Requirements

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
The system SHALL allow submitting a CreditApplication in `Draft` status. Before transitioning to `Submitted`, the system SHALL validate that all required documents are in `Uploaded` or `Verified` status. On submit, the linked Prospect Status SHALL transition to `Submitted`.

#### Scenario: Successful submission
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called on a `Draft` application with all required documents present
- **THEN** CreditApplication Status transitions to `Submitted`, Prospect Status transitions to `Submitted`, and a `CreditApplicationSubmitted` event is published

#### Scenario: Submit blocked — missing documents
- **WHEN** `POST /api/v1/credit-applications/{id}/submit` is called and required documents are missing
- **THEN** the system SHALL return 422 Unprocessable Entity with the list of missing document types

---

### Requirement: Agent approves credit application
The system SHALL allow an agent to approve a CreditApplication in `InReview` status. Approval SHALL trigger automatic prospect-to-customer conversion. The agent SHALL NOT be able to modify any application data before approving.

#### Scenario: Successful approval
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an `InReview` application
- **THEN** CreditApplication Status transitions to `Approved`, Prospect is converted to Customer, and `CreditApplicationApproved` and `ProspectConvertedToCustomer` events are published

#### Scenario: Approval blocked on wrong status
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on an application not in `InReview` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Agent rejects credit application
The system SHALL allow an agent to reject a CreditApplication in `InReview` status with a mandatory rejection reason. On rejection, the linked Prospect SHALL return to `Draft` status, allowing a new application.

#### Scenario: Successful rejection
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with a non-empty rejection reason on an `InReview` application
- **THEN** CreditApplication Status transitions to `Rejected`, Prospect Status returns to `Draft`, and a `CreditApplicationRejected` event is published

#### Scenario: Rejection without reason blocked
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with an empty or missing rejection reason
- **THEN** the system SHALL return 400 Bad Request

---

### Requirement: Credit application status lifecycle
The system SHALL enforce the following and only the following status transitions:
- `Draft` → `Submitted` (via /submit)
- `Submitted` → `InReview` (system transition, e.g., after automatic validation or manual trigger)
- `InReview` → `Approved` (via /approve)
- `InReview` → `Rejected` (via /reject)
- `Approved` and `Rejected` are terminal statuses

#### Scenario: Invalid status transition rejected
- **WHEN** any transition is attempted that is not in the defined lifecycle
- **THEN** the system SHALL return 422 Unprocessable Entity
