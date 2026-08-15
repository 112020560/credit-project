## Purpose

Defines the rules for automatic, atomic, and immutable conversion of an approved Prospect into an active Customer, including audit trail preservation and outbox event publishing.

## Requirements

### Requirement: Automatic conversion on approval
The system SHALL automatically convert a Prospect to a Customer when a linked CreditApplication is approved. The conversion SHALL be atomic — both the Prospect archival and Customer creation MUST succeed or both MUST be rolled back.

#### Scenario: Successful conversion
- **WHEN** `POST /api/v1/credit-applications/{id}/approve` is called successfully
- **THEN** a Customer is created with Status `Active` using all data accumulated on the Prospect (identity, contacts, addresses, work info, fiscal info), the Prospect Status is set to `Converted`, and outbox events `CreditApplicationApproved`, `ProspectConvertedToCustomer`, and `CustomerCreated` are persisted to `outbox_db` via `IOutboxWriter.AppendAsync()`. The `outbox-worker` delivers them to RabbitMQ asynchronously.

#### Scenario: Conversion failure rolls back approval
- **WHEN** Customer creation fails during the conversion transaction
- **THEN** the CreditApplication status remains `InReview`, Prospect status remains `Submitted`, no outbox events are persisted, and the system returns 500

---

### Requirement: Prospect data is immutable after conversion
The system SHALL set the Prospect to `Converted` status upon successful conversion. No further data modifications SHALL be accepted on a Converted Prospect. The Prospect record is the permanent audit trail of what was declared.

#### Scenario: Modification of converted prospect blocked
- **WHEN** any enrichment or update request is received for a Prospect in `Converted` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Agent cannot modify data before approving
The system SHALL NOT expose any endpoint that allows an agent to edit Prospect or CreditApplication data. The approve and reject endpoints accept only a decision (and a rejection reason for rejections).

#### Scenario: No edit endpoint exists for agents
- **WHEN** an agent reviews a CreditApplication in `InReview` status
- **THEN** all data is read-only; the only actions available are `/approve` and `/reject`

---

### Requirement: CustomerCreated event is persisted to the outbox from both origination and direct paths
The system SHALL persist a `CustomerCreated` outbox event whenever a Customer is created, regardless of whether it came from the origination path or the direct `POST /customers` path. The event payload structure SHALL be identical, though the `Metadata` content differs by path:
- **Direct path**: `Metadata` contains financial fields from the request body (`CreditScore`, `MonthlyIncome`, `MonthlyDebt`) if provided.
- **Conversion path**: `Metadata["CreditScore"]` is set from the `TotalScore` of the most recent `RiskEvaluation` for the `CreditApplication`. `MonthlyIncome` and `MonthlyDebt` are not available and are omitted.

#### Scenario: CustomerCreated persisted after conversion — with risk score in Metadata
- **WHEN** a Prospect is successfully converted to a Customer via approval
- **THEN** a `CustomerCreated` outbox event is persisted via `IOutboxWriter.AppendAsync()` with `Metadata["CreditScore"]` set to the `TotalScore` of the most recent `RiskEvaluation` for the `CreditApplication`

#### Scenario: CustomerCreated persisted after conversion — no evaluation available
- **WHEN** a Prospect is successfully converted to a Customer but no `RiskEvaluation` exists for the `CreditApplication`
- **THEN** a `CustomerCreated` outbox event is persisted with `Metadata` as `null`
