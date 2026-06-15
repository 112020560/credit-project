## 1. Application — Event Contract

- [x] 1.1 Create `CustomerUpdatedContract` record in `Crm.Application/Customers/Dtos/` with properties: CustomerId (Guid), UpdatedAt (DateTimeOffset), Version (int), Changes (IDictionary<string, object>)
- [x] 1.2 Update `UpdateCustomerCommand` handler to publish `CustomerUpdatedContract` instead of the anonymous object
