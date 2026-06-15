## ADDED Requirements

### Requirement: Register document for a credit application
The system SHALL allow registering an `ApplicationDocument` against a CreditApplication in `Draft` or `Submitted` status. The document record stores a URL (the client is responsible for uploading to external storage). Status SHALL be set to `Uploaded` on registration.

#### Scenario: Successful document registration
- **WHEN** `POST /api/v1/credit-applications/{id}/documents` is received with a valid Type and StorageUrl on a `Draft` or `Submitted` application
- **THEN** an ApplicationDocument is persisted with Status `Uploaded` and associated to the CreditApplication

#### Scenario: Document registration blocked on non-editable application
- **WHEN** `POST /api/v1/credit-applications/{id}/documents` is called on an application in `InReview`, `Approved`, or `Rejected` status
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: ApplicationDocument is scoped to CreditApplication, not Customer
The system SHALL store ApplicationDocuments separately from CustomerDocuments. ApplicationDocuments SHALL NOT be automatically transferred to the Customer record upon conversion. Post-conversion documents follow the existing CustomerDocument path.

#### Scenario: Application documents remain after conversion
- **WHEN** a Prospect is converted to a Customer
- **THEN** all ApplicationDocuments remain associated to the CreditApplication (archived) and are NOT copied to CustomerDocuments

---

### Requirement: Document types are validated
The system SHALL only accept documents of known types (e.g., NationalId, Passport, IncomeProof, BankStatement, TaxRegistration). Unknown types SHALL be rejected.

#### Scenario: Unknown document type rejected
- **WHEN** a document registration request is received with an unrecognized Type value
- **THEN** the system SHALL return 400 Bad Request

---

### Requirement: Required documents enforced at submit
The system SHALL maintain a configurable list of required document types per application. The submit endpoint SHALL validate all required types have at least one document in `Uploaded` or `Verified` status.

#### Scenario: Submit blocked due to missing required document
- **WHEN** `/submit` is called and a required document type has no registered document
- **THEN** the system SHALL return 422 Unprocessable Entity listing the missing document types
