## ADDED Requirements

### Requirement: CustomerSearchCriteria encapsulates search parameters
The system SHALL provide a `CustomerSearchCriteria` record in `Crm.Domain/Customers/` that encapsulates all parameters needed to search for customers. The `ICustomersRepository.SearchAsync` method SHALL accept a `CustomerSearchCriteria` instead of individual parameters.

#### Scenario: Search uses criteria object
- **WHEN** a handler calls `SearchAsync` on the customers repository
- **THEN** it passes a `CustomerSearchCriteria` instance — not individual `query`, `page`, `pageSize` parameters

#### Scenario: Adding new search criterion does not break repository interface
- **WHEN** a new search filter is needed (e.g., filter by status)
- **THEN** a new property is added to `CustomerSearchCriteria` — the `SearchAsync` method signature does NOT change
