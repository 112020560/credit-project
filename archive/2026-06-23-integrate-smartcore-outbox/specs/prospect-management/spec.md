## MODIFIED Requirements

### Requirement: Create prospect with minimum identity data
The system SHALL allow creating a Prospect with only `FirstName`, `LastName`, and `Email`. Upon successful creation, a `ProspectCreated` event SHALL be persisted to the outbox via `IOutboxWriter.AppendAsync()`. The `outbox-worker` delivers it to RabbitMQ asynchronously.

#### Scenario: Prospect created — event persisted to outbox
- **WHEN** `POST /api/v1/prospects` is called with valid minimum data and the domain commit succeeds
- **THEN** a Prospect is persisted with Status `Draft` and a `ProspectCreated` outbox event is persisted to `outbox_db` with `Status = Pending`

#### Scenario: Prospect creation fails — no outbox event
- **WHEN** `POST /api/v1/prospects` fails due to validation or persistence error
- **THEN** no `ProspectCreated` outbox event is written
