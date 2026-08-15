## Context

Actualmente los CommandHandlers del CRM invocan `IMqProducerService.PublishEvent()` directamente tras el commit de dominio. Esto genera una ventana de pérdida de eventos: si RabbitMQ está caído o la app falla entre el `SaveChanges` y el publish, el evento desaparece sin dejar rastro.

`SmartCore.Outbox` es un NuGet interno ya construido y publicado que implementa el patrón Transactional Outbox. Expone `IOutboxWriter` para persistir eventos en una base de datos dedicada (`outbox_db`) y `IIdempotencyGuard` para garantizar procesamiento único en los consumidores. Un `outbox-worker` independiente (ya desplegado en infraestructura) lee `outbox_db` y entrega los eventos a RabbitMQ.

CommandHandlers afectados:
- `CreateCustomerCommand`
- `UpdateCustomerFinancialsCommand`
- `CreateProspectCommand`
- `ConvertProspectToCustomerCommand` (si aplica)

## Goals / Non-Goals

**Goals:**
- Garantizar durabilidad de eventos: ningún evento de dominio se pierde aunque Rabbit esté caído.
- Mantener trazabilidad completa de todos los eventos emitidos por el CRM en `outbox_db`.
- Eliminar la dependencia directa de los CommandHandlers hacia RabbitMQ.
- Introducir idempotencia en la capa de consumo.

**Non-Goals:**
- Modificar la estructura o contrato de los payloads de eventos existentes.
- Cambiar endpoints HTTP ni lógica de dominio.
- Construir ni modificar el `outbox-worker` (ya desplegado).
- Implementar exactly-once delivery (at-least-once + idempotencia en consumer es suficiente).
- Migrar `IMqProducerService.SendCommand()` (punto-a-punto) — solo se migran los `PublishEvent()`.

## Decisions

### D1 — Outbox write ocurre después del domain SaveChanges, en transacción separada

**Decisión**: El `IOutboxWriter.AppendAsync()` se llama después del `await _unitOfWork.SaveChangesAsync()`, sin participar en la misma transacción de base de datos.

**Rationale**: `outbox_db` y la DB de dominio del CRM son bases de datos distintas (PostgreSQL no admite transacciones distribuidas sin coordinador externo). Usar una transacción distribuida agregaría complejidad operacional desproporcionada al problema.

**Trade-off aceptado**: Existe una ventana pequeña donde el dominio commiteó pero el evento no se escribió en el outbox. Mitigación: si `AppendAsync` falla, el error se loguea como warning pero no se revierte el dominio. El evento puede ser reinsertado manualmente via replay del aggregate si es necesario.

**Alternativa descartada**: Usar `TransactionScope` con DTC — demasiada complejidad operacional y no es compatible con Npgsql en todos los escenarios.

### D2 — DeduplicationKey basada en `{EventType}:{AggregateId}`

**Decisión**: Cada `OutboxEvent` recibe un `DeduplicationKey = $"{eventType}:{aggregateId}"`.

**Rationale**: Previene duplicados si un CommandHandler se reintenta (ej: por un retry de la capa HTTP o MediatR pipeline). El UNIQUE constraint en `outbox_db.Events.DeduplicationKey` hace el insert idempotente sin lógica adicional.

**Limitación**: Si el mismo aggregate puede emitir el mismo tipo de evento más de una vez en el ciclo de vida normal (ej: múltiples `CustomerUpdated`), la clave debe incluir un timestamp o correlationId. Para los eventos actuales del CRM esto no aplica, pero debe revisarse si se añaden eventos de actualización frecuente.

### D3 — `IMqProducerService` se elimina de todos los CommandHandlers; tanto `PublishEvent()` como `SendCommand()` migran al outbox

**Decisión**: Con `SmartCore.Outbox` v1.1.0, la misma interfaz `IOutboxWriter.AppendAsync()` cubre tanto eventos broadcast como comandos punto-a-punto. El routing (exchange vs. queue) lo decide el `outbox-worker` según el `RouteType` configurado para cada `EventType`. Por tanto, también se migran todos los `SendCommand()` al outbox.

**Rationale**: `SmartCore.Outbox` v1.1.0 soporta `RouteType: "Command"` en la configuración del worker, enrutando el mensaje directamente a una queue. Migrar `SendCommand` al outbox añade durabilidad y trazabilidad también a los comandos punto-a-punto, eliminando la dependencia directa de los handlers hacia RabbitMQ de forma completa.

**Implicación**: `IMqProducerService` deja de ser invocado en los CommandHandlers. Puede deprecarse globalmente en un cambio posterior.

### D4 — ServiceName fijo = "crm" inyectado en la configuración

**Decisión**: El `ServiceName` se configura como literal `"crm"` en `AddSmartOutbox(options => { options.ServiceName = "crm"; })`.

**Rationale**: Permite al `outbox-worker` y a los operadores filtrar eventos por servicio origen en `outbox_db`. Es un valor estático por servicio, no tiene sentido derivarlo dinámicamente.

### D5 — Registro de `IOutboxWriter` en `Crm.Infrastructure`, no en `Crm.WebApi`

**Decisión**: `AddSmartOutbox(...)` se llama en el módulo de extensiones de `Crm.Infrastructure` (junto con EF Core, repositorios, etc.).

**Rationale**: Mantiene `Crm.WebApi` como capa de composición delgada. La dependencia en `SmartCore.Outbox` es de infraestructura, no de API.

## Risks / Trade-offs

**[Riesgo] Ventana de pérdida de evento entre domain commit y outbox write**
→ Mitigación: loguear el error con nivel Warning incluyendo el AggregateId y EventType. Implementar un mecanismo de replay por aggregate si el volumen de pérdidas justifica automatizarlo (fuera de scope de este cambio).

**[Riesgo] `outbox_db` caído bloquea la escritura del evento**
→ Mitigación: el dominio ya commiteó, así que el estado de negocio es consistente. El error se loguea y el request HTTP retorna éxito. El CRM no depende de `outbox_db` para su operación principal.

**[Riesgo] DeduplicationKey colisiona para eventos de actualización**
→ Mitigación: para `CustomerUpdated` y `CustomerFinancialsUpdated`, la clave debe incluir un timestamp (`{EventType}:{AggregateId}:{OccurredAt:yyyyMMddHHmmssfff}`). Verificar durante implementación.

**[Riesgo] outbox-worker no tiene configurado el EventType del CRM**
→ Mitigación: si el worker no tiene publisher para un `EventType`, el evento queda en `Status = Skipped` (nunca se pierde). Coordinar con el equipo de infra para agregar los EventTypes a la config del worker antes o justo después del deploy.

## Migration Plan

1. Instalar `SmartCore.Outbox` NuGet en `Crm.Infrastructure`.
2. Agregar `Outbox:ConnectionString` en `appsettings.Development.json` apuntando a `outbox_db` local.
3. Agregar `AddSmartOutbox(...)` en el módulo de infraestructura — esto aplica las migraciones de `outbox_db` en startup.
4. Migrar `CreateCustomerCommand` → reemplazar `PublishEvent` por `AppendAsync`.
5. Migrar `CreateProspectCommand`.
6. Migrar `UpdateCustomerFinancialsCommand`.
7. Migrar `ConvertProspectToCustomerCommand`.
8. Verificar que `IMqProducerService.PublishEvent()` ya no es llamado en ningún handler migrado.
9. Coordinar con infra: agregar `CustomerCreated`, `ProspectCreated`, `CustomerConverted`, `CustomerFinancialsUpdated` a la config del `outbox-worker`.
10. Deploy en staging → validar que el `outbox-worker` recoge los eventos y los publica a Rabbit.
11. Deploy en producción.

**Rollback**: Revertir los CommandHandlers a usar `IMqProducerService.PublishEvent()` directamente. No hay cambios de schema en la DB de dominio del CRM, así que el rollback es solo código.

## Open Questions

- ¿La `DeduplicationKey` para `CustomerUpdated` y `CustomerFinancialsUpdated` debe incluir timestamp para soportar múltiples actualizaciones del mismo aggregate? → Verificar durante implementación.
- ¿El `outbox-worker` ya tiene configurado el connection string de `outbox_db` del ambiente de staging/producción? → Coordinar con infra antes del deploy.
- ¿Se quiere mantener `IMqProducerService` a largo plazo o se planea deprecarlo completamente una vez todos los servicios migren? → Decisión de arquitectura futura, fuera de scope.
