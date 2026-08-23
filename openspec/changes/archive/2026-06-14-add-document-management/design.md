## Context

The platform currently has two document concepts that are tightly scoped:
- `ApplicationDocument` — documents attached to a `CreditApplication` (managed within the credit origination flow)
- `CustomerDocument` — documents attached to a `Customer` (stored but not validated)

Neither provides a reusable, ownership-agnostic document management lifecycle. Regulatory and operational requirements demand a standalone module where documents can be registered by any owner (Customer, future entities), tracked by type, and reviewed by agents with a full audit trail.

This design introduces `Document` as a first-class aggregate with an agent validation workflow.

## Goals / Non-Goals

**Goals:**
- Generic document registration via URL reference (client handles storage upload)
- Polymorphic ownership: a `Document` belongs to any entity via `OwnerId` + `OwnerType`
- Agent validation workflow: `Uploaded → Validated | Rejected`
- Immutable validation audit trail via `DocumentValidation` records
- Events on each state transition: `DocumentUploaded`, `DocumentValidated`, `DocumentRejected`
- Seeded `DocumentType` catalog (not user-manageable in this slice)

**Non-Goals:**
- File upload or storage management (client responsibility)
- Document expiry or re-validation scheduling
- Automatic linking to `ApplicationDocument` or `CustomerDocument` tables
- Admin API for managing `DocumentType` catalog
- Multi-step validation workflows (single agent decision per review)

## Decisions

### 1. Polymorphic ownership (`OwnerId` + `OwnerType`) over typed FK columns

**Decision**: `Document` stores `OwnerId: Guid` and `OwnerType: string` (e.g., `"Customer"`, `"Prospect"`) rather than nullable FK columns per entity type.

**Rationale**: New owner types can be added without schema changes. The module remains decoupled from specific domain aggregates.

**Alternative considered**: Separate nullable FKs (`CustomerId?`, `ProspectId?`). Rejected — every new owner type requires a migration and grows the schema with nullable columns.

---

### 2. Validation history via `DocumentValidation` child table

**Decision**: Every agent review produces a new `DocumentValidation` row. The current status is always the decision of the latest record. The `Document.Status` column mirrors it for fast querying.

**Rationale**: Provides an immutable audit trail of all review decisions. Agents can see who validated/rejected and when.

**Alternative considered**: Overwrite a single `LastValidatedBy` / `LastValidatedAt` on `Document`. Rejected — loses history, fails audit requirements.

---

### 3. `DocumentType` as seeded configuration, not API-managed

**Decision**: `DocumentType` records are seeded in the migration. No CRUD API is exposed in this slice.

**Rationale**: The type catalog is stable and agreed with compliance. Exposing an admin API adds surface area and scope beyond what's needed now.

---

### 4. No hard link to `ApplicationDocument` or `CustomerDocument`

**Decision**: This module does not replace or merge with `ApplicationDocument` (credit-application-scoped) or `CustomerDocument` (legacy). Both remain as-is. `Document` is a parallel, additive entity.

**Rationale**: Avoids a disruptive migration of existing data. Both concepts can coexist; a future consolidation change can unify them if needed.

---

### 5. `POST /api/v1/documents/{id}/validate` accepts decision + optional reason

**Decision**: The validate endpoint accepts `{ Decision: "Validated" | "Rejected", RejectionReason?: string }`. Rejection reason is required when Decision is `Rejected`.

**Rationale**: Mirrors the `RejectCreditApplicationCommand` pattern already in use. FluentValidation enforces the conditional requirement.

## Risks / Trade-offs

- **Orphaned documents**: A document registered for a non-existent `OwnerId` will pass validation since we don't enforce FK integrity on the polymorphic owner. Mitigation: validate owner existence in the command handler.
- **Duplicate storage URLs**: Nothing prevents registering the same URL twice. Mitigation: acceptable for now; deduplification is a future concern.
- **No re-upload flow**: A rejected document requires registering a new `Document`. Mitigation: document in API response; clients should call `POST /api/v1/documents` again.

## Migration Plan

1. Add migration `AddDocumentManagement` — creates `documents`, `document_types`, `document_validations` tables
2. Seed `DocumentType` records in migration `Up` (NationalId, Passport, IncomeProof, BankStatement, TaxRegistration, ProofOfAddress)
3. Deploy — no changes to existing tables; no data migration required
4. Rollback: `dotnet ef migrations remove`

## Open Questions

- Should `Document` eventually replace `ApplicationDocument`? (follow-up change)
- Should `GET /api/v1/customers/{id}/documents` be added as a filtered view? (out of scope for this slice)
- Who is authorized to call `POST /api/v1/documents/{id}/validate`? (agent role — JWT claims validation TBD)
