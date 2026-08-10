## 1. Migración SQL

- [x] 1.1 Crear script `20260810_AddIdempotencyKeysTable.sql`: tabla `idempotency_keys (idempotency_key UUID PRIMARY KEY, response_status INT NOT NULL, response_body TEXT NOT NULL, created_at TIMESTAMPTZ DEFAULT NOW(), expires_at TIMESTAMPTZ NOT NULL)` con índice en `expires_at`
- [x] 1.2 Crear script `20260810_AddAuditLogTable.sql`: tabla `audit_log (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), user_id VARCHAR(256), action VARCHAR(128) NOT NULL, entity_type VARCHAR(128) NOT NULL, entity_id UUID NOT NULL, details JSONB)` con índice en `(entity_type, entity_id)` y en `occurred_at`

## 2. Distributed Locking — Infrastructure

- [x] 2.1 Crear clase `PostgresAdvisoryLock` en `CreditSystem.Infrastructure/Locking/` con métodos `Task<bool> TryAcquireAsync(long lockId, CancellationToken)` y `Task ReleaseAsync(long lockId, CancellationToken)` usando `SELECT pg_try_advisory_lock(@id)` y `SELECT pg_advisory_unlock(@id)` vía Npgsql (conexión dedicada, no pooled)
- [x] 2.2 Crear enum `WorkerLockId` en `CreditSystem.Infrastructure/Locking/` con valores: `InterestAccrual = 1001`, `PaymentMissed = 1002`, `RevolvingInterestAccrual = 1003`, `StatementGeneration = 1004`, `RevolvingPaymentMissed = 1005`, `RiskClassification = 1006`
- [x] 2.3 Crear interfaz `IDistributedLock` en `CreditSystem.Domain/Abstractions/` con los mismos métodos `TryAcquireAsync` / `ReleaseAsync`
- [x] 2.4 Registrar `IDistributedLock → PostgresAdvisoryLock` como Singleton en `DependencyInjection.cs` de Infrastructure, pasando `connectionString` al constructor

## 3. Distributed Locking — Workers

- [x] 3.1 Modificar `InterestAccrualWorker`: inyectar `IDistributedLock`, envolver `RunJobAsync` con `TryAcquireAsync(WorkerLockId.InterestAccrual)` — si retorna false, loguear debug y continuar al siguiente ciclo; liberar el lock en `finally`
- [x] 3.2 Modificar `PaymentMissedWorker` con el mismo patrón usando `WorkerLockId.PaymentMissed`
- [x] 3.3 Modificar `RevolvingInterestAccrualWorker` con `WorkerLockId.RevolvingInterestAccrual`
- [x] 3.4 Modificar `StatementGenerationWorker` con `WorkerLockId.StatementGeneration`
- [x] 3.5 Modificar `RevolvingPaymentMissedWorker` con `WorkerLockId.RevolvingPaymentMissed`
- [x] 3.6 Modificar `RiskClassificationWorker` con `WorkerLockId.RiskClassification`

## 4. Idempotencia de Pagos — Infrastructure

- [x] 4.1 Crear interfaz `IIdempotencyRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con métodos: `Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken)` y `Task SaveAsync(IdempotencyRecord record, CancellationToken)`
- [x] 4.2 Crear record `IdempotencyRecord` en `CreditSystem.Domain/Models/` con propiedades: `Guid Key`, `int ResponseStatus`, `string ResponseBody`, `DateTime ExpiresAt`
- [x] 4.3 Crear `IdempotencyRepository` en `CreditSystem.Infrastructure/Repositories/` que implementa `IIdempotencyRepository` con Dapper: `SELECT ... WHERE idempotency_key = @Key AND expires_at > NOW()` y `INSERT INTO idempotency_keys ... ON CONFLICT DO NOTHING`
- [x] 4.4 Registrar `IIdempotencyRepository → IdempotencyRepository` (Scoped) en `DependencyInjection.cs` de Infrastructure

## 5. Idempotencia de Pagos — API

- [x] 5.1 En `LoanContractEndpoints.cs`, leer header `Idempotency-Key` en el endpoint `POST /loans/{id}/payments`: si está presente y existe en `IIdempotencyRepository`, retornar respuesta almacenada con header `Idempotency-Replayed: true`; si no existe, procesar normalmente y guardar la respuesta en el repositorio con `ExpiresAt = DateTime.UtcNow.AddHours(24)`
- [x] 5.2 Aplicar el mismo patrón en el endpoint de pago revolving `POST /revolving-credits/{id}/payments` en `RevolvingCreditEndpoints.cs`

## 6. Healthchecks

- [x] 6.1 Agregar paquetes NuGet a `CreditSystem.Api`: `AspNetCore.HealthChecks.NpgSql` y `AspNetCore.HealthChecks.UI.Client`
- [x] 6.2 Crear `UnderwritingPolicyHealthCheck` en `CreditSystem.Infrastructure/HealthChecks/` que implementa `IHealthCheck`: verifica que `UnderwritingPolicy` esté registrado en el DI container y no sea null
- [x] 6.3 En `Program.cs`, registrar healthchecks: `services.AddHealthChecks().AddNpgSql(...).AddCheck<UnderwritingPolicyHealthCheck>().AddCheck<RabbitMqHealthCheck>()`
- [x] 6.4 En `Program.cs`, mapear endpoints: `app.MapHealthChecks("/health")` (liveness) y `app.MapHealthChecks("/health/ready")` (readiness con JSON response writer)
- [x] 6.5 Registrar `UnderwritingPolicyHealthCheck` y `RabbitMqHealthCheck` en `DependencyInjection.cs` de Infrastructure como Transient

## 7. Audit Trail — Infrastructure

- [x] 7.1 Crear record `AuditEntry` en `CreditSystem.Domain/Models/` con: `Guid Id`, `DateTime OccurredAt`, `string? UserId`, `string Action`, `string EntityType`, `Guid EntityId`, `object? Details`
- [x] 7.2 Crear interfaz `IAuditLogRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con método único: `Task LogAsync(AuditEntry entry, CancellationToken)`
- [x] 7.3 Crear `AuditLogRepository` en `CreditSystem.Infrastructure/Repositories/` que implementa `IAuditLogRepository`: INSERT en `audit_log` con Dapper; el método absorbe excepciones internamente (try/catch que loguea el error sin relanzarlo)
- [x] 7.4 Registrar `IAuditLogRepository → AuditLogRepository` (Scoped) en `DependencyInjection.cs` de Infrastructure

## 8. Audit Trail — API

- [x] 8.1 En `LoanContractEndpoints.cs`, leer header `X-User-Id` en `POST /loans` (crear contrato): si la operación fue exitosa, llamar `IAuditLogRepository.LogAsync` con `action = "contract.created"`, `entityType = "LoanContract"`, `entityId = loanId`, `details = new { amount, currency }`
- [x] 8.2 En `LoanContractEndpoints.cs`, leer header `X-User-Id` en `POST /loans/{loanId}/disburse` y registrar `action = "contract.disbursed"`
- [x] 8.3 En `LoanContractEndpoints.cs`, leer header `X-User-Id` en `POST /loans/{id}/payments` y registrar `action = "payment.applied"`, `details = new { amount, currency }`
- [x] 8.4 En `RevolvingCreditEndpoints.cs`, leer header `X-User-Id` en `POST /revolving-credits/{id}/payments` y registrar `action = "revolving.payment.applied"`
- [x] 8.5 En `RiskEndpoints.cs`, leer header `X-User-Id` en `PUT /loans/{loanId}/risk-category` y registrar `action = "risk.manual.reclassified"`, `details = new { newCategory }`

## 9. Tests

- [x] 9.1 Test unitario de `WorkerLockId`: verificar que todos los IDs son únicos y positivos
- [x] 9.2 Test de idempotencia: `AuditLogRepository.LogAsync` absorbe excepciones sin propagarlas (cubre el caso de DB inaccesible)
- [x] 9.3 Test unitario de `UnderwritingPolicyHealthCheck`: retorna `Healthy` cuando la policy está presente, `Unhealthy` cuando es null
- [x] 9.4 Test unitario de `AuditLogRepository`: verificar que una excepción en el INSERT es capturada y no propagada
