## Why

Los eventos de dominio del CRM se publican actualmente de forma directa a RabbitMQ desde los CommandHandlers, lo que expone el sistema a pérdida de eventos si el broker no está disponible o la aplicación falla entre el commit de dominio y el publish. Adoptamos `SmartCore.Outbox` para garantizar durabilidad, trazabilidad completa e idempotencia en la entrega de eventos.

## What Changes

- Instalar el NuGet `SmartCore.Outbox` en `Crm.Infrastructure` y `Crm.WebApi`.
- Registrar `IOutboxWriter` e `IIdempotencyGuard` en el contenedor DI vía `AddSmartOutbox(...)`.
- Agregar el connection string de `outbox_db` en `appsettings.json` / `appsettings.Development.json`.
- Reemplazar todas las llamadas a `IMqProducerService.PublishEvent()` en CommandHandlers por `IOutboxWriter.AppendAsync()`.
- Los eventos y comandos ya no se publican directamente a Rabbit; el `outbox-worker` (servicio independiente ya desplegado) lee la tabla `Events` en `outbox_db` y hace el routing: exchange (broadcast) o queue directa (punto-a-punto) según el `RouteType` configurado por `EventType`.
- **BREAKING**: `IMqProducerService` deja de ser invocado en todos los CommandHandlers. Tanto `PublishEvent()` como `SendCommand()` se reemplazan por `IOutboxWriter.AppendAsync()`. `SmartCore.Outbox` v1.1.0 soporta ambos patrones de routing.

## Capabilities

### New Capabilities

- `outbox-event-publishing`: Persistencia de eventos de dominio en `outbox_db` mediante `IOutboxWriter.AppendAsync()` con clave de deduplicación, garantizando at-least-once delivery y trazabilidad completa de la historia de eventos del CRM.

### Modified Capabilities

- `customer-management`: Los eventos `CustomerCreated` y `CustomerUpdated` pasan por el outbox en lugar de publicarse directo a Rabbit.
- `prospect-management`: El evento `ProspectCreated` pasa por el outbox.
- `prospect-to-customer-conversion`: El evento `CustomerConverted` pasa por el outbox.
- `customer-financial-profile`: El evento `CustomerFinancialsUpdated` pasa por el outbox.

## Impact

- **NuGet nuevo**: `SmartCore.Outbox` — agrega dependencia a `Crm.Infrastructure`.
- **Configuración**: nueva clave `Outbox:ConnectionString` apuntando a `outbox_db`.
- **CommandHandlers / Services afectados** (todos los `PublishEvent` se migran; los `SendCommand` permanecen):
  - `CreateCustomerCommand` → `CustomerCreated` (broadcast)
  - `UpdateCustomerCommand` → `CustomerUpdatedContract`
  - `UpdateCustomerFinancialsCommand` → `CustomerUpdated`
  - `CreateProspectCommand` → `ProspectCreatedEvent`
  - `CreateCreditApplicationCommand` → `CreditApplicationCreatedContract`
  - `SubmitCreditApplicationCommand` → `CreditApplicationSubmitted`, `ApprovalRequested`, `RiskEvaluationCompleted`, `CreditApplicationApproved`, `ProspectConverted`, `CustomerCreated`, `CreditApplicationRejected`
  - `TriggerRiskEvaluationCommand` → `RiskEvaluationStarted`, `RiskEvaluationCompleted`
  - `RegisterDocumentCommand` → `DocumentUploaded`
  - `ValidateDocumentCommand` → `DocumentValidated`, `DocumentRejected`
  - `ApprovalWorkflowService` → `ApplicationRejected`, `ApplicationApproved`, `ProspectConverted`, `CustomerCreated`, `ApprovalRequested`
- **Sin cambios en contratos HTTP**: ningún endpoint cambia su request/response.
- **Sin cambios en el dominio**: `Crm.Domain` no se toca.
- **outbox-worker**: requiere configuración de todos los `EventType` listados arriba en su `appsettings.json`.
