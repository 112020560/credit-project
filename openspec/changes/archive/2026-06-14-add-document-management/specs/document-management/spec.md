## ADDED Requirements

### Requirement: Register a document reference
The system SHALL allow registering a `Document` by providing an owner reference (`OwnerId`, `OwnerType`), a `DocumentTypeCode`, and a `StorageUrl`. The client is responsible for uploading the file to external storage before registering. The document SHALL be created with Status `Uploaded`.

#### Scenario: Successful document registration
- **WHEN** `POST /api/v1/documents` is called with a valid `OwnerId`, `OwnerType`, `DocumentTypeCode`, and `StorageUrl`
- **THEN** a Document is persisted with Status `Uploaded` and a `DocumentUploaded` event is published

#### Scenario: Registration with unknown document type rejected
- **WHEN** `POST /api/v1/documents` is called with a `DocumentTypeCode` not in the configured catalog
- **THEN** the system SHALL return 400 Bad Request

#### Scenario: Registration with missing required fields rejected
- **WHEN** `POST /api/v1/documents` is called without `OwnerId`, `OwnerType`, `DocumentTypeCode`, or `StorageUrl`
- **THEN** the system SHALL return 400 Bad Request with field-level validation errors

---

### Requirement: Retrieve a document with validation status
The system SHALL allow retrieving a `Document` by Id. The response SHALL include the document's current status, all validation history entries, and the latest validation decision if any.

#### Scenario: Successful retrieval
- **WHEN** `GET /api/v1/documents/{id}` is called for an existing Document
- **THEN** the system returns the Document including its `DocumentTypeCode`, `Status`, `StorageUrl`, and list of `DocumentValidation` records ordered by `ReviewedAt` descending

#### Scenario: Document not found
- **WHEN** `GET /api/v1/documents/{id}` is called for a non-existent Id
- **THEN** the system SHALL return 404 Not Found

---

### Requirement: Agent validates or rejects a document
The system SHALL allow an agent to validate or reject a `Document` in `Uploaded` status. Validation SHALL transition the Document to `Validated`. Rejection SHALL transition it to `Rejected` and require a non-empty rejection reason. Each decision is recorded as a new `DocumentValidation` entry.

#### Scenario: Successful validation
- **WHEN** `POST /api/v1/documents/{id}/validate` is called with `Decision: "Validated"` on an `Uploaded` document
- **THEN** Document Status transitions to `Validated`, a `DocumentValidation` record is persisted, and a `DocumentValidated` event is published

#### Scenario: Successful rejection
- **WHEN** `POST /api/v1/documents/{id}/validate` is called with `Decision: "Rejected"` and a non-empty `RejectionReason` on an `Uploaded` document
- **THEN** Document Status transitions to `Rejected`, a `DocumentValidation` record is persisted, and a `DocumentRejected` event is published

#### Scenario: Rejection without reason blocked
- **WHEN** `POST /api/v1/documents/{id}/validate` is called with `Decision: "Rejected"` and an empty or missing `RejectionReason`
- **THEN** the system SHALL return 400 Bad Request

#### Scenario: Validation of already-validated or rejected document blocked
- **WHEN** `POST /api/v1/documents/{id}/validate` is called on a Document not in `Uploaded` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Document status lifecycle
The system SHALL enforce the following status transitions:
- `Uploaded` → `Validated` (agent approves)
- `Uploaded` → `Rejected` (agent rejects)
- `Validated` and `Rejected` are terminal — no further transitions allowed on the same Document

#### Scenario: Terminal status cannot be re-validated
- **WHEN** `POST /api/v1/documents/{id}/validate` is called on a Document in `Validated` or `Rejected` status
- **THEN** the system SHALL return 422 Unprocessable Entity

#### Scenario: New document must be registered after rejection
- **WHEN** a document is in `Rejected` status and the owner needs to resubmit
- **THEN** the owner SHALL register a new Document via `POST /api/v1/documents` with the corrected file URL

---

### Requirement: Document validation history is preserved
The system SHALL retain all `DocumentValidation` records for a Document. Each record SHALL capture the decision, rejection reason (if any), reviewer identity, and timestamp.

#### Scenario: Multiple validation attempts are all recorded
- **WHEN** a Document transitions through multiple review cycles (e.g., Uploaded → Rejected → new Document Uploaded → Validated)
- **THEN** each `DocumentValidation` record is preserved and returned in the document retrieval response
