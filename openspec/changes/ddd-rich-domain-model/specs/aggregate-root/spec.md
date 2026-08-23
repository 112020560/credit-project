## ADDED Requirements

### Requirement: AggregateRoot base class with Domain Event support
The system SHALL provide an abstract `AggregateRoot` class in `Crm.Domain.Abstractions` that all aggregate roots inherit from. It SHALL maintain an internal list of `IDomainEvent` instances raised during a business operation, expose them as a read-only collection, and allow clearing them after dispatch.

#### Scenario: Raise and read domain events
- **WHEN** a method on an aggregate calls `RaiseDomainEvent(event)`
- **THEN** the event appears in `AggregateRoot.DomainEvents` and can be iterated by the handler

#### Scenario: Clear domain events after dispatch
- **WHEN** a handler calls `aggregate.ClearDomainEvents()` after publishing to the outbox
- **THEN** `DomainEvents` returns an empty collection

#### Scenario: EF Core does not map domain events
- **WHEN** EF Core materializes an aggregate from the database
- **THEN** the `DomainEvents` collection is NOT persisted or loaded — it starts empty on every materialized instance

### Requirement: IDomainEvent marker interface
The system SHALL provide an `IDomainEvent` marker interface in `Crm.Domain.Abstractions`. All domain events SHALL implement this interface. Domain events SHALL be named in past tense and SHALL include the aggregate id and the timestamp of the operation.

#### Scenario: Domain event naming convention
- **WHEN** a business operation completes on an aggregate
- **THEN** the corresponding domain event name reflects what happened (e.g., `CreditApplicationApprovedEvent`, `ProspectConvertedEvent`, `CustomerCreatedEvent`)
