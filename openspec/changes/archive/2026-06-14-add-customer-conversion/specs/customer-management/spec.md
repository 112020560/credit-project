## ADDED Requirements

### Requirement: Customer profile changes publish a CustomerUpdated event
The system SHALL publish a `CustomerUpdated` event with a typed, versioned contract whenever a Customer's profile is successfully updated via `PUT /api/v1/customers/{id}`. The event payload SHALL include the customer Id, updated fields, timestamp, and a version number.

#### Scenario: CustomerUpdated published after profile update
- **WHEN** `PUT /api/v1/customers/{id}` is called with valid data and the update succeeds
- **THEN** a `CustomerUpdated` event is published with a typed contract containing CustomerId, the changed field values, UpdatedAt timestamp, and Version

#### Scenario: CustomerUpdated not published on failure
- **WHEN** `PUT /api/v1/customers/{id}` fails (customer not found, or validation error)
- **THEN** no `CustomerUpdated` event is published
