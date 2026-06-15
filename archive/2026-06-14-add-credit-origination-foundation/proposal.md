## Why

The CRM creates customers directly and has no credit origination lifecycle, making it unsuitable for financial institutions that require prospect management, credit evaluation, document collection, and approval workflows before a customer is active. Two parallel paths to customer creation must coexist: direct creation for cash customers, and a full onboarding flow for credit customers.

## What Changes

- Introduce a `Prospect` aggregate as the entry point for credit onboarding — a lightweight identity that accumulates data progressively before converting to a `Customer`
- Introduce a `CreditApplication` aggregate linked to a `Prospect`, with a resumable `Draft` state and a full status lifecycle through to approval or rejection
- Introduce `ApplicationDocument` as a new entity scoped to `CreditApplication` (separate from the existing `CustomerDocument`)
- On approval, `Prospect` converts to `Customer` automatically and is archived (immutable) — agent cannot modify data at any point in the process
- A rejected application leaves the `Prospect` active; they may open a new `CreditApplication` (one Prospect → many CreditApplications)
- The existing `POST /customers` endpoint remains unchanged for direct cash-customer creation

## Capabilities

### New Capabilities

- `prospect-management`: Create and progressively enrich prospects (identity, contacts, addresses, work info, fiscal info) through the origination lifecycle
- `credit-applications`: Manage credit application lifecycle (Draft → Submitted → InReview → Approved | Rejected) with resumable drafts and read-only agent review
- `application-document-management`: Upload and track documents tied to a credit application (distinct from customer documents)
- `prospect-to-customer-conversion`: Automatic, immutable conversion of an approved prospect into an active customer, with full audit trail

### Modified Capabilities

- `customer-management`: Customer creation now has two entry points — direct (existing) and via prospect conversion (new). No requirement changes to the direct path.

## Impact

- **Domain**: New aggregates `ProspectAggregate`, `CreditApplicationAggregate`; new entity `ApplicationDocument`
- **APIs**: `POST /api/v1/prospects`, `POST /api/v1/credit-applications`, `POST /api/v1/credit-applications/{id}/submit`, `POST /api/v1/credit-applications/{id}/approve`, `POST /api/v1/credit-applications/{id}/reject`, `POST /api/v1/credit-applications/{id}/documents`
- **Events**: `ProspectCreated`, `CreditApplicationCreated`, `CreditApplicationSubmitted`, `CreditApplicationApproved`, `CreditApplicationRejected`, `ProspectConvertedToCustomer`
- **Database**: New tables for prospects, credit_applications, application_documents
- **Downstream**: credit-service and retail service receive new event types via existing MassTransit broadcast
