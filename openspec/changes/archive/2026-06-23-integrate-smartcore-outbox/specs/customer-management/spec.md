## MODIFIED Requirements

### Requirement: Customer can be created via two independent paths
The CRM supports two origination paths for creating a Customer:
1. **Direct creation** via `POST /api/v1/customers` (agent-initiated).
2. **Conversion** from an approved Prospect via the approval workflow.

Both paths SHALL persist a `CustomerCreated` event to the outbox with the same contract structure via `IOutboxWriter.AppendAsync()`. The `outbox-worker` delivers the event to RabbitMQ asynchronously.

#### Scenario: Customer created via direct path — event persisted to outbox
- **WHEN** `POST /api/v1/customers` is called with valid data and the domain commit succeeds
- **THEN** a Customer is created with Status `Active` and a `CustomerCreated` event is persisted to `outbox_db` with `Status = Pending` — the `outbox-worker` delivers it to Rabbit

#### Scenario: Customer created via conversion path — event persisted to outbox
- **WHEN** a Prospect is approved and the conversion completes successfully
- **THEN** a Customer is created with Status `Active` from Prospect data and a `CustomerCreated` event is persisted to `outbox_db`

### Requirement: CustomerCreated event includes financial metadata when provided
Both customer creation paths SHALL populate the `Metadata` field of the `CustomerCreated` outbox event payload with financial data when the creation request includes it. If `metadata` is absent or a key is missing, the corresponding entry in `Metadata` SHALL be `null` or omitted — no error SHALL be raised.

#### Scenario: Financial metadata present — included in outbox payload
- **WHEN** `POST /api/v1/customers` is called with `CreditScore`, `MonthlyIncome`, and `MonthlyDebt`
- **THEN** a Customer is created and a `CustomerCreated` outbox event is persisted with `Metadata["CreditScore"]`, `Metadata["MonthlyIncome"]`, and `Metadata["MonthlyDebt"]` set to the provided values

#### Scenario: Partial financial metadata — partial payload
- **WHEN** only `CreditScore` is provided in the request
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata["CreditScore"]` set and `MonthlyIncome` / `MonthlyDebt` absent or null — no error is raised

#### Scenario: No financial metadata — Metadata null
- **WHEN** no financial metadata is provided
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata` as `null`

### Requirement: Customer profile changes persist a CustomerUpdated event to the outbox
The system SHALL persist a `CustomerUpdated` outbox event with a typed, versioned contract via `IOutboxWriter.AppendAsync()` whenever a Customer's profile is successfully updated via `PUT /api/v1/customers/{id}`. The event payload SHALL include the customer Id, updated fields, timestamp, and a version number. The `outbox-worker` delivers it to RabbitMQ.

#### Scenario: CustomerUpdated persisted to outbox after profile update
- **WHEN** `PUT /api/v1/customers/{id}` succeeds
- **THEN** a `CustomerUpdated` outbox event is persisted with a typed contract containing CustomerId, the changed field values, UpdatedAt timestamp, and Version

#### Scenario: CustomerUpdated not persisted on failure
- **WHEN** the update fails (validation error or not found)
- **THEN** no `CustomerUpdated` outbox event is persisted
