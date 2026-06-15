## Purpose

Defines how customers are created and managed. Customers can be created via two independent paths: direct creation for cash customers, and automatic conversion from an approved Prospect for credit customers.

## Requirements

### Requirement: Customer can be created via two independent paths
The system SHALL support two distinct paths for Customer creation, both resulting in a Customer with Status `Active`:
1. **Direct path**: `POST /api/v1/customers` (existing) — for cash customers with no onboarding
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

### Requirement: Customer documents are managed via the document-management capability
The system SHALL allow registering documents owned by a Customer using the document-management module by specifying `OwnerId = CustomerId` and `OwnerType = "Customer"`. Customer documents SHALL NOT require a separate nested endpoint under `/customers/{id}/documents` in this slice.

#### Scenario: Register document for a customer
- **WHEN** `POST /api/v1/documents` is called with `OwnerId` set to a valid Customer Id and `OwnerType = "Customer"`
- **THEN** a Document is registered and associated to that Customer via the polymorphic ownership model

#### Scenario: Retrieve customer documents
- **WHEN** a consumer queries documents for a specific Customer
- **THEN** documents can be filtered by `OwnerId` and `OwnerType = "Customer"` using the document-management retrieval capability

### Requirement: Customer profile changes publish a CustomerUpdated event
The system SHALL publish a `CustomerUpdated` event with a typed, versioned contract whenever a Customer's profile is successfully updated via `PUT /api/v1/customers/{id}`. The event payload SHALL include the customer Id, updated fields, timestamp, and a version number.

#### Scenario: CustomerUpdated published after profile update
- **WHEN** `PUT /api/v1/customers/{id}` is called with valid data and the update succeeds
- **THEN** a `CustomerUpdated` event is published with a typed contract containing CustomerId, the changed field values, UpdatedAt timestamp, and Version

#### Scenario: CustomerUpdated not published on failure
- **WHEN** `PUT /api/v1/customers/{id}` fails (customer not found, or validation error)
- **THEN** no `CustomerUpdated` event is published
