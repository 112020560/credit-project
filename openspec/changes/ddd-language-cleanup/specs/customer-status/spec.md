## ADDED Requirements

### Requirement: CustomerStatus constants class
The system SHALL provide a `CustomerStatus` static class in `Crm.Domain/Customers/CustomerStatus.cs` with string constants for all valid Customer status values. This follows the same pattern as `CreditApplicationStatus`, `ProspectStatus`, and `DocumentStatus`.

#### Scenario: Active constant is used instead of magic string
- **WHEN** any code needs to set or compare Customer status to "Active"
- **THEN** it SHALL use `CustomerStatus.Active` — not the literal string `"Active"`

#### Scenario: CustomerStatus is the single source of truth
- **WHEN** a developer searches the codebase for Customer status values
- **THEN** all values are found in `CustomerStatus.cs` — no string literals for status values exist in any handler or entity
