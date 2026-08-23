## ADDED Requirements

### Requirement: ExternalCustomerRef entity replaces CustomersRef
The class currently named `CustomersRef` SHALL be renamed to `ExternalCustomerRef` in `Crm.Domain/Customers/ExternalCustomerRef.cs`. It represents a reference to a customer that was originated in an external system and does not have a Customer record in this CRM. It SHALL include an XML doc comment explaining its purpose.

#### Scenario: Class name and comment express purpose
- **WHEN** a developer encounters `ExternalCustomerRef` in the codebase
- **THEN** the name and its XML doc comment make it immediately clear that this entity represents a customer reference from an external system

### Requirement: RegisterExternalCustomer endpoint
The system SHALL expose a `POST /api/v1/external-customers` endpoint that allows registering or updating a reference to an external customer. This endpoint is the controlled entry point for external systems to write data into the CRM — no external system SHALL write directly to the database table.

#### Scenario: Register new external customer reference
- **WHEN** `POST /api/v1/external-customers` is called with a valid payload including `ExternalId`, `DisplayName`, `LegalName`, and `RiskScore`
- **THEN** an `ExternalCustomerRef` record is created or updated (upsert by `ExternalId`) and persisted via `IExternalCustomerRefRepository`

#### Scenario: Update existing external customer reference
- **WHEN** `POST /api/v1/external-customers` is called with an `ExternalId` that already exists
- **THEN** the existing record is updated with the new values and `Version` is incremented

### Requirement: IExternalCustomerRefRepository in domain
The system SHALL define `IExternalCustomerRefRepository` in `Crm.Domain/Abstractions/Persistence/` with `UpsertAsync(ExternalCustomerRef, CancellationToken)` and `GetByExternalIdAsync(Guid, CancellationToken)` methods. The implementation lives in `Crm.Infrastructure`.

#### Scenario: Repository interface hides persistence details
- **WHEN** the Application layer registers an ExternalCustomerRef
- **THEN** it calls `IExternalCustomerRefRepository.UpsertAsync()` — it has no knowledge of the underlying SQL or EF Core implementation
