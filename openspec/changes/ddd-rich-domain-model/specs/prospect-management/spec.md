## ADDED Requirements

### Requirement: Prospect sub-entities are Value Objects
`ProspectAddress`, `ProspectEmail`, `ProspectPhone`, `ProspectWorkInfo`, and `ProspectFiscalInfo` SHALL be modeled as Value Objects — immutable types with no exposed primary key identity. All properties SHALL use `init` setters. They SHALL be configured in EF Core using `OwnsMany` with a shadow property for the database key. The `Prospect` aggregate owns their full lifecycle.

#### Scenario: Prospect work info cannot be modified after construction
- **WHEN** a `ProspectWorkInfo` instance is created
- **THEN** its properties cannot be reassigned — any change requires replacing the entire Value Object

#### Scenario: EF Core persists prospect data via OwnsMany
- **WHEN** a `Prospect` with enrichment data is saved
- **THEN** the related data is persisted to their respective tables using shadow FK properties — without requiring exposed `Id` properties on the Value Objects

### Requirement: Prospect aggregate behavior — lifecycle methods
The `Prospect` aggregate SHALL expose `Submit()`, `Convert()`, and `Reject()` behavior methods as defined in the `domain-behaviors` capability. Command Handlers SHALL call these methods to perform status transitions.

#### Scenario: SubmitCreditApplicationCommand uses Prospect.Submit()
- **WHEN** a credit application is submitted
- **THEN** the handler calls `prospect.Submit()` instead of setting `prospect.Status = ProspectStatus.Submitted` directly

#### Scenario: Prospect.Convert() called during approval
- **WHEN** a credit application is approved (either auto or manual)
- **THEN** the handler calls `prospect.Convert()` before creating the Customer

## MODIFIED Requirements

### Requirement: Prospect status lifecycle
The system SHALL enforce the following status transitions. Transitions SHALL be enforced inside the `Prospect` aggregate's behavior methods, not in the Application layer.
- `Draft` → `Submitted` (via `Prospect.Submit()` when a CreditApplication is submitted)
- `Draft` → `Converted` (not allowed directly)
- `Submitted` → `Converted` (via `Prospect.Convert()` when a linked CreditApplication is approved)
- `Submitted` → `Draft` (via `Prospect.Reject()` when the linked CreditApplication is rejected)
- `Converted` is a terminal status — no further transitions

#### Scenario: Prospect returns to Draft after rejection
- **WHEN** a linked CreditApplication transitions to `Rejected`
- **THEN** the handler calls `prospect.Reject()`, which sets the status back to `Draft` and raises `ProspectRejectedEvent`

#### Scenario: Invalid transition returns failure
- **WHEN** `Prospect.Convert()` is called on a `Prospect` not in `Submitted` status
- **THEN** `Result.Failure` is returned with `ProspectError.InvalidTransition` and no state change occurs
