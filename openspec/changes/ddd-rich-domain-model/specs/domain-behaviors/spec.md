## ADDED Requirements

### Requirement: CreditApplication aggregate exposes behavior methods
The `CreditApplication` aggregate SHALL expose the following behavior methods that encapsulate state transitions and verify preconditions. Each method SHALL raise the corresponding domain event on success and return `Result.Failure` if the precondition is not met.

- `Submit()` — transitions from `Draft`. Raises `CreditApplicationSubmittedEvent`.
- `Approve()` — transitions from `Submitted`. Raises `CreditApplicationApprovedEvent`.
- `Reject(string reason)` — transitions from `Submitted` or `InReview`. Raises `CreditApplicationRejectedEvent`.
- `SendToReview(Guid? workflowDefinitionId)` — transitions from `Submitted` to `InReview`. Raises `CreditApplicationSentToReviewEvent`.

#### Scenario: Submit from valid state
- **WHEN** `Submit()` is called on a `CreditApplication` in `Draft` status
- **THEN** the status transitions to `Submitted` and a `CreditApplicationSubmittedEvent` is added to `DomainEvents`

#### Scenario: Submit from invalid state
- **WHEN** `Submit()` is called on a `CreditApplication` not in `Draft` status
- **THEN** `Result.Failure` is returned with `CreditApplicationError.InvalidTransition` and no domain event is raised

#### Scenario: Approve from valid state
- **WHEN** `Approve()` is called on a `CreditApplication` in `Submitted` status
- **THEN** the status transitions to `Approved` and a `CreditApplicationApprovedEvent` is added to `DomainEvents`

#### Scenario: Reject with reason
- **WHEN** `Reject(reason)` is called with a non-empty reason on a `CreditApplication` in `InReview` status
- **THEN** the status transitions to `Rejected`, `RejectionReason` is set, and a `CreditApplicationRejectedEvent` is added to `DomainEvents`

#### Scenario: SendToReview sets workflow
- **WHEN** `SendToReview(workflowDefinitionId)` is called on a `CreditApplication` in `Submitted` status
- **THEN** the status transitions to `InReview`, `WorkflowDefinitionId` is set, and a `CreditApplicationSentToReviewEvent` is added to `DomainEvents`

---

### Requirement: Prospect aggregate exposes behavior methods
The `Prospect` aggregate SHALL expose behavior methods that encapsulate lifecycle transitions. Each method SHALL raise the corresponding domain event on success.

- `Submit()` — transitions from `Draft` to `Submitted`. Raises `ProspectSubmittedEvent`.
- `Convert()` — transitions from `Submitted` to `Converted`. Raises `ProspectConvertedEvent`.
- `Reject()` — returns to `Draft` from `Submitted`. Raises `ProspectRejectedEvent`.

#### Scenario: Prospect converts successfully
- **WHEN** `Convert()` is called on a `Prospect` in `Submitted` status
- **THEN** the status transitions to `Converted` and a `ProspectConvertedEvent` is added to `DomainEvents`

#### Scenario: Prospect convert blocked from wrong state
- **WHEN** `Convert()` is called on a `Prospect` not in `Submitted` status
- **THEN** `Result.Failure` is returned and no domain event is raised

#### Scenario: Prospect returns to Draft after rejection
- **WHEN** `Reject()` is called on a `Prospect` in `Submitted` status
- **THEN** the status transitions to `Draft` and a `ProspectRejectedEvent` is added to `DomainEvents`

---

### Requirement: Customer aggregate exposes behavior methods
The `Customer` aggregate SHALL expose behavior methods. Each method SHALL raise the corresponding domain event on success.

- `Activate()` — sets status to `Active`. Raises `CustomerActivatedEvent`.
- `Deactivate()` — sets status to `Inactive`. Raises `CustomerDeactivatedEvent`.
- `UpdateProfile(fullName, displayName, identificationType, identificationNumber, birthDate)` — updates profile fields. Raises `CustomerProfileUpdatedEvent`.
- `UpdateFinancials(creditScore, monthlyIncome, monthlyDebt)` — updates financial fields. Raises `CustomerFinancialsUpdatedEvent`.

#### Scenario: UpdateProfile raises event
- **WHEN** `UpdateProfile(...)` is called on a `Customer` with new valid values
- **THEN** the profile fields are updated and a `CustomerProfileUpdatedEvent` is added to `DomainEvents`

#### Scenario: UpdateFinancials raises event
- **WHEN** `UpdateFinancials(...)` is called on a `Customer`
- **THEN** the financial fields are updated and a `CustomerFinancialsUpdatedEvent` is added to `DomainEvents`

---

### Requirement: WorkflowDefinition aggregate exposes behavior methods
The `WorkflowDefinition` aggregate SHALL expose `Activate()` and `Deactivate()` methods.

- `Activate()` — sets status to `Active`. Raises `WorkflowDefinitionActivatedEvent`.
- `Deactivate()` — sets status to `Inactive`. Raises `WorkflowDefinitionDeactivatedEvent`.

#### Scenario: Activate workflow definition
- **WHEN** `Activate()` is called on a `WorkflowDefinition` in `Draft` status
- **THEN** the status transitions to `Active` and a `WorkflowDefinitionActivatedEvent` is added to `DomainEvents`

---

### Requirement: RiskMatrix aggregate exposes behavior method
The `RiskMatrix` aggregate SHALL expose an `Activate()` method that transitions status to `Active` and raises `RiskMatrixActivatedEvent`.

#### Scenario: Activate risk matrix
- **WHEN** `Activate()` is called on a `RiskMatrix` in `Draft` status
- **THEN** the status transitions to `Active` and a `RiskMatrixActivatedEvent` is added to `DomainEvents`

---

### Requirement: Command Handlers use behavior methods and dispatch Domain Events
All Command Handlers that mutate aggregate state SHALL call the aggregate's behavior methods instead of mutating properties directly. After saving changes, handlers SHALL iterate `aggregate.DomainEvents`, translate each to an `OutboxEvent`, append to the outbox via `IOutboxWriter.AppendAsync()`, and then call `aggregate.ClearDomainEvents()`.

#### Scenario: Handler uses behavior method
- **WHEN** a Command Handler needs to approve a CreditApplication
- **THEN** it calls `application.Approve()` and checks the returned `Result` before proceeding — it does NOT set `application.Status = CreditApplicationStatus.Approved` directly

#### Scenario: Handler dispatches domain events to outbox
- **WHEN** a behavior method succeeds and adds domain events to the aggregate
- **THEN** the handler publishes a corresponding `OutboxEvent` for each domain event after `SaveChangesAsync`
