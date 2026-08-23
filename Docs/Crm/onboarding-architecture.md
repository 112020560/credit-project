# Onboarding Architecture

## Overview

The CRM supports two independent onboarding paths for creating a Customer:

| Path | Trigger | Use case |
|------|---------|----------|
| **Direct** | `POST /api/v1/customers` | Cash customers — no credit evaluation needed |
| **Origination** | Prospect → CreditApplication → Approval | Credit customers — full onboarding pipeline |

Both paths produce a `Customer` with `Status = Active` and publish a `CustomerCreated` event with the same `CreateCustomerContract` shape.

---

## Path A — Direct Customer Creation

The simplest path: no Prospect, no application, no risk check.

```
POST /api/v1/customers
        │
        ▼
CreateCustomerCommand
        │
        ▼
Customer (Active) saved to DB
        │
        ▼
[Event] CustomerCreated (CreateCustomerContract)
[Command] CustomerCreated → queue: credit-service-customer-events
```

**Status transitions:** none (Customer is immediately Active)

---

## Path B — Credit Origination

A multi-stage pipeline with risk scoring, document collection, and a configurable multi-step approval workflow.

### Stage overview

```
1. Register Prospect
2. Enrich Prospect (addresses, contacts, work, fiscal)
3. Open Credit Application
4. Attach Documents
5. Submit → Risk Evaluation
      ├─ AutoApprove ──────────────────────────────► Customer Created
      ├─ AutoReject  ──────────────────────────────► Prospect returns to Draft
      └─ ManualReview ──► Approval Workflow Loop ──► Customer Created (or Reject)
```

---

### Stage 1 — Prospect Registration

```
POST /api/v1/prospects
        │
        ▼
CreateProspectCommand
  - Dedup check on IdentificationType + IdentificationNumber
  - Requires at least one contact (Email or Phone)
        │
        ▼
Prospect (Draft) saved
        │
        ▼
[Event] ProspectCreated (anonymous: ProspectId, FullName, Status)
```

**Prospect entity:**
```
Prospect
  Id, FullName, DisplayName, IdentificationType, IdentificationNumber, BirthDate
  Status: Draft | Submitted | Converted
  + Addresses, Phones, Emails, WorkInfos, FiscalInfos (collections)
```

---

### Stage 2 — Prospect Enrichment

```
PUT /api/v1/prospects/{id}/enrich
        │
        ▼
EnrichProspectCommand
  - Guard: Prospect must not be Converted
  - Appends: Addresses, Phones, Emails, WorkInfos, FiscalInfos
        │
        ▼
Prospect (Draft) updated — no status change
```

Data collected here feeds the risk evaluation (MonthlyIncome, HasAddress, HasWorkInfo, HasFiscalInfo).

---

### Stage 3 — Credit Application

```
POST /api/v1/credit-applications
        │
        ▼
CreateCreditApplicationCommand
  - Guard: Prospect must not be Converted
        │
        ▼
CreditApplication (Draft) saved  →  linked to ProspectId
        │
        ▼
[Event] ApplicationCreated (anonymous: ApplicationId, ProspectId, Status)
```

**CreditApplication entity:**
```
CreditApplication
  Id, ProspectId, Status, RejectionReason, WorkflowDefinitionId (nullable)
  Status: Draft | Submitted | InReview | Approved | Rejected
  + Documents (ApplicationDocument collection)
```

---

### Stage 4 — Document Collection

Two complementary mechanisms:

**Application-scoped documents** (required for submission):
```
POST /api/v1/credit-applications/{id}/documents
        │
        ▼
RegisterApplicationDocumentCommand
  → ApplicationDocument attached to CreditApplication
  → Required types: defined in ApplicationDocumentType.Required
```

**General document registry** (polymorphic, any owner):
```
POST /api/v1/documents
        │
        ▼
RegisterDocumentCommand
  - Validates DocumentTypeCode exists
  - OwnerId + OwnerType identify the owner (Customer, Prospect, etc.)
        │
        ▼
Document (Uploaded) saved
        │
        ▼
[Event] DocumentUploaded (DocumentUploadedContract)

POST /api/v1/documents/{id}/validate
        │
        ▼
ValidateDocumentCommand
  - Guard: Document must be in Uploaded status
  - Decision: Approved | Rejected
        │
        ▼
DocumentValidation record saved
Document status → Validated | Rejected
        │
        ▼
[Event] DocumentValidated | DocumentRejected
```

**Document entity:**
```
Document
  Id, OwnerId, OwnerType, DocumentTypeCode, FileName, StorageUrl
  Status: Uploaded | Validated | Rejected
  + Validations (DocumentValidation collection)
```

---

### Stage 5 — Submission and Risk Evaluation

```
POST /api/v1/credit-applications/{id}/submit
        │
        ▼
SubmitCreditApplicationCommand
  ├─ Guard: Status must be Draft
  ├─ Validate required documents are present
  ├─ Load Prospect
  └─ RiskEvaluationService.EvaluateAsync()
          │
          ▼
      RiskEngineService.Evaluate(RiskMatrix, CreditApplicationData)
        - Scores rules: AgeYears, MonthlyIncome, HasAddress,
          HasWorkInfo, HasFiscalInfo, DocumentCount
        - Compares total score to AutoApprove / AutoReject thresholds
        - Returns: AutoApprove | AutoReject | ManualReview
        - Also returns: SuggestedInterestRate, SuggestedMaxAmount
```

**Risk evaluation outcomes:**

```
                    ┌─── AutoApprove (score >= threshold) ───────────────────────────────┐
                    │                                                                      │
Submit ─► RiskEval ─┤                                                                      ├─► see below
                    │                                                                      │
                    ├─── AutoReject  (score <= threshold) ───────────────────────────────┤
                    │                                                                      │
                    └─── ManualReview (between thresholds) ──────────────────────────────┘
```

---

#### Outcome: AutoApprove

```
Prospect   → Status: Converted
Application → Status: Approved
Customer   → Created (MapProspectToCustomer: copies all collections)

[Event] ApplicationStatusChanged (anonymous)
[Event] ProspectConverted (anonymous: ProspectId, CustomerId)
[Event] CustomerCreated (CreateCustomerContract — broadcast)
[Command] CustomerCreated → queue: credit-service-customer-events
[Event] RiskEvaluationCompleted (anonymous: outcome, score, rates)
```

---

#### Outcome: AutoReject

```
Prospect   → Status: Draft (reset — can reapply after enrichment)
Application → Status: Rejected (RejectionReason: "Auto-rejected by risk engine (score: X)")

[Event] ApplicationSubmitted (anonymous)
[Event] ApplicationRejected (anonymous: ApplicationId, ProspectId, Reason)
[Event] RiskEvaluationCompleted (anonymous: outcome, score)
```

---

#### Outcome: ManualReview

```
Prospect   → Status: Submitted
Application → Status: InReview
              WorkflowDefinitionId = active workflow's Id (snapshot — immutable)

[Event] ApplicationSubmitted (anonymous: ApplicationId, ProspectId)
[Event] ApprovalRequested (ApprovalRequestedContract: step 1 details)   ← only if workflow is active
[Event] RiskEvaluationCompleted (anonymous: outcome, score)
```

---

### Stage 6 — Approval Workflow (ManualReview path)

The approval engine is driven by `ApprovalWorkflowService`. It supports both single-agent (no active workflow) and configurable multi-step workflows.

**Workflow setup (admin, done before onboarding):**
```
POST /api/v1/workflows               → CreateWorkflowDefinitionCommand  → Draft
POST /api/v1/workflows/{id}/activate → ActivateWorkflowDefinitionCommand → Active
                                        (previous Active becomes Superseded)
```

**WorkflowDefinition entity:**
```
WorkflowDefinition
  Id, Name, Status: Draft | Active | Superseded
  + Steps: [ { Id, StepName, Order, RequiredRole? } ]
```

**ApprovalDecision entity (audit trail):**
```
ApprovalDecision
  Id, CreditApplicationId, WorkflowDefinitionId?, WorkflowStepId?
  Decision: Approved | Rejected
  RejectionReason?, DecidedBy?, DecidedAt
```

---

#### Multi-step approval loop

```
                      ┌──────────────────────────────────────────────────────┐
                      │              APPROVAL LOOP                           │
                      │                                                      │
  InReview ──► POST /approve ──► ApprovalWorkflowService                    │
                      │          RecordDecisionAsync()                       │
                      │            │                                          │
                      │            ├─ Record ApprovalDecision for step N     │
                      │            │                                          │
                      │            ├─ Steps remaining?                        │
                      │            │     YES ──► stay InReview               │
                      │            │             [Event] ApprovalRequested   ─┘
                      │            │             (next step details)
                      │            │
                      │            └─ No more steps (or no workflow)
                      │                  │
                      │                  ▼
                      │          Prospect → Converted
                      │          Application → Approved
                      │          Customer → Created
                      │          [Event] ApplicationApproved (ApplicationApprovedContract)
                      │          [Event] ProspectConverted (anonymous)
                      │          [Event] CustomerCreated (CreateCustomerContract)
                      │          [Command] CustomerCreated → credit-service-customer-events
                      │
  InReview ──► POST /reject ──► ApprovalWorkflowService
                                RecordDecisionAsync()
                                  │
                                  ├─ Record ApprovalDecision (Rejected)
                                  ├─ Prospect → Draft (can reapply)
                                  └─ Application → Rejected
                                  [Event] ApplicationRejected (ApplicationRejectedContract)
```

**Single-agent fallback:** if no `WorkflowDefinition` is Active when the application enters `InReview`, `WorkflowDefinitionId` is null. The first `POST /approve` immediately completes all steps and converts the Prospect.

---

### Complete State Machine

**Prospect status:**
```
Draft ──► [EnrichProspect] ──► Draft (no change)
Draft ──► [Submit: ManualReview] ──► Submitted
Draft ──► [Submit: AutoApprove] ──► Converted
Submitted ──► [Approve: all steps done] ──► Converted
Submitted ──► [Reject] ──► Draft
Draft ──► [Submit: AutoReject] ──► Draft (stays Draft)
```

**CreditApplication status:**
```
Draft ──► [RegisterDocuments] ──► Draft (no change)
Draft ──► [Submit: AutoApprove] ──► Approved
Draft ──► [Submit: AutoReject] ──► Rejected
Draft ──► [Submit: ManualReview] ──► InReview
InReview ──► [Approve: more steps] ──► InReview
InReview ──► [Approve: last step] ──► Approved
InReview ──► [Reject] ──► Rejected
```

---

## Event Catalog

| Event | Contract | Published by | Trigger |
|-------|----------|-------------|---------|
| `ProspectCreated` | anonymous | `CreateProspectCommand` | Prospect registered |
| `ApplicationCreated` | anonymous | `CreateCreditApplicationCommand` | Application opened |
| `ApplicationSubmitted` | anonymous | `SubmitCreditApplicationCommand` | Submit called |
| `RiskEvaluationCompleted` | anonymous | `SubmitCreditApplicationCommand` | After risk eval |
| `ApprovalRequested` | `ApprovalRequestedContract` | Submit / Approve | ManualReview entry / step advance |
| `ApplicationApproved` | `ApplicationApprovedContract` | `ApprovalWorkflowService` | Final approve |
| `ApplicationRejected` | `ApplicationRejectedContract` | `ApprovalWorkflowService` | Reject decision |
| `CustomerCreated` (event) | `CreateCustomerContract` | Approve / Submit(AutoApprove) / CreateCustomer | Customer created |
| `CustomerCreated` (command) | `CreateCustomerContract` | Approve / Submit(AutoApprove) | Sent to `credit-service-customer-events` |
| `CustomerUpdated` | `CustomerUpdatedContract` | `UpdateCustomerCommand` | Customer profile updated |
| `DocumentUploaded` | `DocumentUploadedContract` | `RegisterDocumentCommand` | Document registered |
| `DocumentValidated` | `DocumentValidatedContract` | `ValidateDocumentCommand` | Document approved |
| `DocumentRejected` | `DocumentRejectedContract` | `ValidateDocumentCommand` | Document rejected |

---

## API Surface

### Prospect management
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/prospects` | `CreateProspectCommand` |
| GET | `/api/v1/prospects/{id}` | `GetProspectByIdQuery` |
| PUT | `/api/v1/prospects/{id}/enrich` | `EnrichProspectCommand` |

### Credit application lifecycle
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/credit-applications` | `CreateCreditApplicationCommand` |
| GET | `/api/v1/credit-applications/{id}` | `GetCreditApplicationByIdQuery` |
| POST | `/api/v1/credit-applications/{id}/documents` | `RegisterApplicationDocumentCommand` |
| POST | `/api/v1/credit-applications/{id}/submit` | `SubmitCreditApplicationCommand` |
| POST | `/api/v1/credit-applications/{id}/approve` | `ApproveCreditApplicationCommand` |
| POST | `/api/v1/credit-applications/{id}/reject` | `RejectCreditApplicationCommand` |

### Customer management
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/customers` | `CreateCustomerCommand` |
| GET | `/api/v1/customers/{id}` | `GetCustomerByIdQuery` |
| GET | `/api/v1/customers` | `SearchCustomersQuery` |
| PUT | `/api/v1/customers/{id}` | `UpdateCustomerCommand` |

### Document management
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/documents` | `RegisterDocumentCommand` |
| GET | `/api/v1/documents/{id}` | `GetDocumentByIdQuery` |
| POST | `/api/v1/documents/{id}/validate` | `ValidateDocumentCommand` |

### Approval workflow configuration
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/workflows` | `CreateWorkflowDefinitionCommand` |
| POST | `/api/v1/workflows/{id}/activate` | `ActivateWorkflowDefinitionCommand` |

### Risk engine configuration
| Method | Route | Command |
|--------|-------|---------|
| POST | `/api/v1/risk-engine/rules` | `CreateRiskRuleCommand` |
| POST | `/api/v1/risk-engine/matrices` | `CreateRiskMatrixCommand` |
| POST | `/api/v1/risk-engine/matrices/{id}/activate` | `ActivateRiskMatrixCommand` |
| POST | `/api/v1/risk-engine/evaluations/trigger` | `TriggerRiskEvaluationCommand` |
| GET | `/api/v1/risk-engine/evaluations/{id}` | `GetRiskEvaluationByIdQuery` |

---

## Key Design Decisions

### WorkflowDefinitionId is snapshotted on InReview entry
When an application enters `InReview`, the currently Active workflow's Id is stored on `CreditApplication.WorkflowDefinitionId`. Subsequent workflow activations do not affect in-flight applications. This guarantees that the approval chain a Prospect is evaluated against does not change mid-process.

### Single-agent fallback
If no `WorkflowDefinition` is Active, `WorkflowDefinitionId` is null. `ApprovalWorkflowService` treats this as "no workflow steps required" — the first approval immediately converts the Prospect. This preserves pre-workflow behavior on deploy.

### MapProspectToCustomer copies all collections
The conversion copies Phones, Emails, Addresses, WorkInfos, and FiscalInfos from Prospect to Customer with new Ids. Prospect data is preserved unchanged after conversion.

### CustomerCreated is both an event and a point-to-point command
On customer creation (origination path), two messages are sent:
- `PublishEvent(CreateCustomerContract)` — broadcast to all consumers
- `SendCommand<CustomerCreated>(contract, "credit-service-customer-events")` — targeted at the credit service

### Document ownership is polymorphic
`Document` uses `OwnerId + OwnerType` (string) instead of typed foreign keys. This allows attaching documents to any entity (Customer, Prospect, CreditApplication) without schema changes.

### Rejection resets Prospect to Draft
On both AutoReject and manual Reject, the Prospect is returned to `Draft` status. This allows the Prospect to enrich their data and reapply without re-registration.
