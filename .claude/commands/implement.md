---

description: >
Orchestrate implementation of an approved OpenSpec change while preserving
OpenSpec as the authoritative implementation workflow. Delegates actual
task execution to OPSX and applies project-specific agent responsibilities,
engineering rules, validation, and reporting.
---------------------------------------------

# Implement

You are executing the project's implementation workflow.

The purpose of this command is to orchestrate implementation while preserving
OpenSpec/OPSX as the authoritative mechanism for executing change tasks.

OpenSpec owns:

* Change artifacts.
* Task definitions.
* Task ordering and dependencies.
* Task completion tracking.
* Change state.
* Specification consistency.

This project owns:

* Architecture conventions.
* Domain conventions.
* Agent responsibilities.
* Engineering constraints.
* Project-specific implementation rules.
* Validation expectations.

Do not create a parallel implementation workflow that duplicates OpenSpec.

## Core Principle

OpenSpec is the implementation engine.

This command is the project-specific orchestration layer around it.

The relationship is:

```text
                    CLAUDE.md
                        │
                        ▼
                 Project Rules
                        │
                        ▼
                  /implement
                        │
                        ▼
                  /opsx:apply
                        │
             ┌──────────┼──────────┐
             ▼          ▼          ▼
          Backend   Infrastructure  Tests
          Agent        Agent        Agent
             │          │          │
             └──────────┼──────────┘
                        ▼
                  Implementation
```

Do not reproduce OpenSpec's task management inside this command.

## Required Context

Before implementation:

1. Read `CLAUDE.md`.
2. Identify the OpenSpec change being implemented.
3. Inspect the change artifacts.
4. Inspect `tasks.md`.
5. Inspect the relevant specifications.
6. Inspect the design artifact when present.
7. Inspect the existing implementation affected by the change.
8. Inspect relevant existing tests.
9. Inspect the current Git working tree.

If the relevant OpenSpec change cannot be identified, stop and ask for the
change name.

If the change is not ready for implementation according to its OpenSpec
artifacts, do not invent missing artifacts.

Use the appropriate OpenSpec workflow action to continue or update the
change when necessary.

## OpenSpec Is Authoritative

The implementation must use the OpenSpec implementation workflow.

Use:

```text
/opsx:apply
```

or, when the change must be explicit:

```text
/opsx:apply <change-name>
```

OpenSpec is responsible for:

* Reading the change artifacts.
* Reading the task list.
* Working through implementation tasks.
* Updating task completion state.
* Maintaining the OpenSpec change workflow.

Do not manually maintain a second task checklist outside OpenSpec.

Do not copy OpenSpec tasks into another project-specific task system.

Do not replace `/opsx:apply` with a custom implementation loop.

## Project Agent Responsibilities

During `/opsx:apply`, implementation must respect the responsibilities
defined by the project's agents.

### Backend Developer

Use the `backend-developer` role for tasks involving:

* Domain implementation when domain behavior is already defined.
* Application use cases.
* Commands.
* Queries.
* MediatR handlers.
* Application services.
* Application validation.
* DTOs.
* Minimal API endpoints.
* Backend orchestration.

The Backend Developer must follow:

* `CLAUDE.md`
* OpenSpec artifacts
* Approved architectural decisions
* Approved domain decisions

The Backend Developer must not redefine architecture or business behavior.

### Infrastructure

Use the `infrastructure` role for tasks involving:

* PostgreSQL.
* SQL Server.
* Dapper.
* Npgsql.
* Event Store.
* Projection Store.
* Repositories.
* MassTransit.
* RabbitMQ.
* Outbox.
* Background workers.
* Webhooks.
* External integrations.
* Infrastructure configuration.
* Dependency injection.

The Infrastructure Agent must follow:

* `CLAUDE.md`
* OpenSpec artifacts
* Approved architectural decisions
* Approved domain decisions

The Infrastructure Agent must not redefine architecture or business behavior.

### Tester

Use the `tester` role for tasks involving:

* Test implementation.
* Regression tests.
* Domain tests.
* Application tests.
* Infrastructure tests.
* Integration tests.
* API tests.

The Tester must validate behavior against OpenSpec and must not modify
production code merely to make tests pass.

## Agent Selection

Do not invoke every agent for every change.

Determine responsibility from the OpenSpec tasks.

For example:

```text
Domain behavior
    → backend-developer
    → tester

Application use case
    → backend-developer
    → tester

Database / Event Store
    → infrastructure
    → tester

RabbitMQ consumer
    → infrastructure
    → tester

API endpoint
    → backend-developer
    → tester

Pure test change
    → tester
```

Architect and Domain Expert are primarily planning and decision roles.

Do not re-run architectural or domain analysis during implementation
unless implementation exposes a conflict or missing decision.

## Implementation Boundaries

Implementation must follow the decisions already established by:

1. `CLAUDE.md`
2. OpenSpec change artifacts
3. Architectural analysis
4. Domain analysis

If these sources conflict:

```text
STOP
```

Do not silently choose one.

Report the conflict and identify the decision required.

## When Implementation Reveals a New Problem

Implementation may reveal that the approved design is incomplete.

Examples:

* A required domain invariant was not defined.
* A database change is larger than expected.
* A new aggregate boundary is required.
* An existing domain event is insufficient.
* Historical Event Sourcing compatibility is affected.
* A public API contract must change.
* A messaging contract must change.
* Supporting SQL Server requires an architectural decision.
* The proposed Dapper implementation cannot satisfy the requirement
  without changing the persistence boundary.

When this happens:

1. Stop the affected task.
2. Do not invent a solution.
3. Do not silently modify OpenSpec.
4. Report the discovered problem.
5. Identify which artifact or decision must be revisited.
6. Use `/opsx:update` when the planning artifacts themselves need to change.
7. Resume `/opsx:apply` only after the change is coherent again.

OpenSpec explicitly supports revising planning artifacts when implementation
reveals that the plan needs to change.

## Domain and Event Sourcing

When implementing Event Sourced aggregates:

* Preserve aggregate boundaries.
* Preserve domain invariants.
* Implement only defined domain behavior.
* Produce the defined domain events.
* Update state through the existing event application mechanism.
* Preserve event replay.
* Preserve historical event compatibility.
* Consider snapshot compatibility.
* Preserve concurrency semantics.

Do not bypass the Event Store.

If the implementation requires changing the meaning of historical events,
stop and report the issue.

## Persistence

Follow the current infrastructure strategy.

Current implementation:

```text
PostgreSQL
    +
Dapper
    +
Npgsql
```

The architecture allows future database-provider evolution and a possible
future evaluation of EF Core.

However:

* Do not introduce EF Core unless explicitly approved.
* Do not introduce SQL Server merely because future support is possible.
* Do not introduce speculative persistence abstractions.
* Do not leak database-provider types into Domain or Application.
* Keep provider-specific behavior inside Infrastructure.

If a change genuinely introduces SQL Server support, implement it according
to the approved infrastructure design.

## Messaging

When implementing messaging:

* Reuse existing contracts.
* Follow MassTransit conventions.
* Preserve RabbitMQ topology.
* Preserve retry behavior.
* Preserve idempotency requirements.
* Respect Outbox semantics.
* Keep integration events separate from domain events.

Do not introduce duplicate message contracts.

## Projections

When implementing projections:

* Follow `IProjection`.
* Follow `IProjectionEngine`.
* Use the appropriate Projection Store.
* Preserve deterministic behavior.
* Consider idempotency.
* Consider event replay.
* Consider projection rebuild behavior.

## API

When implementing API changes:

* Follow the existing Minimal API endpoint-group pattern.
* Keep endpoints thin.
* Delegate behavior to Application.
* Follow `/api/{resource}`.
* Follow existing response and error conventions.
* Do not introduce URL versioning unless explicitly required.

## Testing

Tests are part of implementation.

For every relevant OpenSpec task, determine whether tests are required.

Examples:

```text
Domain change
    → Domain tests

Application change
    → Application tests

Infrastructure change
    → Infrastructure / integration tests

API change
    → API tests

Bug fix
    → Regression test
```

Do not weaken assertions.

Do not delete failing tests to obtain a passing result.

Do not modify production code merely to satisfy an incorrect test.

## Validation

After the OpenSpec implementation tasks have been completed:

Run the appropriate build:

```bash
dotnet build CreditBackend.sln
```

Run the relevant tests:

```bash
dotnet test CreditBackend.sln
```

When a focused test project is appropriate:

```bash
dotnet test Src/Core/CreditSystem.Tests/CreditSystem.Tests.csproj
```

Use the actual project structure from the repository if it differs from
these commands.

Do not claim successful validation when the commands were not executed.

## Git Diff Review

Before reporting completion:

Inspect the final Git diff.

Verify:

* Only expected files changed.
* No unrelated refactoring was introduced.
* No debugging code remains.
* No secrets were added.
* No accidental configuration changes exist.
* No generated files were unintentionally added.
* No temporary workaround remains unexplained.

## OpenSpec Task State

Do not manually invent or duplicate task state.

The OpenSpec task list is the source of truth for implementation progress.

After implementation:

* Ensure completed tasks are reflected by the OpenSpec workflow.
* Ensure incomplete tasks remain visible.
* Do not mark a task complete when its acceptance criteria have not been
  satisfied.

OpenSpec's `/opsx:apply` workflow is responsible for working through and
checking off tasks.

## Completion Criteria

Implementation is complete only when:

* OpenSpec implementation tasks are completed.
* Required production code is implemented.
* Required tests are implemented.
* The solution builds successfully.
* Relevant tests pass.
* No unresolved implementation blockers remain.
* The Git diff contains only expected changes.
* OpenSpec accurately reflects implementation progress.

## Blockers

Stop when:

* A business rule is undefined.
* The domain model requires an unapproved change.
* Architecture requires a new decision.
* OpenSpec conflicts with the required behavior.
* A database schema decision is missing.
* An external integration contract is missing.
* A public API or messaging contract requires an unapproved breaking change.
* Event Sourcing compatibility cannot be preserved.
* Required infrastructure is unavailable.
* Implementation requires speculative architecture.

When blocked:

```text
Do not guess.
Do not silently redesign.
Do not create a parallel workaround.
Do not mark the OpenSpec task complete.
```

Report the blocker clearly.

## OpenSpec Recovery

If implementation exposes a problem with the current plan:

```text
/opsx:update <change-name>
```

may be used to revise planning artifacts.

After the artifacts are coherent again:

```text
/opsx:apply <change-name>
```

continues implementation.

Do not edit OpenSpec planning artifacts casually during implementation.

Use the OpenSpec workflow to maintain their consistency.

## Implementation Report

At the end of the workflow report:

### 1. Change

Identify the OpenSpec change implemented.

### 2. OpenSpec Progress

Report:

* Tasks completed.
* Tasks remaining.
* Any artifacts updated during implementation.

### 3. Implementation

Summarize:

* Backend changes.
* Infrastructure changes.
* API changes.
* Messaging changes.
* Projection changes.

Only report categories that actually changed.

### 4. Tests

Report:

* Tests added.
* Tests modified.
* Tests executed.
* Results.

### 5. Build

Report the build result.

### 6. Git Diff

Report whether unrelated changes were detected.

### 7. Remaining Issues

Identify remaining concerns or limitations.

### 8. Status

Return exactly one:

```text
IMPLEMENTED
```

The implementation is complete and validation succeeded.

```text
BLOCKED
```

Implementation cannot continue without a required decision or capability.

```text
FAILED
```

Implementation was attempted but could not be completed because of an
implementation or validation failure.

## Important

This command is an orchestration layer.

It does not replace OpenSpec.

OpenSpec remains responsible for the change workflow and task execution.

The project-specific agents provide specialized implementation behavior.

The expected lifecycle is:

```text
Requirement
     │
     ▼
OpenSpec planning
     │
     ▼
Approved Change
     │
     ▼
/implement
     │
     ▼
/opsx:apply
     │
     ├── backend-developer
     ├── infrastructure
     └── tester
     │
     ▼
Build + Tests
     │
     ▼
Review / Verify
     │
     ▼
/opsx:archive
```

Never create a second SDD system beside OpenSpec.

Use OpenSpec's capabilities wherever they already solve the problem.
