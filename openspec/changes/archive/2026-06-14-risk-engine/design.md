## Context

The credit origination flow currently requires an agent to manually review every submitted application before approving or rejecting it. There is no automated signal to prioritize reviews, approve clear cases, or reject obviously ineligible applicants. This bottleneck limits throughput and introduces inconsistency.

This design introduces a configurable Risk Engine that evaluates a `CreditApplication` against a set of weighted rules grouped in a `RiskMatrix`, produces a `ScoreCard` and a `RiskEvaluation`, and drives the next application state transition automatically when the score is clear-cut.

The current `Submitted → InReview` transition (which was previously a manual/system no-op step) becomes the responsibility of the risk engine outcome.

## Goals / Non-Goals

**Goals:**
- Evaluate a submitted credit application against a configurable `RiskMatrix`
- Produce a scored `ScoreCard` with per-rule breakdown for audit
- Return one of three outcomes: `AutoApprove`, `ManualReview`, `AutoReject`
- Drive `CreditApplication` status transitions based on the outcome (without agent input for auto cases)
- Publish `RiskEvaluationStarted` and `RiskEvaluationCompleted` events
- Allow on-demand evaluation via `POST /api/v1/risk-evaluations`

**Non-Goals:**
- Machine learning–based or external credit bureau scoring
- Real-time fraud detection
- A UI for managing rules and matrices (data managed via seed/migration or future admin API)
- Multi-product matrix routing (single active matrix per evaluation in this slice)

## Decisions

### 1. Evaluation triggers automatically inside `SubmitCreditApplicationCommand`

**Decision**: The `SubmitCreditApplicationCommandHandler` calls `RiskEvaluationService` after validating and persisting the `Submitted` status, within the same `UnitOfWork` transaction.

**Rationale**: Evaluation is a direct consequence of submission. A single transaction guarantees that the application is never left in `Submitted` without an evaluation result. If the engine throws, the entire submit rolls back.

**Alternative considered**: Trigger evaluation via a domain event or message consumer after submit. Rejected because it introduces eventual consistency complexity and leaves the application in a transient `Submitted` state visible to users.

---

### 2. Score-based outcome model with configurable thresholds

**Decision**: Each `RiskRule` contributes a weighted score when the target field passes the rule condition. The total score is compared against `AutoApproveThreshold` and `AutoRejectThreshold` defined on the `RiskMatrix`. Scores above the approve threshold → `AutoApprove`; below the reject threshold → `AutoReject`; in between → `ManualReview`.

**Rationale**: A threshold-based model is simple to configure, audit, and explain to regulators. It avoids the opacity of voting models.

**Alternative considered**: Rule-voting (pass/fail per rule, majority decides). Rejected because it doesn't support risk-based pricing and is harder to tune.

---

### 3. `RiskEngine` is a domain service; `RiskEvaluationService` is an application service

**Decision**: `RiskEngine` (in `Crm.Domain`) applies a `RiskMatrix` to application data and returns a `RiskEvaluation` value object. It has no I/O dependencies. `RiskEvaluationService` (in `Crm.Application`) loads the active matrix, invokes the engine, persists the `RiskEvaluation`, and publishes events.

**Rationale**: Keeps domain logic testable without EF or MediatR. The application service handles orchestration.

---

### 4. `RiskEvaluation` is a standalone aggregate linked to `CreditApplication` by FK

**Decision**: `RiskEvaluation` is stored in its own table (`risk_evaluations`) with a `CreditApplicationId` FK. `ScoreCard` entries are stored in a child table (`score_card_entries`).

**Rationale**: Avoids bloating `CreditApplication` with scoring columns. Evaluation records serve as an immutable audit trail and can be queried independently.

---

### 5. `RiskMatrix` is versioned; evaluations record the matrix version used

**Decision**: `RiskMatrix` has a `Version` integer. Each `RiskEvaluation` stores `RiskMatrixId` and `RiskMatrixVersion` at the time of evaluation.

**Rationale**: Rules will change over time. Recording the exact version used ensures evaluations remain reproducible and auditable even after rule updates.

---

### 6. Risk-based pricing is a field on `RiskEvaluation`, not a separate entity

**Decision**: `RiskEvaluation` includes `SuggestedInterestRate` and `SuggestedMaxAmount` as nullable decimals computed by the engine from the score and pricing bands defined on the `RiskMatrix`.

**Rationale**: Keeps pricing output co-located with the evaluation result. A separate `Pricing` entity would add complexity with no benefit at this stage.

## Risks / Trade-offs

- **Matrix misconfiguration → incorrect auto-approvals**: A wrongly calibrated threshold could auto-approve ineligible applicants. Mitigation: require matrix activation via an explicit `ActivateMatrix` step with a review gate; keep auto-approve threshold conservative until validated.
- **Synchronous evaluation adds latency to /submit**: Rule evaluation runs inline on submit. Mitigation: rules are evaluated in-memory (no external calls); latency should be sub-millisecond for a typical matrix with < 50 rules.
- **Single active matrix**: This slice does not support per-product matrix routing. All applications use the same active matrix. Mitigation: document limitation; multi-product routing is a follow-up change.
- **No re-evaluation**: Once evaluated, a `RiskEvaluation` is immutable. If rules change, past evaluations are not re-run. Mitigation: new evaluations can be triggered via `POST /api/v1/risk-evaluations` for `InReview` applications.

## Migration Plan

1. Add EF Core migration for `risk_rules`, `risk_matrices`, `risk_evaluations`, `score_card_entries` tables
2. Seed a default `RiskMatrix` (all thresholds set conservatively — everything routes to `ManualReview`) to avoid breaking existing flow
3. Deploy with no behavior change (default matrix always returns `ManualReview`)
4. Configure rules and tune thresholds in a staging environment before activating auto-approve/reject
5. Rollback: revert migration; remove feature flag if introduced; submit flow falls back to static `Submitted → InReview`

## Open Questions

- Should `POST /api/v1/risk-evaluations` be callable by agents to re-evaluate `InReview` applications, or only by the system on submit?
- Should pricing bands (`SuggestedInterestRate`, `SuggestedMaxAmount`) be exposed in the `GET /credit-applications/{id}` response, or returned only in the evaluation endpoint?
- What is the initial seed matrix configuration for the first production deployment?
