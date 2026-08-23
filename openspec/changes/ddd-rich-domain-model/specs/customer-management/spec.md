## ADDED Requirements

### Requirement: Customer sub-entities are Value Objects
`CustomerAddress`, `CustomerEmail`, `CustomerPhone`, `CustomerFiscalInfo`, and `CustomerWorkInfo` SHALL be modeled as Value Objects — immutable types with no exposed primary key identity. All their properties SHALL use `init` setters or be set only via constructors. They SHALL be configured in EF Core using `OwnsMany` with a shadow property for the database key. The `Customer` aggregate owns their full lifecycle.

#### Scenario: Address cannot be modified after construction
- **WHEN** a `CustomerAddress` instance is created
- **THEN** its properties cannot be reassigned — any change requires replacing the entire Value Object in the collection

#### Scenario: EF Core persists addresses via OwnsMany
- **WHEN** a `Customer` with addresses is saved via `IUnitOfWork.SaveChangesAsync()`
- **THEN** addresses are persisted to the `customer_addresses` table with a shadow FK to `customers.id` — without requiring an exposed `Id` property on the Value Object

### Requirement: Customer aggregate behavior — profile and financials
The `Customer` aggregate SHALL expose `UpdateProfile(...)` and `UpdateFinancials(...)` methods as defined in the `domain-behaviors` capability. Command Handlers SHALL call these methods instead of mutating properties directly.

#### Scenario: UpdateCustomerCommand uses behavior method
- **WHEN** `PUT /api/v1/customers/{id}` is called with valid data
- **THEN** the handler calls `customer.UpdateProfile(...)`, checks the result, and saves — it does NOT mutate `customer.FullName` directly

## MODIFIED Requirements

### Requirement: Customer profile changes persist a CustomerUpdated event to the outbox
The system SHALL persist a `CustomerUpdated` outbox event with a typed, versioned contract via `IOutboxWriter.AppendAsync()` whenever a Customer's profile is successfully updated via `PUT /api/v1/customers/{id}`. The event SHALL be derived from the `CustomerProfileUpdatedEvent` domain event raised by `customer.UpdateProfile(...)`. The event payload SHALL include the customer Id, updated fields, timestamp, and a version number. The `outbox-worker` delivers it to RabbitMQ.

#### Scenario: CustomerUpdated persisted to outbox after profile update
- **WHEN** `PUT /api/v1/customers/{id}` is called with valid data and the update succeeds
- **THEN** a `CustomerUpdated` outbox event is persisted with a typed contract containing CustomerId, the changed field values, UpdatedAt timestamp, and Version

#### Scenario: CustomerUpdated not persisted on failure
- **WHEN** `PUT /api/v1/customers/{id}` fails (customer not found, or validation error)
- **THEN** no `CustomerUpdated` outbox event is persisted
