## ADDED Requirements

### Requirement: Risk evaluation is triggered automatically on credit application submission
The system SHALL automatically trigger a `RiskEvaluation` when a `CreditApplication` transitions to `Submitted`. The evaluation SHALL be executed within the same transaction as the submission. The evaluation outcome SHALL determine the next application status transition.

#### Scenario: Auto-approve outcome
- **WHEN** a `CreditApplication` is submitted and the risk score meets or exceeds `AutoApproveThreshold`
- **THEN** a `RiskEvaluation` is persisted with Outcome `AutoApprove`, the application transitions to `Approved`, `RiskEvaluationCompleted` and `CreditApplicationApproved` events are published, and prospect-to-customer conversion is triggered

#### Scenario: Manual review outcome
- **WHEN** a `CreditApplication` is submitted and the risk score falls between `AutoRejectThreshold` and `AutoApproveThreshold`
- **THEN** a `RiskEvaluation` is persisted with Outcome `ManualReview`, the application transitions to `InReview`, and `RiskEvaluationCompleted` is published

#### Scenario: Auto-reject outcome
- **WHEN** a `CreditApplication` is submitted and the risk score is at or below `AutoRejectThreshold`
- **THEN** a `RiskEvaluation` is persisted with Outcome `AutoReject`, the application transitions to `Rejected`, the Prospect returns to `Draft`, and `RiskEvaluationCompleted` and `CreditApplicationRejected` events are published

#### Scenario: Evaluation failure rolls back submission
- **WHEN** the risk engine throws an unhandled error during evaluation
- **THEN** the submission transaction is rolled back, the application remains `Draft`, and no events are published

---

### Requirement: Risk evaluation can be triggered on-demand
The system SHALL expose `POST /api/v1/risk-evaluations` to allow re-evaluation of a `CreditApplication` in `InReview` status. The re-evaluation SHALL produce a new `RiskEvaluation` record but SHALL NOT automatically transition the application status; the outcome is advisory for the reviewing agent.

#### Scenario: Successful on-demand evaluation
- **WHEN** `POST /api/v1/risk-evaluations` is called with a valid `CreditApplicationId` referencing an `InReview` application
- **THEN** a new `RiskEvaluation` is persisted with the latest active `RiskMatrix`, `RiskEvaluationStarted` and `RiskEvaluationCompleted` events are published, and the evaluation result is returned

#### Scenario: On-demand evaluation blocked for non-InReview applications
- **WHEN** `POST /api/v1/risk-evaluations` is called for a `CreditApplication` not in `InReview` status
- **THEN** the system SHALL return 422 Unprocessable Entity

#### Scenario: On-demand evaluation blocked when no active matrix exists
- **WHEN** `POST /api/v1/risk-evaluations` is called and no `RiskMatrix` has Status `Active`
- **THEN** the system SHALL return 422 Unprocessable Entity with a descriptive error

---

### Requirement: Risk evaluation produces a ScoreCard with per-rule breakdown
Each `RiskEvaluation` SHALL include a `ScoreCard` listing every evaluated rule, the field value observed, whether the rule passed, and the weighted score contribution. The total score and outcome SHALL be stored on the `RiskEvaluation`.

#### Scenario: ScoreCard captures all rules
- **WHEN** a `RiskEvaluation` is completed
- **THEN** the `ScoreCard` contains one entry per rule in the applied `RiskMatrix`, each with: rule name, target field, observed value, pass/fail result, and weighted contribution

#### Scenario: Total score is sum of passed rule weights
- **WHEN** a set of rules are evaluated and K of N rules pass
- **THEN** the total score equals the sum of the weights of the K passing rules

---

### Requirement: Risk evaluation includes pricing recommendation
The `RiskEvaluation` SHALL include a `SuggestedInterestRate` and `SuggestedMaxAmount` derived from the score against pricing bands defined in the active `RiskMatrix`. These fields SHALL be nullable when no pricing bands are configured.

#### Scenario: Pricing recommendation derived from score
- **WHEN** a `RiskEvaluation` is completed and the `RiskMatrix` has pricing bands configured
- **THEN** `SuggestedInterestRate` and `SuggestedMaxAmount` are set based on the score band the applicant falls into

#### Scenario: No pricing when bands not configured
- **WHEN** a `RiskEvaluation` is completed and the `RiskMatrix` has no pricing bands
- **THEN** `SuggestedInterestRate` and `SuggestedMaxAmount` are null

---

### Requirement: Risk evaluation events are published
The system SHALL publish `RiskEvaluationStarted` when evaluation begins and `RiskEvaluationCompleted` when evaluation concludes, regardless of outcome. Both events SHALL include the `CreditApplicationId`, `RiskEvaluationId`, and `RiskMatrixId`.

#### Scenario: Both events published on successful evaluation
- **WHEN** a `RiskEvaluation` completes successfully
- **THEN** `RiskEvaluationStarted` and `RiskEvaluationCompleted` events are published with correct identifiers

#### Scenario: Only started event published on evaluation failure
- **WHEN** a `RiskEvaluation` throws before completing
- **THEN** `RiskEvaluationStarted` is NOT published (evaluation is rolled back) and no partial events are emitted
