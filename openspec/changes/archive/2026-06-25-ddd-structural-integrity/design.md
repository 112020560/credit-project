## Context

Stack: .NET 9, Clean Architecture, DDD + Event Sourcing, Dapper + Npgsql (PostgreSQL), MediatR, MassTransit + RabbitMQ.

El análisis DDD identificó 4 problemas estructurales en el código actual:

1. `LoanContractAggregate` tiene un constructor secundario `(IEnumerable<IDomainEvent>)` que hace chaining a `this(null, events)` — que ya aplica todos los eventos — y luego sobreescribe `State = Initial` y vuelve a iterar los eventos, resultando en doble aplicación.
2. `ContractEngine` (Domain layer) recibe `ILogger<ContractEngine>` en su constructor, acoplando el dominio a Microsoft.Extensions.Logging.
3. `OutboxMessage`, `WebhookSubscription`, `WebhookDelivery` y sus enums viven en `CreditSystem.Domain/Entities/` siendo concerns de entrega de infraestructura, no conceptos del dominio del crédito.
4. `CustomerCreatedConsumer` y `CustomerUpdatedConsumer` escriben SQL directo con Dapper sin ninguna abstracción ni capa de traducción del modelo CRM.

## Goals / Non-Goals

**Goals:**
- Eliminar el constructor buggy de `LoanContractAggregate` garantizando que no haya regresión
- Mantener `ContractEngine` en el domain layer pero sin dependencias de infraestructura
- Limpiar el Domain layer de entidades que son concerns de infraestructura
- Introducir ACL entre el contrato CRM y el modelo interno del Credit System

**Non-Goals:**
- Renombrar `CustomerReference` → `CustomerCreditProfile` (spec separado)
- Cambiar el namespace de `DomainEvent` base (spec separado)
- Agregar nuevos campos al perfil de cliente
- Cambiar el esquema PostgreSQL de `customer_references`

## Decisions

### Decision 1: Eliminar constructor y agregar tarea de validación previa

El constructor `LoanContractAggregate(IEnumerable<IDomainEvent> events)` se elimina. El constructor canónico `(LoanContractState? snapshot, IEnumerable<IDomainEvent> events)` cubre ambos casos: pasar `null` como snapshot equivale a rehidratación completa.

**Alternativa descartada**: mantener el constructor pero corregirlo para no hacer chaining. Descartada porque tener dos constructores públicos con diferente semántica para el mismo caso de uso (rehidratación) es confuso. Uno es suficiente.

**Tarea de validación previa**: antes de eliminar el constructor, se agrega una tarea de búsqueda en toda la solución para confirmar que ningún código externo lo usa. Los callers conocidos son solo el repositorio `LoanContractRepository`, que ya usa el constructor canónico.

### Decision 2: Logging de ContractEngine se mueve al handler de Application

`ContractEngine.EvaluateAsync()` pierde el `ILogger`. El handler `CreateContractCommandHandler` que invoca el engine recibe el logger (ya tiene acceso via MediatR / DI) y loguea el `ContractEvaluationResponse` resultante.

**Alternativa descartada**: pasar un callback de logging como `Action<string>` al engine. Descartada porque añade complejidad sin beneficio real — el handler ya tiene toda la información del resultado y puede loguearlo directamente.

**Alternativa descartada**: extraer el engine a Application layer. Descartada porque el motor de reglas y las reglas son conceptos puros del dominio del crédito (`IContractRule`, `IHardStopRule`, `CreditScoreRule`, etc.) y deben permanecer en Domain.

### Decision 3: Entidades de infraestructura se mueven a Infrastructure/Persistence/Entities/

`OutboxMessage.cs` y `WebhookSubscription.cs` (que incluye `WebhookDelivery`, `OutboxMessageStatus`, `WebhookDeliveryStatus`, `WebhookEventTypes`) se mueven a `CreditSystem.Infrastructure/Persistence/Entities/`.

Las interfaces de repositorio (`IOutboxRepository`, `IWebhookSubscriptionRepository`, `IWebhookDeliveryRepository`, `IPaymentTrackingRepository`) permanecen en `Domain/Abstractions/Persistence/` como puertos — el dominio necesita poder declarar que "quiere publicar en el outbox" sin saber cómo está implementado.

**Alternativa descartada**: mover también las interfaces a Infrastructure. Descartada porque las interfaces son contratos (puertos) que el dominio necesita conocer para sus abstracciones — el patrón Ports & Adapters requiere que el puerto esté en el lado del dominio.

**Impacto de namespaces**: todos los `using CreditSystem.Domain.Entities;` en Infrastructure workers y repositorios que referencien estas clases deben actualizarse a `using CreditSystem.Infrastructure.Persistence.Entities;`.

### Decision 4: ACL via command SyncCustomerFromCrm

Los consumers CRM se convierten en adaptadores: su única responsabilidad es traducir el mensaje de integración a un command interno y despacharlo via MediatR. Todo el conocimiento de la nomenclatura CRM queda confinado en el consumer (el adaptador).

**Flujo resultante**:
```
RabbitMQ → CustomerCreatedConsumer (ACL/Adapter)
                → SyncCustomerFromCrmCommand
                    → SyncCustomerFromCrmCommandHandler (Application)
                        → ICustomerReferenceRepository.UpsertAsync()
                            → CustomerReferenceRepository (Infrastructure/Dapper)
                                → PostgreSQL customer_references
```

**Command `SyncCustomerFromCrmCommand`**: contiene los campos ya traducidos al modelo interno (`DocumentType`, `DocumentNumber`, `ExternalId`), no los nombres del CRM (`IdentificationType`, `IdentificationNumber`, `CustomerId`). La traducción ocurre en el consumer.

**`ICustomerReferenceRepository`** expone un único método `UpsertAsync(CustomerReferenceUpsertData data, CancellationToken ct)`. El tipo `CustomerReferenceUpsertData` es un simple record en Application o Domain que encapsula los datos del upsert.

**Alternativa descartada**: dejar los consumers con acceso a repositorio pero sin SQL directo. Descartada porque los consumers son Infrastructure y no deben tomar decisiones de Application. El command/handler pattern es más consistente con el resto de la arquitectura CQRS.

**Alternativa descartada**: usar un Domain Service para la sincronización. Descartada porque la sincronización de datos externos no es lógica de dominio puro — es orquestación de Application.

## Risks / Trade-offs

- **Ruptura del constructor eliminado** → Mitigation: tarea de validación previa con búsqueda en toda la solución (`grep`/IDE) antes de eliminar. Si se encuentra algún caller, se migra primero y se valida que compila.
- **Cambio de namespace en entidades Outbox/Webhook** → Mitigation: los archivos de Infrastructure que las usan están contenidos dentro de `CreditSystem.Infrastructure` — el impacto es localizado y verificable con `dotnet build`.
- **Doble consumer para Create y Update CRM** → Trade-off aceptado: ambos consumers despachan `SyncCustomerFromCrmCommand` con los mismos campos. Si en el futuro se necesita diferenciar semánticamente un "crear" de un "actualizar", se pueden separar en dos commands distintos.
- **Sin transaction entre MassTransit y MediatR** → Si el handler falla, MassTransit reintenta el mensaje completo. El upsert en `CustomerReferenceRepository` es idempotente (ON CONFLICT DO UPDATE), por lo que los reintentos son seguros.

## Migration Plan

1. Tarea de validación: buscar callers del constructor `LoanContractAggregate(IEnumerable<IDomainEvent>)` en toda la solución
2. Si hay callers: migrarlos a `(null, events)` y confirmar compilación
3. Eliminar el constructor buggy
4. Remover `ILogger` de `ContractEngine`; actualizar registro en DI
5. Mover archivos de entidades Outbox/Webhook; actualizar namespaces; confirmar `dotnet build`
6. Crear `ICustomerReferenceRepository`, `CustomerReferenceRepository`, `SyncCustomerFromCrmCommand`, handler
7. Refactorizar consumers; registrar nuevas dependencias en DI
8. Ejecutar `dotnet build` y `dotnet test` para validar integridad

## Open Questions

- ¿El `CustomerUpdatedConsumer` tiene campos adicionales respecto al `CustomerCreatedConsumer` que deben mapear diferente? (revisar implementación actual antes de unificar en un solo command)
- ¿Se deben agregar tests unitarios para `SyncCustomerFromCrmCommandHandler` en este spec o en un spec de tests separado?
