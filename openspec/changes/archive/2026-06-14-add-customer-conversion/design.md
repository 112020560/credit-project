## Context

`UpdateCustomerCommand` currently publishes a `CustomerUpdated` event using an anonymous object. This is inconsistent with `CreateCustomerCommand` and the origination path, which both use the typed `CreateCustomerContract`. An anonymous payload cannot be deserialized by consumers and is invisible to static analysis or tooling.

The fix is purely additive: introduce `CustomerUpdatedContract` (a record) and substitute it for the anonymous object in the handler. No DB changes, no new endpoints, no migration needed.

## Goals / Non-Goals

**Goals:**
- Typed `CustomerUpdatedContract` record with stable field names and types
- `UpdateCustomerCommand` publishes the typed contract
- Consistent event structure across all customer lifecycle events

**Non-Goals:**
- Changes to the conversion flow (already implemented)
- Adding new update fields beyond what the handler already sets
- Consumer-side deserialization changes (out of scope)

## Decisions

### Use a record type matching the existing anonymous shape
The existing anonymous object has: `CustomerId`, `UpdatedAt`, `Version`, `Changes` (dictionary). The new `CustomerUpdatedContract` will mirror this shape exactly so consumers observing the existing anonymous events are not broken.

*Alternative considered*: Flatten `Changes` into top-level fields. Rejected — would break any existing consumers of the anonymous event.

## Risks / Trade-offs

- Minimal risk — the change is a one-line substitution in the handler with a new file.
- `Changes` dictionary type is preserved to avoid consumer breakage.

## Migration Plan

No migration required. The event shape is identical — only the CLR type changes from anonymous to `CustomerUpdatedContract`.

## Open Questions

- None.
