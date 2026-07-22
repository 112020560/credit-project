## Why

El análisis DDD del sistema identificó 4 problemas de integridad estructural de prioridad alta que comprometen la pureza del dominio, introducen dependencias de infraestructura en la capa Domain, y dejan la integración con el CRM externo sin una capa de anti-corrupción — generando bugs, acoplamiento frágil y riesgo de mantenimiento. El sistema obtuvo una puntuación de 6.5/10 en el análisis DDD; estas correcciones son prerequisito para cualquier otra mejora del modelo.

## What Changes

- **BREAKING** Eliminar el constructor `LoanContractAggregate(IEnumerable<IDomainEvent>)` que aplica eventos dos veces por un bug en el chaining de constructores, corrompiendo el estado al rehidratar
- Remover la dependencia `ILogger<ContractEngine>` de `ContractEngine` en el Domain layer; el logging se mueve al handler de Application
- Mover las clases de entidad `OutboxMessage`, `WebhookSubscription`, `WebhookDelivery` y sus enums/constantes relacionados de `CreditSystem.Domain/Entities/` a `CreditSystem.Infrastructure/Persistence/Entities/`; las interfaces de repositorio permanecen en `Domain/Abstractions/Persistence/`
- Introducir Anti-Corruption Layer para la sincronización de clientes CRM: los consumers `CustomerCreatedConsumer` y `CustomerUpdatedConsumer` se convierten en adaptadores delgados que despachan un command `SyncCustomerFromCrm` via MediatR; la lógica de upsert se mueve a un handler en Application y un repositorio en Infrastructure

## Capabilities

### New Capabilities

- `customer-crm-sync`: Sincronización de perfiles de cliente desde el CRM externo mediante el command `SyncCustomerFromCrm`, con repositorio `ICustomerReferenceRepository` como puerto de dominio e implementación en Infrastructure

### Modified Capabilities

- `loan-contract`: El constructor de rehidratación cambia de firma — cualquier código que use `new LoanContractAggregate(IEnumerable<IDomainEvent>)` debe migrarse a `new LoanContractAggregate(null, events)`

## Impact

- `CreditSystem.Domain` — `LoanContractAggregate.cs` (constructor eliminado), `ContractEngine.cs` (ILogger removido), `Entities/OutboxMessage.cs` y `Entities/WebhookSubscription.cs` (movidos), nueva interfaz `ICustomerReferenceRepository`
- `CreditSystem.Application` — nuevo command `SyncCustomerFromCrm` con handler; `CreateContractCommandHandler` asume el logging del motor de reglas
- `CreditSystem.Infrastructure` — `CustomerCreatedConsumer`, `CustomerUpdatedConsumer` (refactorizados a adaptadores); nuevo `CustomerReferenceRepository`; entidades Outbox/Webhook en nueva ruta `Persistence/Entities/`
- No hay cambios de esquema PostgreSQL en este spec
- Requiere tarea de validación para confirmar que no existen callers externos del constructor eliminado antes de proceder
