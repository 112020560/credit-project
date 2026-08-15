## MODIFIED Requirements

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
