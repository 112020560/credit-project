## Context

The CRM currently creates customers in a single synchronous step (`CreateCustomerCommand`) with no lifecycle — the customer is immediately `Active`. There is no concept of a prospect, a pending application, or an approval step.

The domain already has tables for `CustomerDocument`, `CustomerFiscalInfo`, and `CustomersRef` that are not used by the current create flow, suggesting the model was partially anticipated.

Two customer creation paths must coexist after this change:
- **Direct path** (unchanged): `POST /customers` → `Customer(Active)` for cash customers
- **Origination path** (new): `Prospect → CreditApplication → Customer(Active)` for credit customers

The project follows Clean Architecture: `Crm.Domain` (entities, interfaces) → `Crm.Application` (Commands/Queries via MediatR, FluentValidation) → `Crm.Infrastructure` (EF Core + PostgreSQL, MassTransit/RabbitMQ) → `Crm.WebApi` (Minimal API, IEndpoint pattern).

## Goals / Non-Goals

**Goals:**
- Introduce `Prospect` as a new aggregate, independent from `Customer`
- Introduce `CreditApplication` with a resumable draft lifecycle and read-only agent review
- Introduce `ApplicationDocument` scoped to a `CreditApplication`
- Automatic, immutable `Prospect → Customer` conversion on approval
- Emit new domain events via existing `IMqProducerService`

**Non-Goals:**
- File storage infrastructure (S3, blob) — `ApplicationDocument` stores a URL, upload mechanics are out of scope
- Risk scoring engine — credit-service consumes events and computes scores independently
- Notification/communication service (email, SMS) for status changes
- Frontend / backoffice UI
- Changes to the existing direct customer creation path

## Decisions

### 1. Prospect is a separate aggregate, not an early-state Customer

**Decision:** `Prospect` is its own aggregate in `Crm.Domain`, not a `Customer` with a different status.

**Rationale:** A `Customer` implies an active, fully-converted entity. Mixing lifecycle states in one aggregate complicates queries, repository methods, and event semantics. Keeping them separate makes the conversion event (`ProspectConvertedToCustomer`) explicit and auditable.

**Alternative considered:** `Customer.Status = "Prospect"` — rejected because it blurs the domain boundary and would require filtering all customer queries by status.

---

### 2. Prospect conversion is destructive — Prospect is archived, not deleted

**Decision:** On approval, `Prospect.Status` is set to `Converted` (immutable). A new `Customer` is created from the Prospect's accumulated data. No Prospect data is modified after conversion.

**Rationale:** The agent approved exactly what the prospect declared. The archived Prospect is the audit record. Deletion would destroy compliance data.

---

### 3. CreditApplication Draft has no TTL — it is resumable indefinitely

**Decision:** A `CreditApplication` in `Draft` state persists until explicitly submitted or cancelled by the prospect.

**Rationale:** Connectivity issues, interrupted sessions, or slow document collection are common in financial contexts. Forcing expiry creates friction and data loss. Cleanup of abandoned drafts is a separate operational concern, deferred.

---

### 4. One Prospect → many CreditApplications

**Decision:** A `Prospect` may have multiple `CreditApplication` records. A rejection does not close the Prospect.

**Rationale:** The Prospect is the identity; the CreditApplication is the attempt. Rejection means "this application was denied," not "this person can never apply." Allows retry with different terms or after improving the financial profile.

---

### 5. Agent is strictly read-only during review

**Decision:** No command exists to edit Prospect or CreditApplication data by an agent. Agents can only call `/approve` or `/reject`.

**Rationale:** Immutability of what was declared ensures integrity of the approval. If data is wrong, the agent rejects with a reason, and the prospect corrects and resubmits via a new CreditApplication.

---

### 6. ApplicationDocument is separate from CustomerDocument

**Decision:** A new `ApplicationDocument` entity is scoped to `CreditApplication`. The existing `CustomerDocument` entity remains scoped to `Customer`.

**Rationale:** Documents uploaded during origination belong to the application process. If the application is rejected, these documents should not carry over to a Customer profile that may not exist. After conversion, post-conversion documents use the existing `CustomerDocument` path.

---

### 7. Conversion transaction: archive Prospect + create Customer atomically

**Decision:** `ConvertProspectToCustomerCommand` wraps both writes in a single `IUnitOfWork.SaveChangesAsync` call within one DB transaction.

**Rationale:** A partial conversion (Prospect archived but Customer not created, or vice versa) would be a corrupt state. The existing `UnitOfWork` pattern already supports this.

---

### 8. New endpoints follow existing IEndpoint minimal API pattern

**Decision:** Each new resource gets its own `IEndpoint` class in `Crm.WebApi/Endpoints/`. Routes follow the existing `api/v{version}` versioned group.

## Risks / Trade-offs

- **Dual-path CustomerCreated events** → Downstream services (credit-service, retail) will receive `CustomerCreated` from both the direct path and the origination path. They must handle both idempotently. Mitigation: `CreateCustomerContract` already carries a `Version` field; origination-sourced customers can be identified by the presence of `ProspectConvertedToCustomer` event arriving before `CustomerCreated`.

- **ApplicationDocument without storage** → The spec requires a `StorageUrl` but does not implement upload. A client must upload to an external storage and then register the URL via the API. This creates a gap if the upload succeeds but the registration fails. Mitigation: deferred to a future change that adds pre-signed URL generation.

- **Prospect data accumulation model** → Data is enriched via separate endpoints (contacts, work info, fiscal info, addresses). This means multiple round-trips and partial Prospect states. No enforced completeness until `/submit`. Mitigation: the submit endpoint validates required fields before transitioning to `Submitted`.

## Open Questions

- Should `CreditApplication` carry the requested amount and product type, or is that managed by the credit-service?
- Is there a maximum number of active `CreditApplication` drafts per Prospect, or unlimited?
- What fields are required at `/submit` vs. optional for enrichment?
