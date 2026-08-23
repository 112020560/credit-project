## MODIFIED Requirements

### Requirement: Customer can be created via two independent paths
The system SHALL support two distinct paths for Customer creation, both resulting in a Customer with Status `Active`:
1. **Direct path**: `POST /api/v1/customers` (existing, unchanged) — for cash customers with no onboarding
2. **Origination path**: Prospect → CreditApplication → Approve → automatic conversion (new)

Both paths SHALL publish a `CustomerCreated` event with the same contract structure.

#### Scenario: Direct creation remains functional
- **WHEN** `POST /api/v1/customers` is called with valid data
- **THEN** a Customer is created with Status `Active` and `CustomerCreated` is published — identical behavior to before this change

#### Scenario: Origination path creates customer on approval
- **WHEN** a CreditApplication is approved and conversion succeeds
- **THEN** a Customer is created with Status `Active` from Prospect data and `CustomerCreated` is published

#### Scenario: No cross-contamination between paths
- **WHEN** a Customer is created via the direct path
- **THEN** no Prospect or CreditApplication record is created or required
