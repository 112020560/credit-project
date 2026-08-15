# ADR-0001: Strategic Design — Domain Classification

**Status:** Accepted
**Date:** 2026-08-15

---

## Context

The CRM system has grown organically and lacks an explicit map of its strategic domain structure. New developers must infer which parts of the system are most business-critical by reading all the code. Additionally, the entity `ExternalCustomerRef` (formerly `CustomersRef`) integrates data from external systems without a formally defined boundary.

---

## Decision

We classify the system's subdomains and bounded contexts as follows:

### Core Domain — Maximum modeling investment

These subdomains directly generate competitive advantage and must receive the highest DDD modeling effort:

| Subdomain | Key concepts |
|---|---|
| **Credit Origination** | `Prospect` → `CreditApplication` → `Customer` |
| **Risk Engine** | `RiskMatrix`, `RiskRule`, `RiskEvaluation`, `ScoreCard` |

### Supporting Subdomain — Necessary but not differentiating

| Subdomain | Key concepts |
|---|---|
| **Document Management** | `Document`, `DocumentValidation`, `DocumentType` |
| **Approval Workflows** | `WorkflowDefinition`, `WorkflowStep`, `ApprovalDecision` |

### Generic Subdomain — Use library or external service

| Subdomain | Implementation |
|---|---|
| Authentication / Authorization | JWT (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Messaging | RabbitMQ via MassTransit + SmartCore.Outbox |
| Structured Logging | Serilog → Seq |
| Telemetry | OpenTelemetry → OTLP |

---

## Bounded Contexts

```
┌─────────────────────────────────────────────────────┐
│                  CRM Bounded Context                │
│                                                     │
│  ┌──────────────┐   ┌──────────────────────────┐   │
│  │   Prospects  │──▶│  Credit Applications     │   │
│  │  (Onboarding)│   │  (Origination + Risk)    │   │
│  └──────────────┘   └───────────┬──────────────┘   │
│                                 │ approve           │
│                                 ▼                   │
│                      ┌─────────────────┐            │
│                      │   Customers     │            │
│                      │  (Active base)  │            │
│                      └─────────────────┘            │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │  External Customer ACL                       │   │
│  │  ExternalCustomerRef ← POST /external-cust.  │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
          │                          ▲
          ▼                          │
   RabbitMQ / Outbox         External Systems
   (integration events)      (write via API only)
```

---

## External Integrations

| System | Integration point | Direction |
|---|---|---|
| External Customer System | `POST /api/v1/external-customers` | Inbound → `ExternalCustomerRef` |
| SmartCore Outbox Worker | RabbitMQ queues | Outbound (events + commands) |
| Seq | Serilog sink | Outbound (logs) |
| OTLP Collector | OpenTelemetry exporter | Outbound (traces/metrics) |

---

## Consequences

- All new features must be classified as Core, Supporting, or Generic before implementation begins.
- External systems MUST NOT write directly to the database. They use the `POST /api/v1/external-customers` endpoint as the controlled entry point.
- This ADR must be updated when a new subdomain or external integration is added to the system.
