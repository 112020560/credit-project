# Spec: Projection Dispatcher

Worker asíncrono que mantiene los read models sincronizados con el event store, procesando eventos con checkpoint por proyector, reintentos con backoff y registro auditable de fallos permanentes.

---

## Requirements

### Requirement: Procesamiento asíncrono de eventos hacia proyectores

El sistema SHALL tener un `ProjectionDispatcherWorker` (BackgroundService) que en cada tick:
1. Para cada proyector registrado, lea el checkpoint actual (`last_sequence`) desde `projection_checkpoints`
2. Consulte `stored_events` filtrando `sequence > last_sequence` ORDER BY `sequence ASC`
3. Por cada evento, llame al proyector correspondiente
4. Si el proyector pasa: actualice el checkpoint al `sequence` del evento procesado
5. Si el proyector falla: ejecute la política de reintentos (ver Requirement: Política de reintentos con backoff exponencial)

El worker SHALL continuar procesando el siguiente evento aunque un proyector falle, nunca lanzar una excepción no capturada, y respetar el `CancellationToken` del host para graceful shutdown.

#### Scenario: Eventos nuevos disponibles
- **WHEN** existen eventos en `stored_events` con `sequence > last_checkpoint` del proyector
- **THEN** el worker los procesa en orden ascendente de `sequence`
- **THEN** el checkpoint del proyector avanza por cada evento exitoso

#### Scenario: Sin eventos nuevos
- **WHEN** no hay eventos con `sequence > last_checkpoint`
- **THEN** el worker espera el intervalo configurado (default 5 segundos) y vuelve a consultar

#### Scenario: Proyector no maneja el tipo de evento
- **WHEN** el evento es de un tipo que el proyector no reconoce (ej. `PaymentApplied` para un proyector que solo maneja `ContractCreated`)
- **THEN** el proyector retorna sin error y el checkpoint avanza normalmente

#### Scenario: Fallo del proyector
- **WHEN** un proyector lanza excepción al procesar un evento
- **THEN** el worker ejecuta la política de reintentos (ver Requirement: Política de reintentos con backoff exponencial)
- **THEN** el worker continúa con el siguiente evento sin detenerse

#### Scenario: Graceful shutdown
- **WHEN** el host solicita detención vía CancellationToken
- **THEN** el worker completa el tick actual y detiene el loop sin forzar la terminación de proyecciones en curso

---

### Requirement: Política de reintentos con backoff exponencial

El sistema SHALL reintentar la proyección de un evento fallido hasta 3 veces con backoff exponencial antes de marcarlo como fallo permanente.

Política:
- Intento 1 (inicial): inmediato
- Intento 2: esperar 1 segundo
- Intento 3: esperar 2 segundos
- Intento 4: esperar 4 segundos → si falla, registrar como fallo permanente en `projection_failures`

Después del fallo permanente, el checkpoint DEBE avanzar para no bloquear eventos subsiguientes.

#### Scenario: Fallo transitorio resuelto en reintento
- **WHEN** un proyector falla en el intento 1 pero pasa en el intento 2
- **THEN** el checkpoint avanza normalmente
- **THEN** no se registra fallo en `projection_failures`

#### Scenario: Fallo permanente después de 3 reintentos
- **WHEN** un proyector falla en los 4 intentos (inicial + 3 reintentos)
- **THEN** el sistema registra el fallo en `projection_failures` con `attempts = 4`
- **THEN** el checkpoint avanza al sequence del evento fallido
- **THEN** el worker continúa con el siguiente evento

---

### Requirement: Checkpoint por proyector

El sistema SHALL mantener un registro de progreso independiente por proyector en la tabla `projection_checkpoints`.

Columnas: `projector_name VARCHAR(100) PK`, `last_sequence BIGINT NOT NULL DEFAULT 0`, `updated_at TIMESTAMPTZ`.

Un proyector sin registro en `projection_checkpoints` SHALL tratarse como si tuviera `last_sequence = 0` (procesa desde el inicio).

#### Scenario: Primer arranque del worker
- **WHEN** `projection_checkpoints` no tiene registro para un proyector
- **THEN** el worker procesa todos los eventos desde `sequence = 1`

#### Scenario: Reinicio del worker
- **WHEN** el worker se reinicia después de procesar N eventos
- **THEN** retoma desde `last_sequence` guardado, sin reprocesar eventos anteriores

---

### Requirement: Intervalo configurable

El intervalo de polling SHALL ser configurable vía `appsettings.json` bajo la clave `ProjectionDispatcher:IntervalSeconds` con valor por defecto de 5 segundos.

#### Scenario: Configuración del intervalo
- **WHEN** se configura `ProjectionDispatcher:IntervalSeconds = 10`
- **THEN** el worker espera 10 segundos entre ticks cuando no hay eventos pendientes
