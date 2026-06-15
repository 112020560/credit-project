## Why

Loan origination and customer onboarding require collecting, storing references to, and validating identity and financial documents from customers. The existing `ApplicationDocument` entity (scoped to a `CreditApplication`) only covers documents in the context of a single application. A standalone Document Management module is needed to manage documents as first-class entities with their own validation lifecycle — enabling reuse across customers, applications, and future regulatory workflows.

## What Changes

- Introduce a `Document` aggregate with properties: Id, OwnerId, OwnerType, DocumentTypeCode, StorageUrl, Status, UploadedAt, UpdatedAt
- Introduce a `DocumentType` configuration entity: Code, Name, Description, IsRequired
- Introduce a `DocumentValidation` entity to record agent review decisions: Id, DocumentId, Decision, RejectionReason, ReviewedBy, ReviewedAt
- Expose `POST /api/v1/documents` to register a document reference (client handles upload to storage)
- Expose `GET /api/v1/documents/{id}` to retrieve a document with its latest validation status
- Expose `POST /api/v1/documents/{id}/validate` to allow an agent to approve or reject a document
- Publish `DocumentUploaded` on registration, `DocumentValidated` on approval, `DocumentRejected` on rejection

## Capabilities

### New Capabilities

- `document-management`: Covers the full document lifecycle — registration, retrieval, and agent validation. Defines `Document`, `DocumentType`, and `DocumentValidation` entities, their status transitions (`Uploaded → Validated | Rejected`), and the three API endpoints.

### Modified Capabilities

- `customer-management`: Documents can now be owned by a Customer (`OwnerType = Customer`). The spec SHOULD note that customer documents may be queried via the document-management capability rather than a nested endpoint under `/customers/{id}`.

## Impact

- **Crm.Domain**: New aggregates `Document`, `DocumentType`, `DocumentValidation`; new domain errors; `DocumentStatus` constants
- **Crm.Application**: New commands (`RegisterDocumentCommand`, `ValidateDocumentCommand`) and query (`GetDocumentByIdQuery`); FluentValidation validators; DTOs
- **Crm.Infrastructure**: New EF mappings and repositories (`IDocumentsRepository`, `IDocumentTypesRepository`); new migration `AddDocumentManagement`
- **Crm.WebApi**: 3 new endpoints under tag `Documents`
- **Events**: `DocumentUploaded`, `DocumentValidated`, `DocumentRejected` contracts
- **No breaking changes** to existing `ApplicationDocument` or `CustomerDocument` entities — this is an additive module
