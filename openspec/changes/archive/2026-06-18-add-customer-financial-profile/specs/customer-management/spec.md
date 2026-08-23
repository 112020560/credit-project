## MODIFIED Requirements

### Requirement: CustomerCreated event includes financial metadata when provided
Both customer creation paths SHALL populate the `Metadata` field of the `CustomerCreated` event with financial data when the creation request includes it. If `metadata` is absent or a key is missing, the corresponding entry in `Metadata` SHALL be `null` or omitted — no error SHALL be raised.

#### Scenario: CustomerCreated with financial metadata populated
- **WHEN** `POST /api/v1/customers` is called with a body that includes `metadata` containing `CreditScore`, `MonthlyIncome`, and `MonthlyDebt`
- **THEN** a Customer is created and `CustomerCreated` is published with `Metadata["CreditScore"]`, `Metadata["MonthlyIncome"]`, and `Metadata["MonthlyDebt"]` set to the provided values

#### Scenario: CustomerCreated with partial metadata
- **WHEN** `POST /api/v1/customers` is called with a body that includes `metadata` containing only `CreditScore`
- **THEN** `CustomerCreated` is published with `Metadata["CreditScore"]` set and `MonthlyIncome` / `MonthlyDebt` absent or null — no error is raised

#### Scenario: CustomerCreated without metadata
- **WHEN** `POST /api/v1/customers` is called without a `metadata` field
- **THEN** `CustomerCreated` is published with `Metadata` as `null` — identical behavior to before this change
