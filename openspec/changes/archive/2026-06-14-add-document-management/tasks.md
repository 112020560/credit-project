## 1. Domain — Document Aggregate

- [x] 1.1 Create `Document` entity in `Crm.Domain/Documents/` with properties: Id, OwnerId, OwnerType, DocumentTypeCode, StorageUrl, Status, UploadedAt, UpdatedAt
- [x] 1.2 Create `DocumentValidation` entity with properties: Id, DocumentId, Decision, RejectionReason, ReviewedBy, ReviewedAt
- [x] 1.3 Create `DocumentType` entity with properties: Code (string PK), Name, Description, IsRequired
- [x] 1.4 Create `DocumentStatus` static class with constants: `Uploaded`, `Validated`, `Rejected`
- [x] 1.5 Create `DocumentValidationDecision` static class with constants: `Validated`, `Rejected`
- [x] 1.6 Create `DocumentError` static class with domain errors (NotFound, InvalidDocumentType, InvalidTransition, RejectionReasonRequired)

## 2. Domain — Persistence Abstractions

- [x] 2.1 Create `IDocumentsRepository` interface in `Crm.Domain/Abstractions/Persistence/` with: `AddAsync`, `GetByIdAsync`, `UpdateAsync`
- [x] 2.2 Create `IDocumentTypesRepository` interface with: `GetByCodeAsync`, `ListAllAsync`
- [x] 2.3 Add `IDocumentsRepository` and `IDocumentTypesRepository` to `IUnitOfWork`

## 3. Infrastructure — Database

- [x] 3.1 Add `DbSet<Document>`, `DbSet<DocumentValidation>`, `DbSet<DocumentType>` to `CrmDbContext`
- [x] 3.2 Configure EF mappings for `Document` (table: `documents`; indexes on `OwnerId + OwnerType`; columns snake_case)
- [x] 3.3 Configure EF mappings for `DocumentValidation` (table: `document_validations`; FK to `documents`; index on `document_id`)
- [x] 3.4 Configure EF mappings for `DocumentType` (table: `document_types`; string PK on `code`)
- [x] 3.5 Run `dotnet ef migrations add AddDocumentManagement` and verify generated SQL
- [x] 3.6 Seed `DocumentType` records in migration `Up`: `NationalId`, `Passport`, `IncomeProof`, `BankStatement`, `TaxRegistration`, `ProofOfAddress`

## 4. Infrastructure — Repositories

- [x] 4.1 Implement `DocumentsRepository : IDocumentsRepository` (include `DocumentValidation` on `GetByIdAsync`)
- [x] 4.2 Implement `DocumentTypesRepository : IDocumentTypesRepository`
- [x] 4.3 Add lazy-loaded `DocumentsRepository` and `DocumentTypesRepository` properties to `UnitOfWork`
- [x] 4.4 Register both repositories in `Crm.Infrastructure/DependencyInjection.cs`

## 5. Application — DTOs

- [x] 5.1 Create `RegisterDocumentDto` with: OwnerId, OwnerType, DocumentTypeCode, StorageUrl
- [x] 5.2 Create `ValidateDocumentDto` with: Decision, RejectionReason (nullable)
- [x] 5.3 Create `DocumentValidationDto` with: Id, Decision, RejectionReason, ReviewedBy, ReviewedAt
- [x] 5.4 Create `DocumentDetailDto` with: Id, OwnerId, OwnerType, DocumentTypeCode, StorageUrl, Status, UploadedAt, UpdatedAt, Validations (list of DocumentValidationDto)

## 6. Application — Commands & Queries

- [x] 6.1 Create `RegisterDocumentCommand(RegisterDocumentDto Dto)` + handler: validates DocumentTypeCode exists, validates owner fields not empty, persists Document with Status `Uploaded`, publishes `DocumentUploaded` event
- [x] 6.2 Create `ValidateDocumentCommand(Guid DocumentId, ValidateDocumentDto Dto)` + handler: loads Document, validates Status is `Uploaded`, adds `DocumentValidation` record, updates Document Status to `Validated` or `Rejected`, publishes `DocumentValidated` or `DocumentRejected` event
- [x] 6.3 Create `GetDocumentByIdQuery(Guid DocumentId)` + handler: loads Document with validations, returns `DocumentDetailDto`
- [x] 6.4 Add FluentValidation validators for `RegisterDocumentCommand` (all fields required, non-empty OwnerId) and `ValidateDocumentCommand` (Decision required; RejectionReason required when Decision is `Rejected`)

## 7. Event Contracts

- [x] 7.1 Add `DocumentUploaded` contract: `{ DocumentId, OwnerId, OwnerType, DocumentTypeCode, StorageUrl, UploadedAt }`
- [x] 7.2 Add `DocumentValidated` contract: `{ DocumentId, OwnerId, OwnerType, ReviewedBy, ReviewedAt }`
- [x] 7.3 Add `DocumentRejected` contract: `{ DocumentId, OwnerId, OwnerType, RejectionReason, ReviewedBy, ReviewedAt }`

## 8. WebApi — Endpoints

- [x] 8.1 Create `Crm.WebApi/Endpoints/Documents/Register.cs` — `POST /api/v1/documents` → `RegisterDocumentCommand` → 201 Created with `DocumentDetailDto`
- [x] 8.2 Create `Crm.WebApi/Endpoints/Documents/GetById.cs` — `GET /api/v1/documents/{id}` → `GetDocumentByIdQuery` → 200 OK with `DocumentDetailDto`
- [x] 8.3 Create `Crm.WebApi/Endpoints/Documents/Validate.cs` — `POST /api/v1/documents/{id}/validate` → `ValidateDocumentCommand` → 204 No Content
- [x] 8.4 Add `WithTags("Documents")`, `WithOpenApi`, `MapToApiVersion(v1)`, and correct `Produces` to all three endpoints
