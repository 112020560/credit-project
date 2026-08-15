---

description: >
Perform final validation of an implemented OpenSpec change. Delegates
specification verification to OpenSpec, uses the project's Tester agent
for behavioral validation, confirms build and test results, and archives
the change only when all required gates pass.
---------------------------------------------

# Verify

You are executing the project's final verification workflow.

The purpose of this command is to determine whether an implemented
OpenSpec change is complete, correct, and ready to be archived.

OpenSpec remains the authoritative source for specification verification
and change lifecycle management.

This command adds project-specific validation and quality gates around
the OpenSpec verification process.

## Core Principle

Verification is the final quality gate before archival.

The lifecycle is:

```text
/opsx:apply
      │
      ▼
   /review
      │
      ▼
   APPROVE
      │
      ▼
   /verify
      │
      ├── /opsx:verify
      ├── Tester
      ├── Build
      ├── Tests
      └── Git / Change validation
              │
              ▼
          ALL PASS
              │
              ▼
       /opsx:archive
```

Do not archive a change when a required verification gate fails.

## Required Context

Before verification:

1. Read `CLAUDE.md`.
2. Identify the OpenSpec change.
3. Read the complete OpenSpec change.
4. Read `proposal`.
5. Read relevant `specs`.
6. Read `design`.
7. Read `tasks`.
8. Inspect the current Git diff.
9. Inspect relevant implementation.
10. Inspect relevant tests.
11. Review the result from `/review` when available.

Do not verify only the changed files.

Verification must consider the complete change.

## OpenSpec Verification

OpenSpec is responsible for validating implementation against its own
change artifacts.

Use:

```text
/opsx:verify <change-name>
```

when the expanded OPSX workflow is available.

OpenSpec verification should determine whether:

* Implementation matches the specifications.
* Required tasks are complete.
* Requirements have corresponding implementation.
* Scenarios are addressed.
* The change remains coherent.

Do not duplicate OpenSpec's internal artifact verification logic.

Use its result as evidence for this workflow.

If `/opsx:verify` is unavailable because the project uses the core
profile, use the available OpenSpec validation mechanisms and perform
the equivalent project-level verification manually.

The expanded `/opsx:verify` command is part of OpenSpec's expanded
workflow profile.

## Tester Agent

Use the `tester` agent for behavioral validation.

The Tester must independently evaluate whether the implementation behaves
correctly.

The Tester must validate:

* Domain behavior.
* Application behavior.
* Infrastructure behavior.
* Event Sourcing behavior.
* Projections.
* Messaging.
* API behavior.
* Regression scenarios.

The Tester must compare actual behavior against:

1. OpenSpec.
2. Domain behavior.
3. Existing project rules.
4. Approved architectural decisions.

The Tester must not modify production code to make verification pass.

## Verification Gates

The change must pass all applicable gates.

### Gate 1 — OpenSpec

Verify using:

```text
/opsx:verify
```

Expected:

```text
PASS
```

If OpenSpec identifies incomplete or inconsistent implementation:

```text
FAIL
```

Do not archive.

### Gate 2 — OpenSpec Tasks

Verify that required implementation tasks are complete.

The OpenSpec task list remains the source of truth.

Do not manually create another completion checklist.

If required tasks remain incomplete:

```text
FAIL
```

Do not archive.

### Gate 3 — Build

Run:

```bash
dotnet build CreditBackend.sln
```

Expected:

```text
SUCCESS
```

A build failure blocks archival.

Do not hide build failures.

### Gate 4 — Tests

Run:

```bash
dotnet test CreditBackend.sln
```

When appropriate, also run the focused test project:

```bash
dotnet test Src/Core/CreditSystem.Tests/CreditSystem.Tests.csproj
```

Expected:

```text
PASS
```

If relevant tests fail:

```text
FAIL
```

Do not archive.

If tests cannot execute because of an environment problem, report:

```text
BLOCKED
```

Do not interpret an environment failure as a successful verification.

### Gate 5 — Tester

The Tester must return a final assessment.

Expected:

```text
PASS
```

If the Tester identifies a behavioral defect:

```text
FAIL
```

The defect must be addressed before archival.

### Gate 6 — Reviewer

The previous `/review` should have produced:

```text
APPROVE
```

If the review result is:

```text
CHANGES_REQUIRED
```

return to:

```text
/implement
```

Do not archive.

If no review has been performed for a change that requires review,
run the Reviewer before proceeding.

### Gate 7 — Git Diff

Inspect the final diff.

Verify:

* Only expected files changed.
* No debugging code remains.
* No secrets were introduced.
* No accidental configuration changes exist.
* No unrelated refactoring was introduced.
* No temporary workaround remains unexplained.

Unexpected changes block archival until resolved.

## Event Sourcing Verification

For Event Sourced changes, explicitly verify:

* Domain events are correct.
* Aggregate state is correct.
* Event replay remains valid.
* Historical events remain compatible.
* Event ordering is preserved.
* Snapshot behavior remains valid where applicable.
* Concurrency behavior is preserved.
* Event serialization remains compatible.

A change that passes unit tests but breaks historical event replay must
not be archived.

## Persistence Verification

For persistence changes, verify:

* Database operations work.
* Transactions are preserved.
* Event Store behavior is correct.
* Projection persistence is correct.
* Repository behavior is correct.
* Provider-specific behavior is isolated.

The current persistence stack is:

```text
PostgreSQL
    +
Dapper
    +
Npgsql
```

Do not require EF Core or SQL Server unless they are part of the approved
change.

If the change introduces another database provider, verify the provider
independently without changing the expected business behavior.

## Messaging Verification

For messaging changes, verify:

* Correct message contracts.
* Consumer behavior.
* Retry behavior.
* Idempotency where required.
* Outbox behavior.
* Failure handling.
* Relevant RabbitMQ/MassTransit integration.

If an integration environment is unavailable, mark the corresponding
verification as:

```text
BLOCKED
```

rather than assuming success.

## API Verification

For API changes, verify:

* Correct route.
* Correct HTTP method.
* Request validation.
* Expected response.
* Error behavior.
* Authorization behavior where applicable.
* Backwards compatibility where required.

## Projection Verification

For projection changes, verify:

```text
Domain Event
     ↓
Projection
     ↓
Read Model
```

Verify:

* Correct projection behavior.
* Idempotency where required.
* Event replay.
* Read-model correctness.
* Projection rebuild behavior where applicable.

## Security Verification

For changes involving authentication, authorization, external inputs,
webhooks, integrations, or sensitive data, verify relevant security
behavior.

At minimum consider:

* Authorization.
* Input validation.
* Sensitive data exposure.
* Secret handling.
* SQL injection.
* Unsafe dynamic SQL.
* Webhook security.
* External HTTP behavior.
* Logging of sensitive information.

Security failures block archival when they represent a meaningful
vulnerability.

## Performance Verification

Only perform performance validation when the change materially affects:

* Database queries.
* Event replay.
* Projection processing.
* Messaging throughput.
* Background workers.
* Large data sets.
* External integrations.

Do not introduce performance gates for changes where performance is
irrelevant.

Do not require benchmark evidence for every change.

## Verification Result

Produce a gate summary:

```text
OpenSpec verification    PASS / FAIL / BLOCKED
Tasks                     PASS / FAIL / BLOCKED
Build                     PASS / FAIL / BLOCKED
Tests                     PASS / FAIL / BLOCKED
Tester                    PASS / FAIL / BLOCKED
Reviewer                  PASS / FAIL / BLOCKED
Git diff                  PASS / FAIL / BLOCKED
Security                  PASS / FAIL / BLOCKED / N/A
Performance               PASS / FAIL / BLOCKED / N/A
```

## Final Decision

The final decision must be exactly one of:

```text
VERIFIED
```

All required gates passed.

```text
FAILED
```

One or more required gates failed.

```text
BLOCKED
```

Verification cannot be completed because required information,
infrastructure, or decisions are unavailable.

## Archive Rule

Only when the final result is:

```text
VERIFIED
```

may the OpenSpec change be archived.

Use:

```text
/opsx:archive <change-name>
```

OpenSpec's archive operation validates the change and then moves the
completed change into the archive while merging applicable delta specs
into the main specification set.

Do not use `--no-validate` as part of the normal project workflow.

Do not use `--skip-specs` unless the change explicitly does not modify
specifications and that behavior is appropriate for the change.

Do not bypass OpenSpec validation merely to complete the workflow.

## Archive Result

After requesting archival:

Verify that OpenSpec successfully archived the change.

Expected:

```text
ARCHIVED
```

If archival fails:

```text
ARCHIVE_FAILED
```

Do not claim the change is complete if archival failed.

## Recovery

If verification fails because implementation is incorrect:

```text
/implement
```

Use the existing OpenSpec change.

Do not create a new change unless the intent has fundamentally changed.

If verification identifies that the approved design or specification
is incorrect:

```text
/opsx:update <change-name>
```

Update the OpenSpec artifacts first, then continue implementation with:

```text
/opsx:apply <change-name>
```

OpenSpec is designed to support this iterative workflow; implementation
can expose issues that require updating the planning artifacts before
continuing.

## Do Not Archive When

Never archive when:

* OpenSpec verification fails.
* Required tasks remain incomplete.
* Build fails.
* Relevant tests fail.
* Tester reports a defect.
* Reviewer has unresolved blocking findings.
* Event Sourcing compatibility is broken.
* Required integration validation is blocked.
* Security issues remain unresolved.
* Unexpected changes remain in the Git diff.
* Required business or architectural decisions remain unresolved.

## Completion Report

Return:

### Change

OpenSpec change name.

### Verification Gates

Report each gate:

* OpenSpec.
* Tasks.
* Build.
* Tests.
* Tester.
* Reviewer.
* Git diff.
* Security when applicable.
* Performance when applicable.

### Findings

List any failed or blocked checks.

### Actions Taken

Report any corrective implementation or OpenSpec updates.

### Archive

Report whether:

```text
ARCHIVED
```

or:

```text
ARCHIVE_FAILED
```

### Final Status

Return exactly one:

```text
VERIFIED
ARCHIVED
FAILED
BLOCKED
ARCHIVE_FAILED
```

Use `ARCHIVED` only after OpenSpec confirms that the change was
successfully archived.

## Important

This command is the final project-level quality gate around OpenSpec.

It does not replace OpenSpec verification.

It does not create a parallel specification system.

It does not manually archive changes.

It delegates specification verification to OpenSpec and delegates
behavioral validation to the project's Tester.

Only after all required gates pass should it invoke:

```text
/opsx:archive
```

The intended lifecycle is:

```text
                    OpenSpec
                       │
                       ▼
                 /opsx:propose
                       │
                       ▼
                    CHANGE
                       │
                       ▼
                 /opsx:apply
                       │
                       ▼
                    CODE
                       │
                       ▼
                    /review
                       │
                   APPROVE
                       │
                       ▼
                   /verify
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
      opsx:verify    Tester      Build/Tests
          │            │            │
          └────────────┼────────────┘
                       │
                 ALL GATES PASS
                       │
                       ▼
                /opsx:archive
                       │
                       ▼
                    ARCHIVED
```

OpenSpec remains the owner of the change lifecycle.
