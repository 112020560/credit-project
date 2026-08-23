## MODIFIED Requirements

### Requirement: Customer financial profile can be updated partially via PATCH endpoint
The system SHALL expose `PATCH /api/v1/customers/{id}/financials` to update one or more financial fields (`CreditScore`, `MonthlyIncome`, `MonthlyDebt`) of an existing Customer. Only fields explicitly included in the request body SHALL be updated; absent fields SHALL retain their current value (COALESCE semantics). After a successful update, the system SHALL persist a `CustomerUpdated` outbox event via `IOutboxWriter.AppendAsync()` whose `Changes` dictionary contains only the fields that were included in the request. The `outbox-worker` delivers it to RabbitMQ.

#### Scenario: All three fields updated — full Changes in outbox event
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with `CreditScore`, `MonthlyIncome`, and `MonthlyDebt`
- **THEN** all three columns are updated in the database and a `CustomerUpdated` outbox event is persisted with `Changes` containing the three updated keys

#### Scenario: Partial update — partial Changes in outbox event
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with only `CreditScore` and `MonthlyIncome`
- **THEN** only those two columns are updated and a `CustomerUpdated` outbox event is persisted with `Changes` containing only `CreditScore` and `MonthlyIncome`

#### Scenario: Customer not found — no outbox event
- **WHEN** `PATCH /api/v1/customers/{id}/financials` is called with an unknown `id`
- **THEN** the system returns HTTP 404 and no outbox event is persisted
