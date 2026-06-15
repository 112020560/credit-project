## 1. Domain — Prospect Aggregate

- [x] 1.1 Create `Prospect` entity in `Crm.Domain/Prospects/` with properties: Id, IdentificationType, IdentificationNumber, FullName, DisplayName, BirthDate, Status, CreatedAt, UpdatedAt
- [x] 1.2 Create `ProspectAddress`, `ProspectPhone`, `ProspectEmail`, `ProspectWorkInfo`, `ProspectFiscalInfo` value entities in `Crm.Domain/Prospects/`
- [x] 1.3 Create `ProspectStatus` enum/constants: `Draft`, `Submitted`, `Converted`
- [x] 1.4 Create `ProspectError` static class with domain errors (NotFound, AlreadyConverted, DuplicateIdentification)

## 2. Domain — CreditApplication Aggregate

- [x] 2.1 Create `CreditApplication` entity in `Crm.Domain/CreditApplications/` with properties: Id, ProspectId, Status, RejectionReason, CreatedAt, UpdatedAt
- [x] 2.2 Create `ApplicationDocument` entity with properties: Id, CreditApplicationId, Type, StorageUrl, Status, UploadedAt
- [x] 2.3 Create `CreditApplicationStatus` enum/constants: `Draft`, `Submitted`, `InReview`, `Approved`, `Rejected`
- [x] 2.4 Create `ApplicationDocumentStatus` enum/constants: `Uploaded`, `Verified`
- [x] 2.5 Create `ApplicationDocumentType` enum/constants: `NationalId`, `Passport`, `IncomeProof`, `BankStatement`, `TaxRegistration`
- [x] 2.6 Create `CreditApplicationError` static class with domain errors (NotFound, InvalidTransition, MissingDocuments, AlreadyProcessed)

## 3. Domain — Persistence Abstractions

- [x] 3.1 Create `IProspectsRepository` interface in `Crm.Domain/Abstractions/Persistence/` with: `AddAsync`, `GetByIdAsync`, `ExistsByIdentificationAsync`, `UpdateAsync`
- [x] 3.2 Create `ICreditApplicationsRepository` interface with: `AddAsync`, `GetByIdAsync`, `GetByProspectIdAsync`, `UpdateAsync`

## 4. Infrastructure — Database

- [x] 4.1 Add `DbSet<Prospect>`, `DbSet<ProspectAddress>`, `DbSet<ProspectPhone>`, `DbSet<ProspectEmail>`, `DbSet<ProspectWorkInfo>`, `DbSet<ProspectFiscalInfo>` to `CrmDbContext`
- [x] 4.2 Add `DbSet<CreditApplication>`, `DbSet<ApplicationDocument>` to `CrmDbContext`
- [x] 4.3 Configure EF entity mappings for all new entities in `CrmDbContext.OnModelCreating` (table names, column names, FKs, indexes)
- [x] 4.4 Add EF Core migration: `dotnet ef migrations add AddCreditOriginationFoundation`
- [x] 4.5 Verify migration SQL and apply to development database

## 5. Infrastructure — Repositories

- [x] 5.1 Implement `ProspectsRepository : IProspectsRepository` in `Crm.Infrastructure/Adapters/Outbound/EntityFramework/Repositories/`
- [x] 5.2 Implement `CreditApplicationsRepository : ICreditApplicationsRepository` in the same folder
- [x] 5.3 Register both repositories in `Crm.Infrastructure/DependencyInjection.cs`

## 6. Application — Prospect Commands & Queries

- [x] 6.1 Create `CreateProspectCommand` + handler: validates no duplicate identification, persists Prospect, publishes `ProspectCreated` event
- [x] 6.2 Create `EnrichProspectCommand` + handler: adds/updates address, contact, work info, or fiscal info; blocked on `Converted` status
- [x] 6.3 Create `GetProspectByIdQuery` + handler
- [x] 6.4 Create DTOs: `CreateProspectDto`, `ProspectSummaryDto`, `EnrichProspectDto`
- [x] 6.5 Add FluentValidation validators for `CreateProspectCommand` and `EnrichProspectCommand`

## 7. Application — CreditApplication Commands & Queries

- [x] 7.1 Create `CreateCreditApplicationCommand` + handler: validates Prospect exists and is in `Draft`, persists application, publishes `CreditApplicationCreated`
- [x] 7.2 Create `SubmitCreditApplicationCommand` + handler: validates required documents present, transitions status Draft→Submitted, transitions Prospect Draft→Submitted, publishes `CreditApplicationSubmitted`
- [x] 7.3 Create `ApproveCreditApplicationCommand` + handler: validates status is `InReview`, calls conversion logic, publishes `CreditApplicationApproved` + `ProspectConvertedToCustomer` + `CustomerCreated`
- [x] 7.4 Create `RejectCreditApplicationCommand` + handler: validates status is `InReview` and reason is non-empty, transitions to `Rejected`, returns Prospect to `Draft`, publishes `CreditApplicationRejected`
- [x] 7.5 Create `GetCreditApplicationByIdQuery` + handler
- [x] 7.6 Create DTOs: `CreateCreditApplicationDto`, `CreditApplicationDetailDto`, `RejectCreditApplicationDto`
- [x] 7.7 Add FluentValidation validators for all commands

## 8. Application — Prospect-to-Customer Conversion

- [x] 8.1 Create `ConvertProspectToCustomerCommand` + handler (called internally from ApproveCreditApplicationCommandHandler): maps all Prospect data to a new `Customer` entity, sets Prospect Status to `Converted`, wraps both in a single `UnitOfWork.SaveChangesAsync` transaction
- [x] 8.2 Ensure `CustomerCreated` event published after conversion uses the same `CreateCustomerContract` structure as the direct path

## 9. Application — ApplicationDocument Commands

- [x] 9.1 Create `RegisterApplicationDocumentCommand` + handler: validates application is in `Draft` or `Submitted`, validates document type, persists `ApplicationDocument` with Status `Uploaded`
- [x] 9.2 Create DTOs: `RegisterApplicationDocumentDto`, `ApplicationDocumentDto`
- [x] 9.3 Add FluentValidation for `RegisterApplicationDocumentCommand`

## 10. WebApi — Endpoints

- [x] 10.1 Create `Crm.WebApi/Endpoints/Prospects/Create.cs` — `POST /prospects` → `CreateProspectCommand`
- [x] 10.2 Create `Crm.WebApi/Endpoints/Prospects/Enrich.cs` — `PUT /prospects/{id}` → `EnrichProspectCommand`
- [x] 10.3 Create `Crm.WebApi/Endpoints/Prospects/GetById.cs` — `GET /prospects/{id}` → `GetProspectByIdQuery`
- [x] 10.4 Create `Crm.WebApi/Endpoints/CreditApplications/Create.cs` — `POST /credit-applications` → `CreateCreditApplicationCommand`
- [x] 10.5 Create `Crm.WebApi/Endpoints/CreditApplications/GetById.cs` — `GET /credit-applications/{id}` → `GetCreditApplicationByIdQuery`
- [x] 10.6 Create `Crm.WebApi/Endpoints/CreditApplications/Submit.cs` — `POST /credit-applications/{id}/submit` → `SubmitCreditApplicationCommand`
- [x] 10.7 Create `Crm.WebApi/Endpoints/CreditApplications/Approve.cs` — `POST /credit-applications/{id}/approve` → `ApproveCreditApplicationCommand`
- [x] 10.8 Create `Crm.WebApi/Endpoints/CreditApplications/Reject.cs` — `POST /credit-applications/{id}/reject` → `RejectCreditApplicationCommand`
- [x] 10.9 Create `Crm.WebApi/Endpoints/CreditApplications/RegisterDocument.cs` — `POST /credit-applications/{id}/documents` → `RegisterApplicationDocumentCommand`
- [x] 10.10 Add `WithTags`, `WithOpenApi`, `MapToApiVersion(v1)` and correct `Produces` to all new endpoints

## 11. Event Contracts

- [x] 11.1 Add `ProspectCreated`, `CreditApplicationCreated`, `CreditApplicationSubmitted`, `CreditApplicationApproved`, `CreditApplicationRejected`, `ProspectConvertedToCustomer` contracts to `SharedKernel.Contracts` (or equivalent contracts assembly)
- [x] 11.2 Verify `CustomerCreated` contract is reused unchanged from the existing direct-creation path
