## Context

El sistema corre en .NET 9 con Dapper + Npgsql (sin EF Core), PostgreSQL como única base de datos, RabbitMQ via MassTransit, y BackgroundServices para workers periódicos. La Fase 3 agrega cuatro capacidades de infraestructura ortogonales que no tocan el dominio: distributed locking, idempotencia, healthchecks y audit trail.

## Goals / Non-Goals

**Goals:**
- Hacer los workers seguros para despliegues multi-instancia (HA / Kubernetes)
- Prevenir procesamiento duplicado de pagos en reintentos de red
- Exponer endpoints de salud compatibles con probes de Kubernetes
- Registrar trazabilidad de usuario para operaciones financieras

**Non-Goals:**
- Autenticación/autorización (Fase 1 pendiente — `X-User-Id` es solo un header de confianza por ahora)
- Distributed tracing (ya cubierto por OpenTelemetry en `SmartCore.Telemetry`)
- Lectura del audit log vía API (solo escritura en esta fase)
- Rate limiting

## Decisions

### D1: PostgreSQL advisory locks para distributed locking (en vez de Redis/Zookeeper)

**Decisión**: usar `pg_try_advisory_lock(lockId bigint)` y `pg_advisory_unlock(lockId bigint)` directamente sobre Npgsql.

**Rationale**: el sistema ya depende de PostgreSQL. Agregar Redis solo para locking introduce una dependencia operacional nueva, su propio failover y coste. Los advisory locks de Postgres son session-level, se liberan automáticamente si la conexión cae (crash del pod), y `pg_try_advisory_lock` es no-bloqueante — exactamente lo que necesitamos.

**Alternativas descartadas**: Redis + RedLock (dependencia extra), SemaphoreSlim (solo funciona in-process, no entre instancias).

**Implementación**: una clase `PostgresAdvisoryLock` con métodos `TryAcquireAsync` / `ReleaseAsync`. Cada worker tiene un `WorkerLockId` como constante `long`. El lock se adquiere con una conexión dedicada (fuera del scope del job) y se libera en el `finally` del try/catch del worker loop.

### D2: Tabla `idempotency_keys` con TTL de 24h

**Decisión**: almacenar `(idempotency_key, response_status, response_body, expires_at)` en una tabla Postgres. No usar cache en memoria.

**Rationale**: con múltiples instancias, el cache en memoria no comparte estado. Una tabla Postgres garantiza que cualquier instancia detecte la clave, consistente con el patrón de la base de datos como fuente de verdad del sistema.

**Lookup**: antes de procesar el pago, el endpoint hace un `SELECT` por `idempotency_key WHERE expires_at > NOW()`. Si existe, retorna el body almacenado con status 200 y header `Idempotency-Replayed: true`. Si no existe, procesa y hace `INSERT`.

**Limpieza**: la expiración se filtra en el SELECT (no se necesita job de limpieza inmediato; se puede agregar después un job de vacuuming).

### D3: HealthChecks con paquetes Microsoft estándar + NuGet de comunidad

**Decisión**: usar `Microsoft.Extensions.Diagnostics.HealthChecks` (built-in) + `AspNetCore.HealthChecks.NpgSql` + `AspNetCore.HealthChecks.RabbitMQ`.

**Rationale**: integración nativa con ASP.NET Core, output JSON estándar, soporte de Kubernetes probes out of the box.

**UnderwritingPolicy check**: un check personalizado (implementa `IHealthCheck`) que verifica `sp.GetService<UnderwritingPolicy>() != null`.

### D4: Audit log fire-and-forget con manejo de error silencioso

**Decisión**: el audit log se inserta después de la operación exitosa, y si falla, se loguea el error pero no se propaga al caller.

**Rationale**: el audit log es un concern de observabilidad secundario — una falla de log no debe impedir que el socio reciba confirmación de su pago. La operación financiera principal ya fue persisted en el event store (fuente de verdad). En caso de fallo del audit log, el event store tiene el registro completo de todos modos.

**`IAuditLogRepository`**: un método `LogAsync` con try/catch interno que absorbe excepciones. Se inyecta como Scoped en los handlers/endpoints que lo necesiten.

## Risks / Trade-offs

- **Advisory lock + crash de instancia**: si el pod muere entre `TryAcquire` y `Release`, Postgres libera automáticamente el session lock al cerrar la conexión TCP. No hay lock huérfano. ✓
- **Idempotency sin transacción distribuida**: existe una ventana de race condition si dos solicitudes con la misma key llegan simultáneamente al mismo millisegond. Mitigación: `ON CONFLICT DO NOTHING` en el INSERT + `UNIQUE` constraint en `idempotency_key` — el segundo INSERT pierde y relee la fila existente.
- **Audit log eventual**: en caso de fallo de DB post-pago, la operación queda en el event store pero no en audit_log. Riesgo bajo — el event store es el audit trail técnico definitivo; el audit_log es para consulta operacional.
- **Paquetes de healthcheck de comunidad**: `AspNetCore.HealthChecks.*` no son de Microsoft pero son ampliamente adoptados. Si se prefiere evitar la dependencia, se puede implementar el check de Postgres con Dapper directamente.

## Migration Plan

1. Ejecutar migración SQL: crear tablas `idempotency_keys` y `audit_log`
2. Agregar paquetes NuGet a `CreditSystem.Infrastructure` y `CreditSystem.Api`
3. Desplegar nueva versión — los workers existentes seguirán funcionando; el locking es aditivo (si una instancia no tiene la nueva versión, no compite por el lock)
4. Verificar endpoints `/health` y `/health/ready` desde el orquestador antes de cortar tráfico

## Open Questions

- ¿El sistema de autenticación externo proveerá siempre `X-User-Id`? Por ahora es opcional; cuando se implemente JWT (Fase 1 pendiente) se extraerá del claim `sub` automáticamente.
- ¿Se necesita un job de vacuuming para `idempotency_keys` expiradas? Con volumen bajo, el filtro `WHERE expires_at > NOW()` es suficiente por ahora.
