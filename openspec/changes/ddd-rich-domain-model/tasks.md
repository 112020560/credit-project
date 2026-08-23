## 1. Infraestructura de Domain Events

- [x] 1.1 Crear `IDomainEvent` marker interface en `Crm.Domain/Abstractions/IDomainEvent.cs` (usa SharedKernel.IDomainEvent)
- [x] 1.2 Crear clase abstracta `AggregateRoot` en `Crm.Domain/Abstractions/AggregateRoot.cs` (usa SharedKernel.AggregateRoot)
- [x] 1.3 Ignorar `DomainEvents` en EF Core: en `CrmDbContext` agregar `.Ignore(e => e.DomainEvents)` para cada aggregate root

## 2. Aggregate Root: CreditApplication

- [x] 2.1 Crear domain events en `Crm.Domain/CreditApplications/Events/`: `CreditApplicationSubmittedEvent`, `CreditApplicationApprovedEvent`, `CreditApplicationRejectedEvent`, `CreditApplicationSentToReviewEvent`
- [x] 2.2 Hacer que `CreditApplication` herede de `AggregateRoot`
- [x] 2.3 Agregar método `Submit()` en `CreditApplication` con validación de precondición (Status == Draft) y `RaiseDomainEvent(CreditApplicationSubmittedEvent)`
- [x] 2.4 Agregar método `Approve()` en `CreditApplication` con validación de precondición (Status == Submitted) y `RaiseDomainEvent(CreditApplicationApprovedEvent)`
- [x] 2.5 Agregar método `Reject(string reason)` en `CreditApplication` con validación de precondición y `RaiseDomainEvent(CreditApplicationRejectedEvent)`
- [x] 2.6 Agregar método `SendToReview(Guid? workflowDefinitionId)` en `CreditApplication` con validación de precondición y `RaiseDomainEvent(CreditApplicationSentToReviewEvent)`

## 3. Aggregate Root: Prospect

- [x] 3.1 Crear domain events en `Crm.Domain/Prospects/Events/`: `ProspectSubmittedEvent`, `ProspectConvertedEvent`, `ProspectRejectedEvent`
- [x] 3.2 Hacer que `Prospect` herede de `AggregateRoot`
- [x] 3.3 Agregar método `Submit()` en `Prospect` (Draft → Submitted) con `RaiseDomainEvent`
- [x] 3.4 Agregar método `Convert()` en `Prospect` (Submitted → Converted) con validación y `RaiseDomainEvent`
- [x] 3.5 Agregar método `Reject()` en `Prospect` (Submitted → Draft) con `RaiseDomainEvent`

## 4. Aggregate Root: Customer

- [x] 4.1 Crear domain events en `Crm.Domain/Customers/Events/`: `CustomerCreatedEvent`, `CustomerActivatedEvent`, `CustomerDeactivatedEvent`, `CustomerProfileUpdatedEvent`, `CustomerFinancialsUpdatedEvent`
- [x] 4.2 Hacer que `Customer` herede de `AggregateRoot`
- [x] 4.3 Agregar método `Activate()` en `Customer` con `RaiseDomainEvent`
- [x] 4.4 Agregar método `Deactivate()` en `Customer` con `RaiseDomainEvent`
- [x] 4.5 Agregar método `UpdateProfile(fullName, displayName, identificationType, identificationNumber, birthDate)` con `RaiseDomainEvent`
- [x] 4.6 Agregar método `UpdateFinancials(creditScore, monthlyIncome, monthlyDebt)` con `RaiseDomainEvent`

## 5. Aggregate Roots: WorkflowDefinition y RiskMatrix

- [x] 5.1 Crear `WorkflowDefinitionActivatedEvent` en `Crm.Domain/ApprovalWorkflows/Events/`
- [x] 5.2 Hacer que `WorkflowDefinition` herede de `AggregateRoot` y agregar `Activate()` / `Deactivate()` con `RaiseDomainEvent`
- [x] 5.3 Crear `RiskMatrixActivatedEvent` en `Crm.Domain/RiskEngine/Events/`
- [x] 5.4 Hacer que `RiskMatrix` herede de `AggregateRoot` y agregar `Activate()` con `RaiseDomainEvent`

## 6. Value Objects compartidos (Customer + Prospect)

- [x] 6.1 Crear directorio `Crm.Domain/ValueObjects/` y definir `Address.cs` como `record` con propiedades: `Type`, `Street`, `City`, `State`, `Country`, `PostalCode`, `IsPrimary`
- [x] 6.2 Crear `EmailContact.cs` (`record`): `Email`, `IsPrimary`, `Verified`
- [x] 6.3 Crear `PhoneContact.cs` (`record`): `Number`, `Type`, `IsPrimary`, `Verified`
- [x] 6.4 Crear `WorkInfo.cs` (`record`): `Occupation`, `EmployerName`, `Salary`, `WorkAddress`
- [x] 6.5 Crear `FiscalInfo.cs` (`record`): `TaxId` y demás campos relevantes
- [x] 6.6 Reemplazar las colecciones de `Customer`: cambiar `ICollection<CustomerAddress>` → `ICollection<Address>`, etc. para todos los tipos de VO
- [x] 6.7 Reemplazar las colecciones de `Prospect`: mismos cambios con los tipos compartidos
- [x] 6.8 Eliminar los archivos `CustomerAddress.cs`, `CustomerEmail.cs`, `CustomerPhone.cs`, `CustomerFiscalInfo.cs`, `CustomerWorkInfo.cs` del Domain
- [x] 6.9 Eliminar los archivos `ProspectAddress.cs`, `ProspectEmail.cs`, `ProspectPhone.cs`, `ProspectWorkInfo.cs`, `ProspectFiscalInfo.cs` del Domain
- [x] 6.10 Actualizar `CrmDbContext`: configurar `OwnsMany` para Customer → `customer_addresses`, `customer_emails`, etc. con shadow properties para el Id interno
- [x] 6.11 Actualizar `CrmDbContext`: configurar `OwnsMany` para Prospect → `prospect_addresses`, `prospect_emails`, etc.
- [x] 6.12 Actualizar `CreateCustomerCommand`, `UpdateCustomerCommand` para construir VOs usando record constructors (sin asignar Id)
- [x] 6.13 Actualizar `CreateProspectCommand` y `EnrichProspectCommand` para construir VOs con los tipos compartidos

## 8. Factory Method: Customer.FromProspect

- [x] 8.1 Crear `Customer.FromProspect(Prospect prospect)` static factory method en `Customer.cs` que copie todos los campos de identidad y colecciones de VOs, fije Status = Active, y llame `RaiseDomainEvent(CustomerCreatedEvent)`
- [x] 8.2 Eliminar el método estático `ApproveCreditApplicationCommandHandler.MapProspectToCustomer` de `ApproveCreditApplicationCommand.cs`
- [x] 8.3 Actualizar `SubmitCreditApplicationCommand` (AutoApprove path) para usar `Customer.FromProspect(prospect)` en lugar del método eliminado
- [x] 8.4 Actualizar `ApprovalWorkflowService.RecordDecisionAsync` (ManualReview approve) para usar `Customer.FromProspect(prospect)`
- [x] 8.5 Actualizar `ApproveCreditApplicationCommand` (si tiene lógica propia) para usar `Customer.FromProspect(prospect)`

## 9. Notification Handlers para coordinación entre aggregates (REC-05)

- [ ] 9.1 Crear `CreditApplicationApprovedHandler : INotificationHandler<CreditApplicationApprovedEvent>` en `Crm.Application/CreditApplications/`: carga Prospect, llama `prospect.Convert()`, llama `Customer.FromProspect(prospect)`, guarda Prospect + Customer, publica `ProspectConverted` y `CustomerCreated` al outbox
- [ ] 9.2 Crear `CreditApplicationRejectedHandler : INotificationHandler<CreditApplicationRejectedEvent>` en `Crm.Application/CreditApplications/`: carga Prospect, llama `prospect.Reject()`, guarda Prospect, publica `ProspectRejected` al outbox
- [ ] 9.3 Crear `CreditApplicationSentToReviewHandler : INotificationHandler<CreditApplicationSentToReviewEvent>` en `Crm.Application/CreditApplications/`: carga Prospect, llama `prospect.Submit()`, guarda Prospect, publica `ApprovalRequested` al outbox
- [ ] 9.4 Registrar los notification handlers en `Crm.Application/DependencyInjection.cs` (MediatR los descubre por reflection pero verificar el assembly scan)

## 10. Actualizar Command Handlers (un aggregate por handler)

- [x] 10.1 Simplificar `SubmitCreditApplicationCommand`: usar `application.Approve()` / `application.SendToReview()` / `application.Reject()` + `prospect.Convert()` / `prospect.Submit()` / `prospect.Reject()` en lugar de asignación directa
- [x] 10.2 Simplificar `ApproveCreditApplicationCommand`: delega a `ApprovalWorkflowService` (ya simplificado)
- [x] 10.3 Simplificar `RejectCreditApplicationCommand`: delega a `ApprovalWorkflowService.RecordDecisionAsync` (ya usa `application.Reject`)
- [x] 10.4 Simplificar `ApprovalWorkflowService.RecordDecisionAsync`: usa `application.Approve()`, `application.Reject()`, `prospect.Convert()`, `prospect.Reject()`
- [x] 10.5 Actualizar `ActivateWorkflowDefinitionCommand`: usar `workflowDefinition.Activate()` y `active.Deactivate()`
- [x] 10.6 Actualizar `ActivateRiskMatrixCommand`: usar `riskMatrix.Activate()`
- [x] 10.7 Actualizar `UpdateCustomerCommand`: usar `customer.UpdateProfile(...)`
- [x] 10.8 Actualizar `UpdateCustomerFinancialsCommand`: usar `customer.UpdateFinancials(...)`
- [x] 10.9 Unificar nombres de eventos de integración: reemplazar `ApplicationApproved` → `CreditApplicationApproved` y `ApplicationRejected` → `CreditApplicationRejected`

## 11. Migración de Base de Datos

- [x] 11.1 Ejecutar `dotnet ef migrations add RichDomainModelValueObjects --project ../Crm.Infrastructure --startup-project .` desde `Crm.WebApi/`
- [x] 11.2 Revisar el SQL generado: verificar que las tablas de VOs mantienen los mismos nombres que las tablas originales (`customer_addresses`, `prospect_emails`, etc.)
- [ ] 11.3 Ejecutar `dotnet ef database update` en entorno de desarrollo (manual)

## 12. Verificación

- [x] 12.1 Ejecutar `dotnet build CrmProject.sln` y resolver todos los errores de compilación — Build succeeded
- [x] 12.2 Verificar que ningún handler muta propiedades de aggregates directamente — sin instancias de `application.Status =`, `prospect.Status =`, `customer.FullName =`
- [x] 12.3 Verificar que `Customer.FromProspect` es el único punto de conversión — `MapProspectToCustomer` eliminado
- [ ] 12.4 Verificar que `SubmitCreditApplicationCommand` solo toca `CreditApplication` (no carga ni guarda Prospect ni Customer directamente) — pendiente si se implementa tarea 9
- [ ] 12.5 Verificar que el flujo completo de submit → auto-approve / manual-review / auto-reject funciona end-to-end manualmente
