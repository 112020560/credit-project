## Why

Financial institutions require configurable, automated risk evaluation before originating a loan. Without an internal scoring engine, every credit application requires full manual agent review — slowing origination and introducing inconsistency. A risk engine enables automatic approvals for low-risk applicants, automatic rejections for high-risk ones, and risk-based pricing to align loan terms with borrower profile.

## What Changes

- Introduce a `RiskRule` entity to define individual scoring rules (e.g., minimum income, max debt ratio, age range)
- Introduce a `RiskMatrix` entity to group and weight rules for a given product or segment
- Introduce a `ScoreCard` entity to store the evaluated score breakdown per application
- Introduce a `RiskEvaluation` entity to record the full evaluation result (score, outcome, pricing recommendation)
- Add `RiskEvaluationService` to orchestrate rule application and produce a score
- Add `RiskEngine` as the domain service that applies a `RiskMatrix` against a `CreditApplication` and returns a `RiskEvaluation`
- Publish `RiskEvaluationStarted` and `RiskEvaluationCompleted` events
- Expose `POST /api/v1/risk-evaluations` to trigger evaluation on-demand or automatically on submission
- Modify the credit application lifecycle: after a `CreditApplication` transitions to `Submitted`, a risk evaluation is automatically triggered; the outcome determines whether the application moves to `InReview` (manual), `Approved` (auto), or `Rejected` (auto)

## Capabilities

### New Capabilities

- `risk-rules`: Defines how `RiskRule` and `RiskMatrix` entities are created, configured, and managed. Covers rule types, weighting, and matrix versioning.
- `risk-evaluation`: Covers the evaluation lifecycle — triggering an evaluation, applying the matrix, producing a `ScoreCard` and `RiskEvaluation`, and publishing events. Includes the `POST /api/v1/risk-evaluations` endpoint.

### Modified Capabilities

- `credit-applications`: The `Submitted → InReview` transition is no longer a manual system trigger. After submission, a risk evaluation is automatically triggered. The evaluation outcome drives the next status: `InReview` (manual review required), `Approved` (auto-approved), or `Rejected` (auto-rejected). Agents only intervene when the engine returns `ManualReview`.

## Impact

- **Crm.Domain**: New aggregates `RiskRule`, `RiskMatrix`, `ScoreCard`, `RiskEvaluation`; new domain service `RiskEngine`
- **Crm.Application**: New commands/queries for risk evaluation; updated `SubmitCreditApplicationCommand` handler to trigger evaluation
- **Crm.Infrastructure**: New EF mappings and repositories for risk entities; new migration
- **Crm.WebApi**: New endpoint `POST /api/v1/risk-evaluations`
- **Events**: Two new contracts — `RiskEvaluationStarted`, `RiskEvaluationCompleted`
- **Credit Origination flow**: Submission now triggers automated risk evaluation before any agent interaction
