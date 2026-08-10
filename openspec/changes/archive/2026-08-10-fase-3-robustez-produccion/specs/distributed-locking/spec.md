## Purpose

Garantizar que cada worker de background se ejecute en una sola instancia a la vez cuando el sistema corre en modo alta disponibilidad (múltiples réplicas), usando PostgreSQL advisory locks como mecanismo de exclusión mutua.

## Requirements

### Requirement: Advisory lock antes de ejecutar un job

El sistema SHALL intentar adquirir un PostgreSQL advisory lock exclusivo por sesión (`pg_try_advisory_lock`) antes de ejecutar el cuerpo de cada BackgroundService job. Si el lock no está disponible (otra instancia lo tiene), el worker SHALL saltar esa ejecución y esperar hasta el siguiente ciclo programado.

Workers que aplican: `InterestAccrualWorker`, `PaymentMissedWorker`, `RevolvingInterestAccrualWorker`, `StatementGenerationWorker`, `RevolvingPaymentMissedWorker`, `RiskClassificationWorker`.

#### Scenario: Lock disponible — ejecución normal

- **WHEN** ninguna otra instancia tiene el advisory lock del worker
- **THEN** el worker adquiere el lock, ejecuta el job completo y libera el lock al terminar

#### Scenario: Lock no disponible — skip silencioso

- **WHEN** otra instancia ya tiene el advisory lock del worker
- **THEN** `pg_try_advisory_lock` retorna false, el worker registra un log de nivel Debug y retorna sin ejecutar el job

#### Scenario: Lock liberado tras excepción

- **WHEN** el job lanza una excepción no controlada durante la ejecución
- **THEN** el lock es liberado automáticamente al cerrar la conexión (session-level lock se libera con la conexión)
- **THEN** el worker registra el error y espera el siguiente ciclo

### Requirement: Lock ID único por worker

Cada worker SHALL usar un lock ID entero (`long`) único y fijo, derivado de un enum o constante en código, para evitar colisiones entre workers distintos.

#### Scenario: Workers distintos no bloquean entre sí

- **WHEN** `InterestAccrualWorker` tiene su lock adquirido
- **THEN** `PaymentMissedWorker` puede adquirir su propio lock sin interferencia

#### Scenario: Misma instancia no puede adquirir el mismo lock dos veces

- **WHEN** el mismo proceso intenta adquirir el mismo lock ID dos veces (re-entrada)
- **THEN** la segunda llamada a `pg_try_advisory_lock` retorna true (session locks son reentrantes en Postgres) sin bloqueo
