## 1. Instalación y configuración

- [x] 1.1 Agregar referencia al NuGet `SmartCore.Outbox` en `Crm.Infrastructure.csproj`
- [x] 1.2 Registrar `AddSmartOutbox(...)` con `ServiceName = "crm"` en el módulo de extensiones de `Crm.Infrastructure`
- [x] 1.3 Agregar `Outbox:ConnectionString` en `appsettings.Development.json` apuntando a `outbox_db`
- [ ] 1.4 Verificar que `IOutboxWriter` e `IIdempotencyGuard` resuelven correctamente desde el contenedor DI al iniciar la app

## 2. Migrar CreateCustomerCommand

- [x] 2.1 Inyectar `IOutboxWriter` en `CreateCustomerCommand` handler
- [x] 2.2 Reemplazar `PublishEvent(contract)` por `AppendAsync()` con `EventType = "CustomerCreated"`, `AggregateId = customer.Id`, `DeduplicationKey = $"CustomerCreated:{customer.Id}"` y payload serializado incluyendo `Metadata` financiero
- [x] 2.3 Reemplazar `SendCommand<CustomerCreated>(contract, "credit-service-customer-events")` por `AppendAsync()` con `EventType = "CustomerCreatedCommand"`, `AggregateId = customer.Id`, `DeduplicationKey = $"CustomerCreatedCommand:{customer.Id}"` y el mismo payload — el outbox-worker enruta con `RouteType: "Command"`, `Queue: "credit-service-customer-events"`

## 3. Migrar UpdateCustomerCommand

- [x] 3.1 Inyectar `IOutboxWriter` en `UpdateCustomerCommand` handler
- [x] 3.2 Reemplazar `PublishEvent(new CustomerUpdatedContract(...))` por `AppendAsync()` con `EventType = "CustomerUpdated"`, `AggregateId = customer.Id`, `DeduplicationKey = $"CustomerUpdated:{customer.Id}:{DateTime.UtcNow:yyyyMMddHHmmssfff}"` y payload serializado

## 4. Migrar UpdateCustomerFinancialsCommand

- [x] 4.1 Inyectar `IOutboxWriter` en `UpdateCustomerFinancialsCommand` handler
- [x] 4.2 Reemplazar `PublishEvent()` por `AppendAsync()` con `EventType = "CustomerFinancialsUpdated"`, `AggregateId = customerId`, `DeduplicationKey = $"CustomerFinancialsUpdated:{customerId}:{DateTime.UtcNow:yyyyMMddHHmmssfff}"` y payload con solo los campos incluidos en el request

## 5. Migrar CreateProspectCommand

- [x] 5.1 Inyectar `IOutboxWriter` en `CreateProspectCommand` handler
- [x] 5.2 Reemplazar `PublishEvent(new ProspectCreatedEvent(...))` por `AppendAsync()` con `EventType = "ProspectCreated"`, `AggregateId = prospect.Id`, `DeduplicationKey = $"ProspectCreated:{prospect.Id}"` y payload serializado

## 6. Migrar CreateCreditApplicationCommand

- [x] 6.1 Inyectar `IOutboxWriter` en `CreateCreditApplicationCommand` handler
- [x] 6.2 Reemplazar `PublishEvent(new CreditApplicationCreatedContract {...})` por `AppendAsync()` con `EventType = "CreditApplicationCreated"`, `AggregateId = application.Id`, `DeduplicationKey = $"CreditApplicationCreated:{application.Id}"` y payload serializado

## 7. Migrar SubmitCreditApplicationCommand

- [x] 7.1 Inyectar `IOutboxWriter` en `SubmitCreditApplicationCommand` handler
- [x] 7.2 Reemplazar `PublishEvent(new CreditApplicationSubmittedContract {...})` → `AppendAsync()` con `EventType = "CreditApplicationSubmitted"`, `DeduplicationKey = $"CreditApplicationSubmitted:{application.Id}"`
- [x] 7.3 Reemplazar `PublishEvent(new ApprovalRequestedContract(...))` → `AppendAsync()` con `EventType = "ApprovalRequested"`, `AggregateId = application.Id`, `DeduplicationKey = $"ApprovalRequested:{application.Id}:{firstStep.Id}"`
- [x] 7.4 Reemplazar `PublishEvent(new RiskEvaluationCompletedContract {...})` → `AppendAsync()` con `EventType = "RiskEvaluationCompleted"`, `AggregateId = application.Id`, `DeduplicationKey = $"RiskEvaluationCompleted:{application.Id}"`
- [x] 7.5 Reemplazar `PublishEvent(new CreditApplicationApprovedContract {...})` → `AppendAsync()` con `EventType = "CreditApplicationApproved"`, `DeduplicationKey = $"CreditApplicationApproved:{application.Id}"`
- [x] 7.6 Reemplazar `PublishEvent(new ProspectConvertedContract {...})` → `AppendAsync()` con `EventType = "ProspectConverted"`, `AggregateId = prospect.Id`, `DeduplicationKey = $"ProspectConverted:{prospect.Id}"`
- [x] 7.7 Reemplazar `PublishEvent(contract)` para `CustomerCreated` broadcast → `AppendAsync()` con `EventType = "CustomerCreated"`, `AggregateId = createdCustomer.Id`, `DeduplicationKey = $"CustomerCreated:{createdCustomer.Id}"`
- [x] 7.8 Reemplazar `PublishEvent(new CreditApplicationRejectedContract {...})` → `AppendAsync()` con `EventType = "CreditApplicationRejected"`, `DeduplicationKey = $"CreditApplicationRejected:{application.Id}"`
- [x] 7.9 Reemplazar `SendCommand<CustomerCreated>(contract, "credit-service-customer-events")` por `AppendAsync()` con `EventType = "CustomerCreatedCommand"`, `AggregateId = createdCustomer.Id`, `DeduplicationKey = $"CustomerCreatedCommand:{createdCustomer.Id}"` — el outbox-worker enruta con `RouteType: "Command"`, `Queue: "credit-service-customer-events"`

## 8. Migrar TriggerRiskEvaluationCommand

- [x] 8.1 Inyectar `IOutboxWriter` en `TriggerRiskEvaluationCommand` handler
- [x] 8.2 Reemplazar `PublishEvent(new RiskEvaluationStartedContract {...})` → `AppendAsync()` con `EventType = "RiskEvaluationStarted"`, `AggregateId = application.Id`, `DeduplicationKey = $"RiskEvaluationStarted:{application.Id}"`
- [x] 8.3 Reemplazar `PublishEvent(new RiskEvaluationCompletedContract {...})` → `AppendAsync()` con `EventType = "RiskEvaluationCompleted"`, `AggregateId = application.Id`, `DeduplicationKey = $"RiskEvaluationCompleted:{application.Id}:{DateTime.UtcNow:yyyyMMddHHmmssfff}"` (puede evaluarse múltiples veces)

## 9. Migrar RegisterDocumentCommand

- [x] 9.1 Inyectar `IOutboxWriter` en `RegisterDocumentCommand` handler
- [x] 9.2 Reemplazar `PublishEvent(new DocumentUploadedContract(...))` → `AppendAsync()` con `EventType = "DocumentUploaded"`, `AggregateId = document.Id`, `DeduplicationKey = $"DocumentUploaded:{document.Id}"` y payload serializado

## 10. Migrar ValidateDocumentCommand

- [x] 10.1 Inyectar `IOutboxWriter` en `ValidateDocumentCommand` handler
- [x] 10.2 Reemplazar `PublishEvent(new DocumentValidatedContract(...))` → `AppendAsync()` con `EventType = "DocumentValidated"`, `AggregateId = document.Id`, `DeduplicationKey = $"DocumentValidated:{document.Id}"`
- [x] 10.3 Reemplazar `PublishEvent(new DocumentRejectedContract(...))` → `AppendAsync()` con `EventType = "DocumentRejected"`, `AggregateId = document.Id`, `DeduplicationKey = $"DocumentRejected:{document.Id}"`

## 11. Migrar ApprovalWorkflowService

- [x] 11.1 Inyectar `IOutboxWriter` en `ApprovalWorkflowService`
- [x] 11.2 Reemplazar `PublishEvent(new ApplicationRejectedContract(...))` → `AppendAsync()` con `EventType = "ApplicationRejected"`, `AggregateId = application.Id`, `DeduplicationKey = $"ApplicationRejected:{application.Id}"`
- [x] 11.3 Reemplazar `PublishEvent(new ApplicationApprovedContract(...))` → `AppendAsync()` con `EventType = "ApplicationApproved"`, `AggregateId = application.Id`, `DeduplicationKey = $"ApplicationApproved:{application.Id}"`
- [x] 11.4 Reemplazar `PublishEvent(new ProspectConvertedContract {...})` → `AppendAsync()` con `EventType = "ProspectConverted"`, `AggregateId = prospect.Id`, `DeduplicationKey = $"ProspectConverted:{prospect.Id}"`
- [x] 11.5 Reemplazar `PublishEvent(contract)` para `CustomerCreated` broadcast → `AppendAsync()` con `EventType = "CustomerCreated"`, `AggregateId = customer.Id`, `DeduplicationKey = $"CustomerCreated:{customer.Id}"`
- [x] 11.6 Reemplazar `PublishEvent(new ApprovalRequestedContract(...))` para el siguiente step → `AppendAsync()` con `EventType = "ApprovalRequested"`, `AggregateId = application.Id`, `DeduplicationKey = $"ApprovalRequested:{application.Id}:{nextStep.Id}"`
- [x] 11.7 Reemplazar `SendCommand<CustomerCreated>(contract, "credit-service-customer-events")` por `AppendAsync()` con `EventType = "CustomerCreatedCommand"`, `AggregateId = customer.Id`, `DeduplicationKey = $"CustomerCreatedCommand:{customer.Id}"` — el outbox-worker enruta con `RouteType: "Command"`, `Queue: "credit-service-customer-events"`

## 12. Limpieza

- [x] 12.1 Verificar que `IMqProducerService.PublishEvent()` ya no es invocado en ningún handler
- [x] 12.2 Hacer build completo (`dotnet build CrmProject.sln`) sin errores ni warnings nuevos

## 13. Validación

- [ ] 13.1 Crear cliente → verificar fila `CustomerCreated` en `outbox_db.Events` con `Status = Pending`
- [ ] 13.2 Actualizar cliente → verificar fila `CustomerUpdated` en `outbox_db.Events`
- [ ] 13.3 Crear prospecto → verificar fila `ProspectCreated` en `outbox_db.Events`
- [ ] 13.4 Crear credit application → verificar fila `CreditApplicationCreated` en `outbox_db.Events`
- [ ] 13.5 Ejecutar flujo de aprobación completo → verificar filas `CreditApplicationApproved`, `ProspectConverted`, `CustomerCreated` en `outbox_db.Events`
- [ ] 13.6 Registrar documento → verificar fila `DocumentUploaded` en `outbox_db.Events`
- [ ] 13.7 Validar/rechazar documento → verificar fila `DocumentValidated` o `DocumentRejected` en `outbox_db.Events`
- [ ] 13.8 Verificar que el `outbox-worker` procesa las filas `Pending` y actualiza `Status = Published`
- [ ] 13.9 Confirmar con infra que todos los `EventType` están configurados en el `outbox-worker`
