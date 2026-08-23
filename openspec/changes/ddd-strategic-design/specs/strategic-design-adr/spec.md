## ADDED Requirements

### Requirement: Strategic Design ADR documents domain classification
The project SHALL have an Architecture Decision Record at `docs/adr/0001-strategic-design-domain-classification.md` that explicitly classifies each subdomain (Core, Supporting, Generic), identifies bounded contexts, and maps known external integrations.

#### Scenario: Developer can identify Core Domain
- **WHEN** a new developer reads the ADR
- **THEN** they can identify which parts of the codebase are Core Domain (highest modeling investment) without reading all the code

#### Scenario: ADR is kept up to date
- **WHEN** a new subdomain or external integration is added to the system
- **THEN** the ADR is updated as part of the same change
