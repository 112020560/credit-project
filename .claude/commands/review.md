---

description: >
Review an implemented OpenSpec change against the project's architecture,
domain model, engineering rules, specifications, and implementation quality.
Acts as a quality gate before final verification. Does not archive the
OpenSpec change.
----------------

# Review

You are executing the project's code review workflow.

The purpose of this command is to perform an independent quality review
of an implemented OpenSpec change before final verification.

The review must determine whether the implementation is suitable to proceed
to `/verify`.

This command does not archive the OpenSpec change.

## Core Principle

OpenSpec remains the authoritative system for the change lifecycle.

This command provides a project-specific quality gate around the completed
implementation.

The lifecycle is:

```text
/opsx:apply
      │
      ▼
   /review
      │
      ▼
  reviewer
      │
      ├── CHANGES_REQUIRED
      │        │
      │        ▼
      │    /implement
      │
      └── APPROVE
               │
               ▼
            /verify
```

Do not create a parallel review or change-management system.

## Required Context

Before reviewing:

1. Read `CLAUDE.md`.
2. Identify the relevant OpenSpec change.
3. Read the complete OpenSpec change.
4. Inspect `proposal`.
5. Inspect relevant `specs`.
6. Inspect `design`.
7. Inspect `tasks`.
8. Inspect the current Git diff.
9. Inspect the implementation affected by the change.
10. Inspect relevant tests.
11. Inspect the implementation and testing results when available.

Do not review only individual files without understanding the complete change.

## OpenSpec State

Before reviewing, verify that the change has gone through the implementation
workflow.

The implementation should have been performed through:

```text
/opsx:apply
```

Do not manually reconstruct OpenSpec task state.

The OpenSpec task list is the source of truth for implementation progress.

If required implementation tasks remain incomplete, report the condition.

Do not archive the change.

## Reviewer Agent

Use the `reviewer` agent as the primary quality-review authority.

The Reviewer must evaluate:

* OpenSpec compliance.
* Architecture.
* Domain integrity.
* Application design.
* Infrastructure design.
* Event Sourcing.
* Persistence.
* Messaging.
* API.
* Testing.
* Security.
* Performance.
* Maintainability.
* Change scope.

The Reviewer must not implement fixes.

The Reviewer must not redefine requirements.

The Reviewer must not modify OpenSpec to make the implementation appear
correct.

## Review Scope

The review must compare:

```text
OpenSpec
   │
   ├── proposal
   ├── specs
   ├── design
   └── tasks
        │
        ▼
Implementation
        │
        ▼
Git Diff
        │
        ▼
Tests
```

The goal is to determine whether the implementation faithfully realizes
the approved change.

## Architecture Review

Verify:

* Clean Architecture boundaries are preserved.
* Domain does not depend on Infrastructure.
* Domain does not depend on API.
* Application does not contain infrastructure implementation details.
* Infrastructure implements technical concerns.
* API endpoints remain thin.
* Existing dependency direction is preserved.
* No unnecessary architectural layer was introduced.
* No speculative architecture was introduced.

Architectural violations must be reported even when the implementation
currently works.

## Domain Review

When domain behavior is affected, verify:

* Aggregate boundaries.
* Invariants.
* State transitions.
* Domain events.
* Business rules.
* Value Objects.
* Domain Services.
* Existing domain behavior.

Verify that business logic remains in the Domain layer.

Do not accept business rules implemented only in:

* API endpoints.
* Command handlers.
* Repositories.
* Infrastructure.
* Background workers.

unless the approved architecture explicitly requires it.

Do not invent missing business requirements during review.

## Event Sourcing Review

For Event Sourcing changes, verify:

* Correct domain events.
* Correct aggregate state changes.
* Correct event application.
* Event replay compatibility.
* Historical event compatibility.
* Event ordering.
* Aggregate version/concurrency behavior.
* Snapshot compatibility.
* Serialization compatibility.
* Event Store usage.

Pay particular attention to changes that appear correct for new
aggregates but could break existing event streams.

If historical event compatibility cannot be established, report the issue.

## Persistence Review

The current persistence implementation is:

```text
PostgreSQL
    +
Dapper
    +
Npgsql
```

Review whether:

* Persistence remains inside Infrastructure.
* Dapper-specific concerns remain inside Infrastructure.
* Npgsql-specific concerns remain inside Infrastructure.
* Database provider-specific behavior is isolated.
* SQL is appropriately parameterized.
* Transactions are preserved.
* Event Store semantics are preserved.
* Projection persistence is correct.

The architecture allows future SQL Server support and possible future
evaluation of EF Core.

Do not reject the current Dapper implementation because EF Core may be
considered in the future.

Do reject unnecessary coupling of Domain or Application to:

* PostgreSQL.
* SQL Server.
* Dapper.
* Npgsql.
* EF Core.

Do not require speculative abstractions solely for a hypothetical future
migration.

## Messaging Review

When messaging is affected, verify:

* Existing contracts are reused where appropriate.
* Integration messages remain separate from domain events.
* MassTransit conventions are preserved.
* RabbitMQ topology is not unnecessarily changed.
* Retry behavior is preserved.
* Idempotency requirements are respected.
* Outbox semantics are preserved.
* Consumers handle failures appropriately.

## API Review

For API changes, verify:

* Correct HTTP method.
* Correct route.
* Existing `/api/{resource}` conventions.
* Thin endpoint implementation.
* Application handler usage.
* Validation behavior.
* Response contract.
* Error behavior.
* Backwards compatibility where required.

Do not introduce URL versioning unless explicitly required.

## Projection Review

For projection changes, verify:

* Correct events are handled.
* Read models are updated correctly.
* Projection behavior is deterministic.
* Idempotency is preserved where required.
* Event replay remains valid.
* Projection rebuild behavior is considered.

Do not place business rules inside projections.

## Background Worker Review

For worker changes, verify:

* Business logic remains in Domain/Application.
* Worker responsibility is orchestration and scheduling.
* Cancellation is respected.
* Failure handling is appropriate.
* Retry behavior is preserved.
* Duplicate execution is considered.
* Scoped dependencies are handled correctly.

## Test Review

The review must evaluate whether the tests actually validate the change.

Verify:

* Domain behavior has appropriate tests.
* Application behavior has appropriate tests.
* Infrastructure behavior has appropriate tests.
* API behavior has appropriate tests where required.
* Integration behavior has appropriate tests where required.
* Regression tests exist for relevant bug fixes.
* Event Sourcing behavior tests events and state where appropriate.
* Tests do not merely reproduce implementation details.
* Assertions are meaningful.
* Tests are deterministic.
* Existing tests remain relevant.

A large number of tests does not automatically indicate adequate coverage.

## Security Review

Consider relevant security concerns:

* Authentication.
* Authorization.
* Input validation.
* Sensitive data exposure.
* SQL injection.
* Dynamic SQL.
* External HTTP calls.
* Webhook security.
* Message trust boundaries.
* Secret handling.
* Logging of sensitive data.
* Configuration exposure.

Only report findings that are relevant to the change or that represent
a meaningful vulnerability introduced or exposed by it.

## Performance Review

Consider meaningful performance implications involving:

* Database access.
* Event Store.
* Projections.
* Messaging.
* Background workers.
* HTTP calls.
* Large collections.
* Repeated queries.
* N+1 access.
* Serialization.
* Event replay.

Do not reject correct code based on speculative micro-optimizations.

## Change Scope

Inspect the Git diff for:

* Unrelated refactoring.
* Unrelated formatting.
* Accidental configuration changes.
* Removed functionality.
* Unexpected public contract changes.
* Debugging code.
* Temporary workarounds.
* Generated files that should not be committed.
* Secrets.

The implementation should remain focused on the OpenSpec change.

## OpenSpec Compliance

Verify each relevant acceptance criterion and requirement.

Classify each as:

```text
PASS
FAIL
NOT_APPLICABLE
```

Do not change the requirement to match the implementation.

If implementation and OpenSpec disagree:

* Report the discrepancy.
* Do not modify OpenSpec.
* Do not approve the implementation.

If the specification itself is incomplete or contradictory, report:

`BLOCKED`

rather than guessing.

## Findings

Use the severity levels defined by `reviewer.md`:

```text
BLOCKER
CRITICAL
MAJOR
MINOR
NIT
```

Every finding must contain:

### Severity

The severity level.

### Location

File and relevant component or logical section.

### Problem

What is wrong.

### Why It Matters

The consequence.

### Recommendation

What should be changed.

Do not implement the recommendation.

## Review Decision

The Reviewer must return exactly one:

```text
APPROVE
```

The implementation is suitable to proceed to `/verify`.

```text
CHANGES_REQUIRED
```

The implementation requires corrections before verification.

```text
BLOCKED
```

The review cannot determine correctness because required information or
decisions are missing.

## Decision Rules

Return `CHANGES_REQUIRED` when:

* BLOCKER findings exist.
* CRITICAL findings exist.
* Unresolved MAJOR findings exist.
* Required OpenSpec behavior is not implemented.
* Architectural boundaries are violated.
* Important Event Sourcing compatibility is broken.
* Critical tests are missing or incorrect.

Return `APPROVE` when:

* OpenSpec requirements are satisfied.
* No blocking findings remain.
* Architecture is respected.
* Domain behavior is consistent.
* Implementation is maintainable.
* Appropriate tests exist.
* Relevant concerns have been evaluated.

Minor findings and NITs do not automatically prevent approval.

Return `BLOCKED` when:

* Required business decisions are missing.
* Specifications conflict.
* Architectural decisions are missing.
* Required implementation evidence is unavailable.
* Validation cannot be meaningfully performed.

## Review Result

When the review finishes, produce:

### Specification

Summary of OpenSpec compliance.

### Architecture

Summary of architectural findings.

### Domain

Summary of domain findings.

### Implementation

Summary of application and infrastructure findings.

### Tests

Summary of test quality and coverage.

### Security

Relevant security findings.

### Performance

Relevant performance findings.

### Findings

List findings ordered by severity.

### Positive Observations

Mention important decisions that are correct or well implemented.

### Decision

Return:

`APPROVE`

or:

`CHANGES_REQUIRED`

or:

`BLOCKED`

## Next Step

If the decision is:

```text
APPROVE
```

do not archive the OpenSpec change.

The next workflow stage is:

```text
/verify
```

If the decision is:

```text
CHANGES_REQUIRED
```

the implementation must return to:

```text
/implement
```

The relevant OpenSpec tasks must be corrected through the OpenSpec workflow.

If the decision is:

```text
BLOCKED
```

stop and report the missing decision or information.

## Constraints

* Do not modify production code.
* Do not modify tests.
* Do not modify OpenSpec artifacts to hide defects.
* Do not archive the change.
* Do not redefine requirements.
* Do not invent business rules.
* Do not introduce speculative requirements.
* Do not reject code solely because of personal style preference.
* Do not approve a change that contradicts OpenSpec.

## Important

This command is a quality gate.

It does not implement fixes.

It does not verify the entire system.

It does not archive the OpenSpec change.

The expected lifecycle is:

```text
/opsx:apply
      │
      ▼
   /review
      │
      ▼
  reviewer
      │
      ├── CHANGES_REQUIRED
      │        │
      │        ▼
      │    /implement
      │
      └── APPROVE
               │
               ▼
            /verify
               │
               ▼
        /opsx:archive
```

OpenSpec remains the owner of the change lifecycle.
