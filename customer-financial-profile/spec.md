## Purpose

Defines how financial profile data (CreditScore, MonthlyIncome, MonthlyDebt) is stored and updated on a Customer. Financial fields are optional and can be set at creation time or updated independently via a dedicated PATCH endpoint.

## Requirements

### Requirement: Customer financial profile can be updated partially via PATCH endpoint
The system SHALL expose `PATCH /api/v1/customers/{id}/financials` to update one or more financial fields (`CreditScore`, `MonthlyIncome`, `MonthlyDebt`) of an existing Customer. Only fields explicitly included in the request body SHALL be updated; absent fields SHALL retain their current value (COALESCE semantics). After a successful update, the system SHALL persist a `CustomerUpdated` outbox event via `IOutboxWriter.AppendAsync()` whose `Changes` dictionary contains only the fields that were included in the request. The `outbox-worker` delivers it to RabbitMQ.

#### Scenario: Partial update with all three fields
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with `{ "CreditScore": 780, "MonthlyIncome": 5500.00, "MonthlyDebt": 300.00 }`
- **THEN** all three columns are updated in the database, and a `CustomerUpdated` outbox event is persisted with `Changes` containing the three updated keys

#### Scenario: Partial update with subset of fields
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with `{ "CreditScore": 780, "MonthlyIncome": 5500.00 }`
- **THEN** only `CreditScore` and `MonthlyIncome` are updated; `MonthlyDebt` retains its current value in the database
- **AND** a `CustomerUpdated` outbox event is persisted with `Changes` containing only `CreditScore` and `MonthlyIncome`

#### Scenario: Update for non-existent customer returns 404
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with a `customerId` that does not exist
- **THEN** the system returns HTTP 404 and no outbox event is persisted

#### Scenario: Empty body returns validation error
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with an empty body (no fields)
- **THEN** the system returns HTTP 422 with a validation error indicating at least one field is required

### Requirement: Financial data persisted as nullable columns on Customer
The system SHALL store `CreditScore`, `MonthlyIncome`, and `MonthlyDebt` as nullable columns on the `Customers` table. NULL values SHALL be stored without error when the data is not provided.

#### Scenario: Customer created without financial data has null columns
- **WHEN** a Customer is created via `POST /api/v1/customers` without financial metadata
- **THEN** `CreditScore`, `MonthlyIncome`, and `MonthlyDebt` are stored as NULL in the database

#### Scenario: Customer created with financial data has populated columns
- **WHEN** a Customer is created via `POST /api/v1/customers` with `metadata` containing `CreditScore`, `MonthlyIncome`, and `MonthlyDebt`
- **THEN** the corresponding columns are populated with the provided values
