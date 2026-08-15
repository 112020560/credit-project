## 1. Migración de base de datos

- [x] 1.1 Crear `Src/Core/CreditSystem.Infrastructure/Migrations/20260812_AsyncProjectionDispatcher.sql`:
  - `ALTER TABLE rm_loan_summaries ALTER COLUMN interest_rate TYPE NUMERIC(8,4)` — corrige el overflow con tasas > 9.9999%
  - `ALTER TABLE stored_events ADD COLUMN IF NOT EXISTS sequence BIGSERIAL` — columna de orden para el worker
  - `CREATE INDEX IF NOT EXISTS idx_stored_events_sequence ON stored_events(sequence ASC)`
  - `CREATE TABLE projection_checkpoints (projector_name VARCHAR(100) PRIMARY KEY, last_sequence BIGINT NOT NULL DEFAULT 0, updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW())`
  - `CREATE TABLE projection_failures (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), event_id UUID NOT NULL REFERENCES stored_events(id), stream_id UUID NOT NULL, event_type VARCHAR(100) NOT NULL, projector_name VARCHAR(100) NOT NULL, error_message TEXT NOT NULL, occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), attempts INT NOT NULL DEFAULT 1, resolved BOOLEAN NOT NULL DEFAULT false, resolved_at TIMESTAMPTZ)`
  - `CREATE INDEX idx_projection_failures_unresolved ON projection_failures(occurred_at DESC) WHERE resolved = false`

## 2. Repositorios de infraestructura

- [x] 2.1 Crear interfaz `IProjectionCheckpointRepository` en `Src/Core/CreditSystem.Application/Interfaces/`:
  - `GetCheckpointAsync(string projectorName, CancellationToken ct) → Task<long>`
  - `SaveCheckpointAsync(string projectorName, long sequence, CancellationToken ct) → Task`
- [x] 2.2 Implementar `ProjectionCheckpointRepository` en `Src/Core/CreditSystem.Infrastructure/Repositories/` con Dapper (UPSERT en `projection_checkpoints`)
- [x] 2.3 Crear interfaz `IProjectionFailureRepository` en `Src/Core/CreditSystem.Application/Interfaces/`:
  - `RecordFailureAsync(Guid eventId, Guid streamId, string eventType, string projectorName, string errorMessage, int attempts, CancellationToken ct) → Task`
  - `ResolveAsync(Guid id, CancellationToken ct) → Task<bool>`
  - `GetUnresolvedAsync(CancellationToken ct) → Task<IEnumerable<ProjectionFailure>>`
  - `GetAllAsync(CancellationToken ct) → Task<IEnumerable<ProjectionFailure>>`
- [x] 2.4 Crear record/clase `ProjectionFailure` en `Src/Core/CreditSystem.Domain/Models/` con todos los campos de la tabla
- [x] 2.5 Implementar `ProjectionFailureRepository` en `Src/Core/CreditSystem.Infrastructure/Repositories/` con Dapper
- [x] 2.6 Registrar ambos repositorios como `AddScoped` en `DependencyInjection.cs`

## 3. Leer eventos por secuencia desde el Event Store

- [x] 3.1 Agregar método `GetEventsSinceSequenceAsync(long fromSequence, int batchSize, CancellationToken ct) → Task<IEnumerable<StoredEventRow>>` a `IEventStore` / `PostgresEventStore`
  - `StoredEventRow` expone: `Guid Id`, `Guid StreamId`, `string EventType`, `string EventData`, `long Sequence`, `DateTime StoredAt`
  - SELECT `id, stream_id, event_type, event_data, sequence, stored_at FROM stored_events WHERE sequence > @FromSequence ORDER BY sequence ASC LIMIT @BatchSize`

## 4. ProjectionDispatcherWorker

- [x] 4.1 Crear `ProjectionDispatcherWorker : BackgroundService` en `Src/Core/CreditSystem.Infrastructure/Workers/`
  - Constructor recibe: `IEnumerable<IProjection> projectors`, `IEventStore eventStore`, `IProjectionCheckpointRepository checkpoints`, `IProjectionFailureRepository failures`, `IOptions<ProjectionDispatcherOptions> options`, `ILogger<ProjectionDispatcherWorker>`
  - `ExecuteAsync`: loop con `PeriodicTimer` usando `IntervalSeconds` de la config
- [x] 4.2 Implementar el tick del worker:
  - Para cada proyector: obtener `lastSequence` del checkpoint
  - Consultar `GetEventsSinceSequenceAsync(lastSequence, batchSize: 100)`
  - Para cada evento: deserializar y llamar `projector.ProjectAsync(domainEvent)` con reintentos
- [x] 4.3 Implementar política de reintentos (retries: 3, backoff: 1s/2s/4s):
  - Si pasa en algún intento: actualizar checkpoint con el `sequence` del evento
  - Si falla en todos: llamar `failures.RecordFailureAsync(...)`, avanzar checkpoint igualmente
- [x] 4.4 Crear `ProjectionDispatcherOptions` (POCO) con `IntervalSeconds = 5` y `BatchSize = 100`
- [x] 4.5 Registrar en `DependencyInjection.cs`:
  - `services.AddHostedService<ProjectionDispatcherWorker>()`
  - `services.Configure<ProjectionDispatcherOptions>(config.GetSection("ProjectionDispatcher"))`

## 5. Eliminar proyección síncrona de handlers

- [x] 5.1 Auditar qué handlers inyectan `IProjectionEngine` (buscar en `Src/Core/CreditSystem.Application/`)
- [x] 5.2 Eliminar `IProjectionEngine` de `CreateContractCommandHandler`: remover inyección en constructor, campo privado, y llamada a `ProjectEventsAsync`
- [x] 5.3 Eliminar `IProjectionEngine` de cualquier otro handler identificado en 5.1
- [x] 5.4 Verificar que `IProjectionEngine` siga registrado en DI (lo usa el worker a través de los proyectores individuales; si ya no se usa directamente, evaluar si eliminar la abstracción)

## 6. API Admin — fallos y rebuild

- [x] 6.1 Crear `ProjectionAdminEndpoints.cs` en `Src/Core/CreditSystem.Api/Endpoints/` con:
  - `GET /api/admin/projection-failures` — query param `resolved=false` (default), retorna lista de `ProjectionFailureResponse`
  - `POST /api/admin/projection-failures/{id}/resolve` — llama `IProjectionFailureRepository.ResolveAsync`, retorna 200/404
  - `POST /api/admin/projections/rebuild` — resetea checkpoints a 0, trunca read models, retorna 202
- [x] 6.2 Crear `ProjectionFailureResponse` record con todos los campos de `ProjectionFailure`
- [x] 6.3 Implementar el endpoint `rebuild`: truncar las tablas de read models y hacer UPSERT de checkpoints a `last_sequence = 0` en una transacción
- [x] 6.4 Registrar `app.MapProjectionAdminEndpoints()` en `Program.cs`

## 7. Health Check

- [x] 7.1 Crear `ProjectionHealthCheck : IHealthCheck` en `Src/Core/CreditSystem.Infrastructure/HealthChecks/`
  - Consulta `projection_failures` donde `resolved = false AND occurred_at > NOW() - INTERVAL '30 minutes'`
  - Si count > 0 → `Degraded` con descripción "N unresolved projection failures in the last 30 minutes"
  - Si count = 0 → `Healthy`
- [x] 7.2 Registrar en `DependencyInjection.cs`: `services.AddHealthChecks().AddCheck<ProjectionHealthCheck>("projection-health")`

## 8. Tests

- [x] 8.1 Crear `ProjectionDispatcherWorkerTests.cs` en `Src/Core/CreditSystem.Tests/Infrastructure/Workers/`:
  - Test: evento exitoso avanza checkpoint
  - Test: fallo transitorio (1er intento falla, 2do pasa) no registra en `projection_failures`
  - Test: fallo permanente (4 intentos) registra en `projection_failures` y avanza checkpoint
  - Test: un proyector fallando no bloquea el siguiente evento
- [x] 8.2 Actualizar `CreateContractCommandHandlerTests.cs`: remover mock de `IProjectionEngine` del `BuildHandler()`
- [x] 8.3 Crear `ProjectionHealthCheckTests.cs` en `Src/Core/CreditSystem.Tests/Infrastructure/HealthChecks/`:
  - Test: sin fallos recientes → Healthy
  - Test: con fallos recientes → Degraded
