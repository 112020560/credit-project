## Context

El sistema usa Event Sourcing: los eventos son la fuente de verdad en `stored_events` y los read models (`rm_loan_summaries`, `rm_payment_history`, etc.) son derivados. Actualmente los command handlers proyectan eventos síncronamente dentro del mismo request HTTP, usando `IProjectionEngine.ProjectEventsAsync()`. Si la proyección falla, el error es capturado silenciosamente y el handler retorna `201` — dejando el sistema en estado inconsistente sin visibilidad operacional.

Bug activo identificado: `interest_rate` en `rm_loan_summaries` es `NUMERIC(5,4)` (máximo 9.9999%), lo que causa desbordamiento con tasas reales (16.5%). Este es el primer fallo concreto de una fragilidad estructural.

## Goals / Non-Goals

**Goals:**
- Desacoplar la proyección del request HTTP — los read models son eventualmente consistentes
- Garantizar que los fallos de proyección sean visibles, rastreables y recuperables
- Permitir reconstruir read models desde el event store en cualquier momento
- Corregir el bug de `NUMERIC(5,4)` en `interest_rate`
- Eliminar el `catch` que oculta fallos en handlers

**Non-Goals:**
- Garantía de consistencia inmediata (los read models pueden estar segundos atrás)
- Cambiar el esquema de los read models existentes (solo corrección de tipo de columna)
- Reemplazar el `OutboxPublisherWorker` de RabbitMQ (son concerns distintos)
- Procesamiento paralelo de eventos de diferentes streams

## Decisions

### D1: Worker basado en polling de `stored_events`, no en outbox de mensajes

**Elegido:** `ProjectionDispatcherWorker` hace polling directamente sobre `stored_events` usando un número de secuencia (`id` autoincremental o columna `sequence`).

**Alternativa descartada:** Usar el `event_outbox` como fuente. El outbox está diseñado para publicación externa (RabbitMQ). Reutilizarlo para proyecciones crearía acoplamiento entre dos responsabilidades distintas y complicaría el contrato del outbox.

**Rationale:** `stored_events` es la fuente canónica. El worker mantiene un checkpoint (`last_sequence`) por proyector en `projection_checkpoints`. En cada tick, lee eventos con `sequence > last_checkpoint` ordenados por secuencia y los procesa uno a uno.

> **Nota de implementación:** Verificar si `stored_events` ya tiene columna `sequence` (autoincremental) o si se debe usar `stored_at` como proxy de orden. Usar la columna existente; si no existe, agregar `sequence BIGSERIAL` en la migración.

### D2: Checkpoint por proyector, no global

**Elegido:** Cada proyector tiene su propio checkpoint en `projection_checkpoints(projector_name PK, last_sequence BIGINT)`.

**Alternativa descartada:** Checkpoint global (todos los proyectores avanzan juntos). Si un proyector falla, bloquearía el progreso de todos los demás.

**Rationale:** Si `LoanSummaryProjector` falla en el evento N, `PaymentHistoryProjector` puede seguir procesando eventos N+1, N+2, etc. Los checkpoints independientes maximizan la superficie de recuperación.

### D3: Reintentos en memoria con backoff, fallo permanente registrado en BD

**Elegido:** El worker reintenta cada evento fallido hasta 3 veces con backoff exponencial (1s, 2s, 4s) antes de marcar el fallo como permanente en `projection_failures`. Luego avanza el checkpoint y continúa.

**Alternativa descartada:** No avanzar el checkpoint hasta que el evento se proyecte exitosamente (bloqueo). Esto causaría que un evento con error permanente (ej. bug en el código) bloquee toda la proyección indefinidamente.

**Rationale:** Es preferible tener un read model con un gap conocido (visible en `projection_failures`) que un sistema completamente bloqueado. El operador puede corregir el bug y ejecutar `POST /admin/projections/rebuild` para recuperarse.

### D4: Rebuild resetea checkpoints a 0

**Elegido:** `POST /api/admin/projections/rebuild` trunca los read models y resetea todos los checkpoints a 0, forzando al worker a reprocesar todos los eventos desde el inicio.

**Alternativa descartada:** Rebuild selectivo por proyector. Más complejo, y la operación completa es idempotente (el worker es determinista sobre el event store).

**Rationale:** Los read models son siempre reconstruibles. El rebuild es una operación de mantenimiento poco frecuente. La simplicidad de "borrar todo y volver a procesar" es correcta aquí.

### D5: Eliminar IProjectionEngine de command handlers

**Elegido:** Remover toda llamada a `_projectionEngine.ProjectEventsAsync()` de los handlers. El `201` significa "evento persistido", no "read model actualizado".

**Alternativa descartada:** Mantener proyección síncrona como "best effort" en paralelo con el worker. Causaría proyección doble y race conditions.

**Rationale:** Semántica clara: el handler escribe al event store. El worker proyecta. Sin mezcla de responsabilidades.

## Risks / Trade-offs

- **[Riesgo] Read model desactualizado en tests de integración** → Mitigación: En tests E2E, llamar al worker explícitamente después de guardar eventos, o usar un modo síncrono configurable en tests.

- **[Riesgo] El worker procesa eventos mientras se hace deploy** → Mitigación: El worker usa `CancellationToken` del host; un graceful shutdown espera a que termine el tick actual.

- **[Riesgo] `stored_events` no tiene columna de secuencia autoincremental** → Mitigación: Verificar esquema; si usa `stored_at TIMESTAMPTZ`, puede usarse como proxy de orden (con riesgo de colisión en inserciones simultáneas). Lo ideal es `sequence BIGSERIAL`. Revisar en la fase de implementación.

- **[Trade-off] Consistencia eventual vs. inmediata** → Los clientes que hacen `POST /loans` y luego `GET /loans/{id}/summary` inmediatamente pueden ver el resumen vacío por algunos segundos. Documentar en respuesta del API.

## Migration Plan

1. Aplicar migración SQL (corrección de tipo + nuevas tablas)
2. Hacer deploy del worker desactivado (feature flag o simplemente sin registro en DI hasta que esté listo)
3. Eliminar `ProjectEventsAsync` de handlers en el mismo deploy
4. Activar el worker
5. Ejecutar `POST /admin/projections/rebuild` una vez para proyectar eventos existentes que el handler ya no va a proyectar

## Open Questions

- ¿`stored_events` tiene columna `sequence BIGSERIAL` o se ordena por `stored_at`? → Revisar antes de implementar el worker.
- ¿Cuántos handlers además de `CreateContractCommandHandler` usan `IProjectionEngine`? → Auditar antes de eliminar la interfaz del DI.
- ¿El intervalo de 5 segundos es aceptable para el negocio? → Configurable vía `appsettings.json`.
