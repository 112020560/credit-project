## ADDED Requirements

### Requirement: Customer documents are managed via the document-management capability
The system SHALL allow registering documents owned by a Customer using the document-management module by specifying `OwnerId = CustomerId` and `OwnerType = "Customer"`. Customer documents SHALL NOT require a separate nested endpoint under `/customers/{id}/documents` in this slice.

#### Scenario: Register document for a customer
- **WHEN** `POST /api/v1/documents` is called with `OwnerId` set to a valid Customer Id and `OwnerType = "Customer"`
- **THEN** a Document is registered and associated to that Customer via the polymorphic ownership model

#### Scenario: Retrieve customer documents
- **WHEN** a consumer queries documents for a specific Customer
- **THEN** documents can be filtered by `OwnerId` and `OwnerType = "Customer"` using the document-management retrieval capability
