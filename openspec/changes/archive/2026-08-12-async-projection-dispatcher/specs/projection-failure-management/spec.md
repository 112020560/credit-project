# Spec: Projection Failure Management

Visibilidad operacional de fallos de proyección con capacidad de resolución manual y reconstrucción completa de read models desde el event store.

---

## ADDED Requirements

### Requirement: Registro de fallos de proyección

El sistema SHALL registrar cada fallo permanente de proyección en la tabla `projection_failures` con información suficiente para diagnóstico y recuperación.

Columnas:
- `id UUID PK DEFAULT gen_random_uuid()`
- `event_id UUID NOT NULL REFERENCES stored_events(id)`
- `stream_id UUID NOT NULL`
- `event_type VARCHAR(100) NOT NULL`
- `projector_name VARCHAR(100) NOT NULL`
- `error_message TEXT NOT NULL`
- `occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `attempts INT NOT NULL DEFAULT 1`
- `resolved BOOLEAN NOT NULL DEFAULT false`
- `resolved_at TIMESTAMPTZ`

#### Scenario: Fallo registrado correctamente
- **WHEN** un proyector falla permanentemente al procesar un evento
- **THEN** `projection_failures` contiene una fila con `event_id`, `stream_id`, `event_type`, `projector_name`, `error_message` y `resolved = false`
- **THEN** `attempts` refleja el número total de intentos realizados (máximo 4)

#### Scenario: El mismo evento falla en múltiples proyectores
- **WHEN** un evento es rechazado permanentemente por dos proyectores distintos
- **THEN** `projection_failures` contiene dos filas separadas, una por proyector

---

### Requirement: Listado de fallos via endpoint admin

El sistema SHALL exponer `GET /api/admin/projection-failures` que retorna los fallos de proyección no resueltos.

Parámetros de query:
- `resolved=true`: incluir también los fallos ya resueltos (default: solo no resueltos)

Respuesta por fallo: `id`, `eventId`, `streamId`, `eventType`, `projectorName`, `errorMessage`, `occurredAt`, `attempts`, `resolved`, `resolvedAt`.

#### Scenario: Sin fallos pendientes
- **WHEN** `GET /api/admin/projection-failures` y no hay fallos con `resolved = false`
- **THEN** retorna `200` con array vacío

#### Scenario: Fallos pendientes
- **WHEN** existen filas con `resolved = false`
- **THEN** retorna `200` con lista de fallos, ordenados por `occurred_at DESC`

#### Scenario: Historial completo
- **WHEN** `GET /api/admin/projection-failures?resolved=true`
- **THEN** retorna todos los fallos incluyendo los resueltos

---

### Requirement: Resolución manual de fallos

El sistema SHALL exponer `POST /api/admin/projection-failures/{id}/resolve` que marca un fallo como resuelto.

#### Scenario: Resolución exitosa
- **WHEN** `POST /api/admin/projection-failures/{id}/resolve` con un `id` válido y `resolved = false`
- **THEN** retorna `200` y el fallo queda con `resolved = true` y `resolved_at = NOW()`

#### Scenario: Fallo no encontrado
- **WHEN** `id` no existe en `projection_failures`
- **THEN** retorna `404`

#### Scenario: Ya estaba resuelto
- **WHEN** el fallo ya tiene `resolved = true`
- **THEN** retorna `200` (idempotente)

---

### Requirement: Reconstrucción completa de read models

El sistema SHALL exponer `POST /api/admin/projections/rebuild` que reconstruye todos los read models desde cero.

Comportamiento:
1. Pausa el `ProjectionDispatcherWorker`
2. Trunca todos los read models (`rm_loan_summaries`, `rm_payment_history`, `rm_delinquent_loans`, `rm_loan_portfolio`, `rm_revolving_credit_summaries`, `rm_payment_tracking`)
3. Resetea todos los checkpoints en `projection_checkpoints` a `last_sequence = 0`
4. Marca todos los fallos en `projection_failures` como resueltos
5. Reanuda el worker — este reprocesará todos los eventos desde el inicio

#### Scenario: Rebuild exitoso
- **WHEN** `POST /api/admin/projections/rebuild`
- **THEN** retorna `202 Accepted` inmediatamente (el rebuild es asíncrono vía el worker)
- **THEN** los checkpoints quedan en 0
- **THEN** los fallos quedan resueltos

---

### Requirement: Health check de proyecciones

El sistema SHALL exponer el estado de las proyecciones en `/health` bajo la clave `projection-health`.

Estados:
- `Healthy`: no hay fallos no resueltos en los últimos 30 minutos
- `Degraded`: hay al menos un fallo no resuelto ocurrido en los últimos 30 minutos
- `Unhealthy`: no aplica para este check (los fallos de proyección no bloquean el sistema)

#### Scenario: Sistema sin fallos recientes
- **WHEN** `GET /health` y no hay `projection_failures` con `resolved = false` y `occurred_at > NOW() - 30 minutes`
- **THEN** `projection-health` aparece como `Healthy`

#### Scenario: Sistema con fallos recientes
- **WHEN** existe al menos un fallo no resuelto ocurrido en los últimos 30 minutos
- **THEN** `projection-health` aparece como `Degraded`
- **THEN** el detalle incluye el conteo de fallos recientes
