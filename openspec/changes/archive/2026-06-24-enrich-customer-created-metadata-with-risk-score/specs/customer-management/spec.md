## MODIFIED Requirements

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
