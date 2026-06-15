## Why

The automatic conversion of an approved Prospect to a Customer is already implemented (via the approval workflow change). However, the `CustomerUpdated` event is published as an anonymous object in `UpdateCustomerCommand` instead of a typed contract, creating an inconsistency with `CustomerCreated` (which uses `CreateCustomerContract`). This change formalizes the `CustomerUpdated` event contract and ensures both customer lifecycle events have consistent, versioned payloads.

## What Changes

- Add `CustomerUpdatedContract` typed record in `Crm.Application/Customers/Dtos/`
- Update `UpdateCustomerCommand` to publish `CustomerUpdatedContract` instead of an anonymous object
- Add the `CustomerUpdated` event to the `customer-management` spec as a requirement

### Already implemented (no action needed)
- `ApplicationApproved → Convert Prospect to Customer` — done in `ApprovalWorkflowService`
- `CustomerCreated` event — published as `CreateCustomerContract` from both direct and origination paths
- `PUT /api/v1/customers/{id}` — `UpdateCustomerCommand` exists and works

## Capabilities

### New Capabilities
- (none)

### Modified Capabilities
- `customer-management`: Add requirement "Customer profile changes publish a CustomerUpdated event" — the behavior exists but is not specced and uses an untyped payload.

## Impact

- `Crm.Application/Customers/Dtos/CustomerUpdatedContract.cs` — new file
- `Crm.Application/Customers/UpdateCustomerCommand.cs` — replace anonymous event object with `CustomerUpdatedContract`
- `openspec/specs/customer-management/spec.md` — delta spec adds new requirement
