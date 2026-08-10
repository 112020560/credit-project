## Why

El sistema no puede operar en alta disponibilidad (múltiples instancias) sin causar bugs financieros graves: los workers acumulan interés dos veces, los pagos duplicados son aceptados sin detección, y no existe visibilidad del estado del servicio ni trazabilidad de quién ejecutó cada operación. La Fase 3 cierra estas brechas antes de llevar el sistema a producción.

## What Changes

- **Distributed locking en workers**: cada BackgroundService adquiere un PostgreSQL advisory lock antes de ejecutar su job. Si otra instancia ya tiene el lock, salta esa ejecución silenciosamente. Cero dependencias externas — usa la conexión Postgres existente.
- **Idempotencia de pagos**: los endpoints `POST /api/payments/{loanId}/apply` y `POST /api/revolving/{creditId}/payment` aceptan el header `Idempotency-Key` (UUID). El sistema almacena la respuesta del primer procesamiento y la devuelve en reintentos, expirando a las 24 horas.
- **Healthchecks**: se exponen `/health` (liveness) y `/health/ready` (readiness). El readiness verifica conectividad a PostgreSQL, alcanzabilidad de RabbitMQ y que `UnderwritingPolicy` esté cargada.
- **Audit trail de usuario**: los endpoints de escritura aceptan el header opcional `X-User-Id`. Cada operación financiera se registra en una tabla `audit_log` (append-only) con: timestamp, user_id, acción, entidad, entity_id, detalles JSON.

## Capabilities

### New Capabilities

- `distributed-locking`: mecanismo de advisory lock sobre PostgreSQL que garantiza ejecución exclusiva de cada worker en entornos multi-instancia
- `payment-idempotency`: almacenamiento y reutilización de respuestas de pago identificadas por `Idempotency-Key`
- `healthchecks`: endpoints estándar de liveness y readiness con verificación de dependencias externas
- `audit-trail`: registro append-only de operaciones financieras con identificador de usuario

### Modified Capabilities

- `loan-contract`: los endpoints de creación y desembolso ahora registran `X-User-Id` en el audit log

## Impact

- **Nuevas tablas SQL**: `idempotency_keys`, `audit_log`
- **Paquetes NuGet**: `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.RabbitMQ`
- **Workers afectados**: `InterestAccrualWorker`, `PaymentMissedWorker`, `RevolvingInterestAccrualWorker`, `StatementGenerationWorker`, `RevolvingPaymentMissedWorker`, `RiskClassificationWorker`
- **Endpoints afectados**: `POST /payments/{loanId}/apply`, `POST /revolving/{creditId}/payment`, `POST /loans` (crear contrato), `POST /loans/{loanId}/disburse`, `PUT /loans/{loanId}/risk-category`
- Sin cambios en el dominio ni en los agregados — toda la lógica es infraestructura y API
