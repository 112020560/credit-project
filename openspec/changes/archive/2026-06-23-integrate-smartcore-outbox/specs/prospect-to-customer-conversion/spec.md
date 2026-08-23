## MODIFIED Requirements

### Requirement: Automatic conversion on approval
When a CreditApplication is approved, the system SHALL atomically create a Customer from the Prospect, set the Prospect Status to `Converted`, and persist the events `CreditApplicationApproved`, `ProspectConvertedToCustomer`, and `CustomerCreated` to the outbox via `IOutboxWriter.AppendAsync()`. The `outbox-worker` delivers them to RabbitMQ asynchronously.

#### Scenario: Approval triggers conversion and outbox events
- **WHEN** an agent approves a CreditApplication with Status `InReview`
- **THEN** a Customer is created with Status `Active` using all data accumulated on the Prospect (identity, contacts, addresses, work info, fiscal info), the Prospect Status is set to `Converted`, and outbox events `CreditApplicationApproved`, `ProspectConvertedToCustomer`, and `CustomerCreated` are persisted to `outbox_db`

#### Scenario: Approval failure — no state change, no outbox events
- **WHEN** the approval process fails (e.g., database error)
- **THEN** the CreditApplication status remains `InReview`, Prospect status remains `Submitted`, no outbox events are persisted, and the system returns 500

### Requirement: CustomerCreated event is persisted to the outbox from both origination and direct paths
The system SHALL persist a `CustomerCreated` outbox event whenever a Customer is created, regardless of whether it came from the origination path or the direct `POST /customers` path. The event payload SHALL be identical in structure.

#### Scenario: CustomerCreated persisted after conversion
- **WHEN** a Prospect is approved and conversion completes
- **THEN** a `CustomerCreated` outbox event is persisted via `IOutboxWriter.AppendAsync()`, identical in payload structure to the one persisted by direct customer creation
