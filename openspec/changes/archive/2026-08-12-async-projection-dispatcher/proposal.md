## Why

El sistema proyecta read models de forma síncrona dentro del mismo request HTTP que persiste los eventos. Cuando la proyección falla, un `catch` la absorbe y retorna `201` al cliente aunque el read model nunca se creó — una mentira estructural que hace imposible detectar, rastrear o recuperarse de fallos de proyección. Adicionalmente existe un bug activo: la columna `interest_rate` en `rm_loan_summaries` es `NUMERIC(5,4)` (máximo 9.9999%) y desborda con cualquier tasa real (ej. 16.5%).

## What Changes

- **Eliminar** la proyección síncrona de todos los command handlers
- **Crear** `ProjectionDispatcherWorker`: background service que lee eventos desde `stored_events` usando checkpoints por proyector, llama a cada proyector, reintenta hasta 3 veces con backoff exponencial, y registra fallos permanentes en `projection_failures`
- **Crear** tabla `projection_checkpoints`: posición de cada proyector en el stream de eventos
- **Crear** tabla `projection_failures`: registro auditable de fallos con error, intentos, y estado de resolución
- **Crear** endpoints admin: listar fallos, resolver manualmente, reconstruir read models desde cero
- **Crear** `ProjectionHealthCheck`: `Degraded` si hay fallos no resueltos en los últimos 30 minutos
- **Corregir** `interest_rate` en `rm_loan_summaries`: `NUMERIC(5,4)` → `NUMERIC(8,4)`
- **BREAKING**: `IProjectionEngine` ya no se inyecta en command handlers; los read models son eventualmente consistentes

## Capabilities

### New Capabilities

- `projection-dispatcher`: Worker asíncrono que procesa eventos desde el event store hacia proyectores, con checkpoint tracking, reintentos, y registro de fallos
- `projection-failure-management`: Visibilidad operacional de fallos de proyección y capacidad de reconstruir read models desde cero

### Modified Capabilities

- `loan-contract`: El flujo de creación de contrato ya no garantiza que el read model exista al retornar `201`; el `201` indica comando aceptado y persistido, no proyección completa

## Impact

- `CreditSystem.Application`: Eliminar `IProjectionEngine` de `CreateContractCommandHandler` y cualquier otro handler que lo use
- `CreditSystem.Infrastructure`: Nuevos repositorios `ProjectionCheckpointRepository`, `ProjectionFailureRepository`; nuevo worker `ProjectionDispatcherWorker`
- `CreditSystem.Api`: Nuevos endpoints bajo `/api/admin/projections/` y `/api/admin/projection-failures/`
- Base de datos: 2 tablas nuevas, 1 corrección de tipo de columna
- Health checks: nueva entrada `projection-health` en `/health`
- Clientes de la API: deben entender que `POST /loans` retorna `201` con consistencia eventual en read models
