## ADDED Requirements

### Requirement: Define a workflow definition with ordered steps
The system SHALL allow defining a `WorkflowDefinition` with an ordered list of `WorkflowStep` records. Each step has a name and an optional required role. A workflow definition starts in `Draft` status. Only one `WorkflowDefinition` SHALL have `Active` status at any time; activating a new one automatically deactivates the previous active definition.

#### Scenario: Create workflow definition
- **WHEN** `POST /api/v1/workflows` is called with a name and at least one step
- **THEN** a WorkflowDefinition is persisted in `Draft` status with the provided steps in the given order

#### Scenario: Activate a workflow definition
- **WHEN** `POST /api/v1/workflows/{id}/activate` is called on a `Draft` WorkflowDefinition
- **THEN** the definition transitions to `Active`; any previously `Active` definition transitions to `Superseded`

#### Scenario: Cannot create workflow with no steps
- **WHEN** `POST /api/v1/workflows` is called with an empty steps list
- **THEN** the system SHALL return 400 Bad Request

---

### Requirement: Workflow is instantiated when application enters InReview
The system SHALL automatically associate the currently active `WorkflowDefinition` with a `CreditApplication` when it enters `InReview` status. The active workflow's step sequence defines the required approval chain for that application. If no `WorkflowDefinition` is `Active`, the system SHALL allow a single-agent direct approval (fallback behavior).

#### Scenario: Active workflow associated on InReview entry
- **WHEN** a CreditApplication transitions to `InReview` (via risk evaluation outcome)
- **THEN** the active WorkflowDefinition Id is captured against the application for subsequent step tracking

#### Scenario: No active workflow — single-agent fallback
- **WHEN** a CreditApplication transitions to `InReview` and no WorkflowDefinition is `Active`
- **THEN** the first agent call to `/approve` SHALL immediately approve the application (single-step fallback)

---

### Requirement: Agent records an approval decision per workflow step
The system SHALL allow an agent to record an `ApprovalDecision` for the current pending workflow step of a `CreditApplication` in `InReview` status. Each call to `POST /api/v1/credit-applications/{id}/approve` or `/reject` advances the workflow by one step.

#### Scenario: Agent approves a step — more steps remain
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called and there are further pending steps
- **THEN** an `ApprovalDecision` with outcome `Approved` is recorded for the current step and the application remains in `InReview`; an `ApprovalRequested` event is published for the next step

#### Scenario: Agent approves final step — workflow complete
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called on the last pending step
- **THEN** an `ApprovalDecision` with outcome `Approved` is recorded, CreditApplication transitions to `Approved`, the linked Prospect is converted to a Customer, and `ApplicationApproved` and `ProspectConvertedToCustomer` events are published

#### Scenario: Agent rejects any step — workflow terminated
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with a non-empty rejection reason
- **THEN** an `ApprovalDecision` with outcome `Rejected` is recorded, the workflow chain is terminated immediately, the CreditApplication transitions to `Rejected`, the Prospect returns to `Draft`, and an `ApplicationRejected` event is published

#### Scenario: Rejection without reason blocked
- **WHEN** `POST /api/v1/credit-applications/{id}/reject` is called with an empty or missing rejection reason
- **THEN** the system SHALL return 400 Bad Request

#### Scenario: Decision on application not in InReview blocked
- **WHEN** `/approve` or `/reject` is called on an application not in `InReview` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Approval decision history is preserved
The system SHALL retain all `ApprovalDecision` records for a `CreditApplication`. Each record SHALL capture the workflow step, outcome, rejection reason if applicable, the deciding agent identity, and timestamp.

#### Scenario: All decisions are persisted
- **WHEN** a multi-step workflow completes (approved or rejected)
- **THEN** one `ApprovalDecision` record exists per step that was acted on, with full audit detail
