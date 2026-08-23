## Purpose

Defines how customers are created and managed. Customers can be created via two independent paths: direct creation for cash customers, and automatic conversion from an approved Prospect for credit customers.

## Requirements

### Requirement: Customer can be created via two independent paths
The system SHALL support two distinct paths for Customer creation, both resulting in a Customer with Status `Active`:
1. **Direct path**: `POST /api/v1/customers` (existing) — for cash customers with no onboarding
2. **Origination path**: Prospect → CreditApplication → Approve → automatic conversion (new)

Both paths SHALL persist a `CustomerCreated` event to the outbox via `IOutboxWriter.AppendAsync()` with the same contract structure. The `outbox-worker` delivers it to RabbitMQ asynchronously.

#### Scenario: Direct creation remains functional
- **WHEN** `POST /api/v1/customers` is called with valid data
- **THEN** a Customer is created with Status `Active` and a `CustomerCreated` event is persisted to `outbox_db` with `Status = Pending`

#### Scenario: Origination path creates customer on approval
- **WHEN** a CreditApplication is approved and conversion succeeds
- **THEN** a Customer is created with Status `Active` from Prospect data and a `CustomerCreated` event is persisted to `outbox_db`

#### Scenario: No cross-contamination between paths
- **WHEN** a Customer is created via the direct path
- **THEN** no Prospect or CreditApplication record is created or required

### Requirement: Customer documents are managed via the document-management capability
The system SHALL allow registering documents owned by a Customer using the document-management module by specifying `OwnerId = CustomerId` and `OwnerType = "Customer"`. Customer documents SHALL NOT require a separate nested endpoint under `/customers/{id}/documents` in this slice.

#### Scenario: Register document for a customer
- **WHEN** `POST /api/v1/documents` is called with `OwnerId` set to a valid Customer Id and `OwnerType = "Customer"`
- **THEN** a Document is registered and associated to that Customer via the polymorphic ownership model

#### Scenario: Retrieve customer documents
- **WHEN** a consumer queries documents for a specific Customer
- **THEN** documents can be filtered by `OwnerId` and `OwnerType = "Customer"` using the document-management retrieval capability

### Requirement: CustomerCreated event includes financial metadata when provided
Both customer creation paths SHALL populate the `Metadata` field of the `CustomerCreated` outbox event payload with financial data when available. If no financial data is available, `Metadata` SHALL be `null` — no error SHALL be raised.

- **Direct path** (`POST /api/v1/customers`): `Metadata` is populated from the fields `CreditScore`, `MonthlyIncome`, and `MonthlyDebt` provided in the request body. If absent or null, they are omitted from `Metadata`.
- **Conversion path** (Prospect → CreditApplication → Approve): `Metadata["CreditScore"]` is populated from `TotalScore` of the most recent `RiskEvaluation` for the `CreditApplication`. `MonthlyIncome` and `MonthlyDebt` are not included in this path.

#### Scenario: CustomerCreated with financial metadata populated — direct path
- **WHEN** `POST /api/v1/customers` is called with `CreditScore`, `MonthlyIncome`, and `MonthlyDebt` in the body
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata["CreditScore"]`, `Metadata["MonthlyIncome"]`, and `Metadata["MonthlyDebt"]` set to the provided values

#### Scenario: CustomerCreated with partial metadata — direct path
- **WHEN** `POST /api/v1/customers` is called with only `CreditScore` in the body
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata["CreditScore"]` set and `MonthlyIncome` / `MonthlyDebt` absent — no error is raised

#### Scenario: CustomerCreated without metadata — direct path
- **WHEN** `POST /api/v1/customers` is called without financial fields
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata` as `null`

#### Scenario: CustomerCreated with risk score — conversion path
- **WHEN** a CreditApplication is approved and the conversion completes successfully
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata["CreditScore"]` set to the `TotalScore` of the most recent `RiskEvaluation` for that `CreditApplication`

#### Scenario: CustomerCreated without evaluation — conversion path edge case
- **WHEN** a CreditApplication is approved but no `RiskEvaluation` exists for it
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata` as `null` — no error is raised

### Requirement: Customer profile changes persist a CustomerUpdated event to the outbox
The system SHALL persist a `CustomerUpdated` outbox event with a typed, versioned contract via `IOutboxWriter.AppendAsync()` whenever a Customer's profile is successfully updated via `PUT /api/v1/customers/{id}`. The event payload SHALL include the customer Id, updated fields, timestamp, and a version number. The `outbox-worker` delivers it to RabbitMQ.

#### Scenario: CustomerUpdated persisted to outbox after profile update
- **WHEN** `PUT /api/v1/customers/{id}` is called with valid data and the update succeeds
- **THEN** a `CustomerUpdated` outbox event is persisted with a typed contract containing CustomerId, the changed field values, UpdatedAt timestamp, and Version

#### Scenario: CustomerUpdated not persisted on failure
- **WHEN** `PUT /api/v1/customers/{id}` fails (customer not found, or validation error)
- **THEN** no `CustomerUpdated` outbox event is persisted
