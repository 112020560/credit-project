## MODIFIED Requirements

### Requirement: Customer can be created via two independent paths
The system SHALL support two distinct paths for Customer creation, both resulting in a Customer with Status `CustomerStatus.Active` (the constant, not a magic string literal). All other behavior remains unchanged.

#### Scenario: Direct creation uses CustomerStatus.Active constant
- **WHEN** `POST /api/v1/customers` is called with valid data
- **THEN** a Customer is created with `Status = CustomerStatus.Active` (using the constant from `CustomerStatus` class) and a `CustomerCreated` event is persisted to `outbox_db`

#### Scenario: Origination path creates customer on approval
- **WHEN** a CreditApplication is approved and conversion succeeds
- **THEN** a Customer is created with `Status = CustomerStatus.Active` from Prospect data and a `CustomerCreated` event is persisted to `outbox_db`
