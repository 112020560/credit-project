## ADDED Requirements

### Requirement: Create prospect with minimum identity data
The system SHALL allow creating a Prospect with only identification, full name, and one contact (email or phone). All other fields are optional at creation time. The Prospect SHALL be created with Status `Draft`.

#### Scenario: Successful prospect creation
- **WHEN** a valid `POST /api/v1/prospects` request is received with IdentificationType, IdentificationNumber, FullName, and at least one contact
- **THEN** a Prospect is persisted with Status `Draft` and a `ProspectCreated` event is published

#### Scenario: Duplicate identification rejected
- **WHEN** a `POST /api/v1/prospects` is received with an IdentificationNumber that already exists (regardless of Status)
- **THEN** the system SHALL return 409 Conflict and no Prospect is created

#### Scenario: Missing required fields rejected
- **WHEN** a `POST /api/v1/prospects` is received without IdentificationType, IdentificationNumber, FullName, or any contact
- **THEN** the system SHALL return 400 Bad Request with field-level validation errors

---

### Requirement: Enrich prospect progressively
The system SHALL allow adding or updating addresses, contacts, work info, and fiscal info to an existing Prospect in `Draft` or `Submitted` status via dedicated endpoints. Enrichment SHALL NOT be allowed on Prospects with Status `Converted` or `Rejected`.

#### Scenario: Add address to draft prospect
- **WHEN** a valid request is received to add an address to a Prospect in `Draft` status
- **THEN** the address is persisted and associated to the Prospect

#### Scenario: Enrichment blocked on converted prospect
- **WHEN** a request is received to enrich a Prospect with Status `Converted`
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Prospect status lifecycle
The system SHALL enforce the following status transitions:
- `Draft` → `Submitted` (when a CreditApplication is submitted)
- `Draft` → `Converted` (not allowed directly)
- `Submitted` → `Converted` (when a linked CreditApplication is approved)
- `Submitted` → `Draft` (when the linked CreditApplication is rejected, allowing retry)
- `Converted` is a terminal status — no further transitions

#### Scenario: Prospect returns to Draft after rejection
- **WHEN** a linked CreditApplication transitions to `Rejected`
- **THEN** the Prospect Status is set back to `Draft` and a new CreditApplication may be created
