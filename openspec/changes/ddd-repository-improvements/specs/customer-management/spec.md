## MODIFIED Requirements

### Requirement: Customer can be created via two independent paths
The system SHALL support two distinct paths for Customer creation. The repository method used to load a Customer for update operations SHALL be `GetForUpdateAsync` (not `GetCustomerByIdTrackingAsync`). All other behavior remains unchanged.

#### Scenario: UpdateCustomerCommand uses GetForUpdateAsync
- **WHEN** `PUT /api/v1/customers/{id}` is called
- **THEN** the handler calls `unitOfWork.CustomersRepository.GetForUpdateAsync(id, ct)` to load the Customer for modification

### Requirement: Customer search uses CustomerSearchCriteria
The system SHALL allow searching for customers via `SearchCustomersQuery`. The underlying repository method SHALL accept a `CustomerSearchCriteria` object instead of individual parameters. The API endpoint parameters (query string, page, pageSize) are mapped to a `CustomerSearchCriteria` in the Application layer before calling the repository.

#### Scenario: Search returns paginated results
- **WHEN** `GET /api/v1/customers?query=john&page=1&pageSize=20` is called
- **THEN** the handler creates a `CustomerSearchCriteria("john", 1, 20)` and passes it to `SearchAsync`, returning a paginated list of matching customers
