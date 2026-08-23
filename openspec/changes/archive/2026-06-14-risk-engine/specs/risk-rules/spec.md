## ADDED Requirements

### Requirement: Create a risk rule
The system SHALL allow creating a `RiskRule` with a name, rule type, target field, condition parameters, and a numeric weight. Supported rule types SHALL be: `RangeCheck` (field value within min/max), `ThresholdCheck` (field value above or below a threshold), and `EnumCheck` (field value in an allowed set).

#### Scenario: Successful rule creation
- **WHEN** a valid `RiskRule` is persisted with a name, type, target field, parameters, and a positive weight
- **THEN** the rule is stored and available for inclusion in a `RiskMatrix`

#### Scenario: Rule with zero or negative weight rejected
- **WHEN** a `RiskRule` is created with a weight of zero or less
- **THEN** the system SHALL reject it with a 400 Bad Request

#### Scenario: Rule with unknown type rejected
- **WHEN** a `RiskRule` is created with a type not in the supported set
- **THEN** the system SHALL reject it with a 400 Bad Request

---

### Requirement: Create a risk matrix
The system SHALL allow creating a `RiskMatrix` that groups one or more `RiskRule` references, assigns `AutoApproveThreshold` and `AutoRejectThreshold` score values, and optionally defines pricing bands (`InterestRateBands`, `MaxAmountBands`). The matrix SHALL have a `Version` integer starting at 1 and a status of `Draft` on creation.

#### Scenario: Successful matrix creation
- **WHEN** a `RiskMatrix` is created with at least one rule, valid thresholds, and the approve threshold greater than the reject threshold
- **THEN** the matrix is stored with Status `Draft` and Version 1

#### Scenario: Matrix with overlapping thresholds rejected
- **WHEN** `AutoApproveThreshold` is less than or equal to `AutoRejectThreshold`
- **THEN** the system SHALL return 400 Bad Request

#### Scenario: Matrix with no rules rejected
- **WHEN** a `RiskMatrix` is created with an empty rules list
- **THEN** the system SHALL return 400 Bad Request

---

### Requirement: Activate a risk matrix
The system SHALL allow activating a `RiskMatrix` in `Draft` status. Only one matrix SHALL be active at any time. Activating a new matrix SHALL automatically deactivate the previously active one. An activated matrix MUST NOT be editable.

#### Scenario: Successful matrix activation
- **WHEN** `ActivateMatrix` is called on a `Draft` matrix
- **THEN** the matrix status transitions to `Active` and any previously `Active` matrix transitions to `Superseded`

#### Scenario: Only one active matrix at a time
- **WHEN** a second matrix is activated while one is already `Active`
- **THEN** the previously active matrix transitions to `Superseded` and the new one becomes `Active`

#### Scenario: Active matrix cannot be edited
- **WHEN** any modification is attempted on a matrix with Status `Active`
- **THEN** the system SHALL return 422 Unprocessable Entity

---

### Requirement: Risk matrix is versioned
The system SHALL record the version of the `RiskMatrix` on every `RiskEvaluation` so that evaluations remain auditable even after the matrix is updated. Creating a new version of an existing matrix increments the version number.

#### Scenario: Evaluation records matrix version
- **WHEN** a `RiskEvaluation` is created using a `RiskMatrix` at version N
- **THEN** the evaluation stores `RiskMatrixId` and `RiskMatrixVersion = N`

#### Scenario: Updating a matrix creates a new version
- **WHEN** a new `RiskMatrix` is created based on a superseded matrix
- **THEN** the version number is incremented relative to the previous version
