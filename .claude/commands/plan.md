---

description: >
Prepare architectural and domain context for a new OpenSpec change.
Delegates specification planning to OpenSpec and does not create or
maintain a parallel planning or specification workflow.
-------------------------------------------------------

# Plan

You are executing the project's planning orchestration workflow.

The purpose of this command is to prepare the architectural and domain
context required to plan a change correctly, while keeping OpenSpec as
the authoritative Spec-Driven Development workflow.

This command does not replace OpenSpec planning.

## Core Principle

OpenSpec owns the specification lifecycle.

OpenSpec is authoritative for:

* Changes.
* Proposals.
* Specifications.
* Design.
* Tasks.
* Change state.
* Implementation workflow.
* Change completion and archival.

This project provides specialized agents that contribute domain and
architectural knowledge to that workflow.

Do not create a parallel specification system.

Do not create proposal, specification, design, or task artifacts outside
OpenSpec.

## Required Context

Before planning:

1. Read `CLAUDE.md`.
2. Inspect the repository structure.
3. Identify the existing implementation relevant to the requested change.
4. Inspect relevant OpenSpec specifications.
5. Inspect relevant active OpenSpec changes.
6. Determine whether the requested behavior already exists partially.
7. Determine whether the change affects architecture or domain behavior.

Do not assume the requested behavior is completely new.

## Step 1 — Understand the Request

Analyze the user's request only far enough to determine:

* What behavior is being requested.
* Whether existing behavior is being changed.
* Which part of the system appears to be affected.
* Whether architectural analysis is required.
* Whether domain analysis is required.

Do not create OpenSpec artifacts manually during this step.

Do not invent business rules.

If the requirement is fundamentally ambiguous, identify the ambiguity
before continuing.

## Step 2 — Architectural Context

When the change has meaningful architectural impact, use the
`architect` agent.

The Architect should analyze:

* Affected components.
* Existing architectural boundaries.
* Dependencies.
* API implications.
* Persistence implications.
* Messaging implications.
* Projection implications.
* Worker implications.
* Integration implications.
* Architectural risks.
* Decisions that require explicit approval.

The Architect must not implement code.

The Architect must not create a parallel specification system.

The Architect's analysis is contextual input for the OpenSpec planning
workflow.

If the change has no meaningful architectural impact, state:

`Architecture impact: None identified.`

## Step 3 — Domain Context

When the change affects business behavior, use the `domain-expert` agent.

The Domain Expert should analyze:

* Aggregates.
* Entities.
* Value Objects.
* Domain Services.
* Domain Events.
* Business Rules.
* Invariants.
* State transitions.
* Event Sourcing implications.

The Domain Expert must not implement code.

The Domain Expert must not invent business rules.

The Domain Expert's analysis is contextual input for the OpenSpec planning
workflow.

If the change has no meaningful domain impact, state:

`Domain impact: None identified.`

## Step 4 — Existing OpenSpec Context

Before invoking the OpenSpec planning workflow:

* Inspect relevant existing specifications.
* Inspect related active changes.
* Identify existing requirements that the new change may affect.
* Identify potential conflicts.

Do not modify OpenSpec artifacts directly unless the OpenSpec workflow
requires it.

If an existing specification conflicts with the requested behavior,
surface the conflict rather than silently choosing one.

## Step 5 — Delegate Planning to OpenSpec

After the required architectural and domain context has been gathered,
delegate specification planning to OpenSpec.

Use the appropriate OpenSpec planning workflow, normally:

```text
/opsx:propose
```

OpenSpec is responsible for determining and creating the appropriate
change artifacts, including as applicable:

```text
proposal
specs
design
tasks
```

Do not manually recreate these artifacts as part of this command.

Do not maintain a second task list.

Do not duplicate OpenSpec's change lifecycle.

## OpenSpec Planning Rules

When handing context to OpenSpec:

* Preserve the user's original requirement.
* Include relevant architectural findings.
* Include relevant domain findings.
* Include identified constraints.
* Include unresolved decisions.
* Preserve existing OpenSpec specifications.
* Avoid inventing requirements.
* Avoid changing existing behavior without explicit justification.

OpenSpec remains responsible for deciding how that information is
represented in its change artifacts.

## Human Decisions

If the Architect or Domain Expert identifies a decision that cannot be
safely made by the agents, surface it before implementation.

Examples include:

* New business rules.
* Changes to financial calculations.
* Changes to aggregate boundaries.
* Breaking API changes.
* Changes to public message contracts.
* Changes to Event Sourcing semantics.
* Database migration strategy.
* New external integration contracts.
* Introduction of a new database provider.
* Migration from Dapper to EF Core.

Do not silently make these decisions.

If a required decision prevents a coherent OpenSpec change from being
created, stop and report the issue.

## What This Command Does Not Do

This command does not:

* Implement production code.
* Create tests.
* Execute `/opsx:apply`.
* Review implementation.
* Verify implementation.
* Archive OpenSpec changes.
* Replace OpenSpec's proposal/spec/design/tasks workflow.
* Maintain a separate project planning format.

Those responsibilities belong to later stages of the workflow.

## Planning Result

After the OpenSpec planning workflow completes, report:

### Requirement

Briefly summarize the requested change.

### Architectural Context

Summarize the relevant architectural conclusions.

### Domain Context

Summarize the relevant domain conclusions.

### OpenSpec Change

Identify the OpenSpec change created or updated.

Do not reproduce the OpenSpec artifacts in this report unless necessary.

### Open Decisions

List decisions that remain unresolved.

### Status

Return exactly one:

```text
READY
```

The OpenSpec change is sufficiently defined for implementation.

```text
BLOCKED
```

A required architectural or business decision prevents planning from
being completed coherently.

```text
NEEDS_CLARIFICATION
```

The original requirement is insufficiently defined.

## Important

This command is an orchestration layer around OpenSpec.

It exists to provide project-specific architectural and domain expertise.

It does not replace OpenSpec.

The intended lifecycle is:

```text
Requirement
    │
    ▼
  /plan
    │
    ├── Architect
    │
    └── Domain Expert
            │
            ▼
     /opsx:propose
            │
            ▼
       OpenSpec Change
            │
            ├── proposal
            ├── specs
            ├── design
            └── tasks
            │
            ▼
          READY
            │
            ▼
       /implement
```

OpenSpec remains the source of truth for the change.
