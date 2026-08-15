# Arquitectura del Sistema de Crédito

## ¿Qué hace este sistema?

Es el backend de gestión de créditos de una cooperativa. Permite crear y administrar préstamos personales y líneas de crédito rotativas, aplicar pagos, calcular intereses, detectar mora, y generar reportes del portafolio.

---

## La idea central: Event Sourcing

La mayoría de los sistemas guardan **el estado actual** de las cosas:

```
Base de datos tradicional:
  préstamo #1 → { balance: 450,000, estado: Activo, tasa: 16.5% }
```

Este sistema guarda **lo que pasó**, no lo que es:

```
Este sistema:
  préstamo #1 → [
    "Se creó un contrato por 1,500,000 al 16.5% a 12 meses"
    "Fue aprobado"
    "Se desembolsó por transferencia bancaria"
    "Se aplicó un pago de 30,000 → nuevo balance: 1,470,000"
  ]
```

El balance de 1,470,000 no existe en ninguna columna de la base de datos. Se calcula sumando los eventos. Esto tiene ventajas importantes:

- **Auditoría completa**: se sabe exactamente qué pasó, cuándo, y en qué orden
- **Recuperación**: si algo falla, los datos nunca se pierden — los eventos siempre están
- **Reconstrucción**: si una vista de datos se corrompe, se puede regenerar desde cero
- **Historia**: se puede ver cómo estaba un préstamo en cualquier momento del pasado

---

## Los tres principios que guían el diseño

### 1. DDD — El código habla el lenguaje del negocio

El código usa los mismos términos que usa el equipo de crédito: `LoanContract`, `Disburse`, `ApplyPayment`, `RecordMissedPayment`. No hay traducciones raras entre lo que el negocio dice y lo que el código hace.

### 2. CQRS — Escribir y leer son caminos separados

- **Escribir** (comandos): `POST /loans`, `POST /loans/{id}/payments` → modifica el event store
- **Leer** (consultas): `GET /loans/{id}/summary` → lee tablas optimizadas para lectura rápida

No se usa la misma base de datos para las dos cosas. Los comandos van al event store; las consultas van a tablas simples y planas llamadas **read models**.

### 3. Consistencia eventual

Cuando se crea un préstamo y el sistema responde `201 Created`, significa: **"el comando fue aceptado y los eventos fueron guardados"**. Los read models (las tablas de consulta) se actualizan segundos después, de forma asíncrona.

Esto es equivalente a cómo funciona un banco real: cuando hacés una transferencia, el banco te confirma al instante pero el dinero puede tardar minutos en aparecer en el destino.

---

## Cómo fluye una operación

### Crear un préstamo: `POST /loans`

```
Cliente (app web / app móvil)
         │
         │  POST /loans { monto, plazo, cliente, producto }
         ▼
┌─────────────────────────────────────────────┐
│              API (CreditSystem.Api)         │
│  Recibe la solicitud, la valida básicamente │
└───────────────────┬─────────────────────────┘
                    │ despacha comando
                    ▼
┌─────────────────────────────────────────────┐
│         Application (CQRS Handler)          │
│                                             │
│  1. Busca al cliente                        │
│  2. Busca el producto de crédito            │
│  3. Carga la política de underwriting       │
│  4. Evalúa las reglas del motor:            │
│     • Score crediticio ✓                    │
│     • Deuda vs ingreso ✓                    │
│     • Garantías ✓                           │
│     • Préstamos activos ✓                   │
│     • Capacidad de pago ✓                   │
│     • Aportes cooperativos ✓               │
│  5. Crea el aggregate LoanContract          │
│     → genera eventos en memoria             │
│  6. Guarda en el Event Store                │
└───────────────────┬─────────────────────────┘
                    │ persiste
                    ▼
┌─────────────────────────────────────────────┐
│           Event Store (Postgres)            │
│                                             │
│  event_streams:                             │
│    stream_id: abc-123, version: 2           │
│                                             │
│  stored_events:                             │
│    ContractCreated  (sequence: 1041)        │
│    ContractApproved (sequence: 1042)        │
│                                             │
│  event_outbox:                              │
│    pendiente de publicar en RabbitMQ        │
└─────────────────────────────────────────────┘
         │
         │  201 Created { loanId: abc-123 }
         ▼
      Cliente

[Segundos después, en segundo plano]
         │
         ▼
┌─────────────────────────────────────────────┐
│       ProjectionDispatcherWorker            │
│   (lee eventos nuevos cada 5 segundos)      │
│                                             │
│  ¿Cuál fue el último evento procesado?      │
│    → projection_checkpoints: sequence 1040  │
│                                             │
│  Dame eventos con sequence > 1040:          │
│    → ContractCreated (1041)                 │
│    → ContractApproved (1042)                │
│                                             │
│  Despacha a cada projector:                 │
│    LoanSummaryProjector    → rm_loan_summaries ✓
│    DelinquentLoansProjector → no aplica     │
│    LoanPortfolioProjector  → rm_loan_portfolio ✓
│    ...                                      │
│                                             │
│  Avanza checkpoint a 1042                   │
└─────────────────────────────────────────────┘
```

### Consultar un préstamo: `GET /loans/{id}/summary`

```
Cliente
  │
  │ GET /loans/abc-123/summary
  ▼
API
  │
  └─→ SELECT * FROM rm_loan_summaries WHERE loan_id = 'abc-123'
      (tabla simple, lectura instantánea)
  │
  ▼
{ loanId, customerName, balance, status, nextPaymentDate, ... }
```

---

## Las tablas de la base de datos

### Event Store (fuente de verdad)

| Tabla | Para qué sirve |
|-------|----------------|
| `event_streams` | Un registro por préstamo/línea de crédito. Guarda la versión actual para control de concurrencia |
| `stored_events` | Todos los eventos de todos los aggregates. Cada fila es algo que pasó |
| `event_outbox` | Cola de eventos pendientes de enviar a RabbitMQ |
| `event_snapshots` | Foto del estado guardada cada cierto número de eventos (optimización) |

### Control de proyecciones

| Tabla | Para qué sirve |
|-------|----------------|
| `projection_checkpoints` | Hasta qué evento llegó cada projector (para saber qué falta procesar) |
| `projection_failures` | Eventos que fallaron al proyectarse después de 3 reintentos |

### Read Models (lectura rápida)

| Tabla | Contenido |
|-------|-----------|
| `rm_loan_summaries` | Resumen de cada préstamo: balance, estado, tasa, próximo pago |
| `rm_delinquent_loans` | Préstamos en mora con días de atraso y monto vencido |
| `rm_payment_history` | Historial de pagos aplicados |
| `rm_loan_portfolio` | Totales agregados del portafolio (activos, montos, etc.) |
| `rm_revolving_credit_summaries` | Resumen de líneas de crédito rotativas |
| `rm_revolving_transactions` | Movimientos de cada línea de crédito |
| `rm_revolving_statements` | Estados de cuenta generados |
| `rm_payment_tracking` | Seguimiento de pagos por cuota |

---

## Cómo funciona el worker de proyecciones

El `ProjectionDispatcherWorker` es un proceso que corre en segundo plano cada 5 segundos. Es el corazón de la consistencia eventual.

```
stored_events                       projection_checkpoints
┌─────────────────────────────┐     ┌──────────────────────────────┐
│ sequence │ event_type       │     │ projector_name  │ last_seq   │
│ 1001     │ ContractCreated  │     │ LoanSummary     │ 1000       │
│ 1002     │ ContractApproved │     │ DelinquentLoans │ 1002       │
│ 1003     │ LoanDisbursed    │     │ PaymentHistory  │ 1000       │
│ 1004     │ PaymentApplied   │     └──────────────────────────────┘
└─────────────────────────────┘

Para LoanSummary (checkpoint = 1000):
  → Trae eventos con sequence > 1000: [1001, 1002, 1003, 1004]
  → Procesa cada uno → actualiza rm_loan_summaries
  → Avanza checkpoint a 1004

Para DelinquentLoans (checkpoint = 1002):
  → Trae eventos con sequence > 1002: [1003, 1004]
  → Procesa cada uno → rm_delinquent_loans (si aplica)
  → Avanza checkpoint a 1004
```

**Cada projector tiene su propio checkpoint.** Si uno está atrasado no afecta a los demás.

### Qué pasa si un projector falla

```
Evento 1003 → LoanSummaryProjector → Error de base de datos

  Intento 1 → falla → espera 1 segundo
  Intento 2 → falla → espera 2 segundos
  Intento 3 → falla → espera 4 segundos
  Intento 4 → falla → registra en projection_failures → avanza checkpoint

El sistema sigue procesando eventos posteriores.
El fallo queda registrado y visible para el equipo técnico.
```

---

## Cómo ver y resolver fallos de proyección

```
# Ver fallos no resueltos
GET /api/v1/admin/projection/failures

# Ver historial completo
GET /api/v1/admin/projection/failures?resolved=true

# Marcar un fallo como resuelto manualmente
POST /api/v1/admin/projection/failures/{id}/resolve

# Reconstruir todos los read models desde cero
POST /api/v1/admin/projection/rebuild
```

El endpoint `/rebuild` borra todas las tablas de lectura y reinicia todos los checkpoints a 0. El worker automáticamente vuelve a procesar todos los eventos desde el principio y reconstruye los read models. **Los datos de negocio nunca se pierden** porque los eventos originales siempre están en `stored_events`.

---

## Los módulos del sistema

```
┌──────────────────────────────────────────────────────────────────┐
│                          CreditSystem.Api                        │
│  Endpoints REST: préstamos, pagos, productos, reportes, admin    │
└────────────────────────────┬─────────────────────────────────────┘
                             │
          ┌──────────────────┴──────────────────┐
          │                                     │
          ▼                                     ▼
┌─────────────────────┐             ┌───────────────────────────┐
│ CreditSystem.       │             │ CreditSystem.             │
│ Application         │             │ Infrastructure            │
│                     │             │                           │
│ • Comandos y        │             │ • Event Store (Postgres)  │
│   manejadores       │             │ • Projectors              │
│ • Validaciones      │             │ • Workers (background)    │
│   (FluentValidation)│             │ • Mensajería (RabbitMQ)   │
│ • Jobs de           │             │ • Webhooks                │
│   mantenimiento     │             │ • Repositorios            │
└──────────┬──────────┘             └───────────────────────────┘
           │
           ▼
┌─────────────────────┐
│ CreditSystem.Domain │
│                     │
│ • Aggregates        │
│ • Eventos de dominio│
│ • Reglas de negocio │
│ • Value Objects     │
└─────────────────────┘
```

---

## Los préstamos y su ciclo de vida

```
                    Create()
                       │
                       ▼
                  [Approved]
                       │
                  Disburse()
                       │
                       ▼
                   [Active]
                  ┌────┴────┐
          pago    │         │ pago no llega
          llega   │         │
                  │         ▼
                  │    [Delinquent]
                  │         │
                  │  RecordMissedPayment()
                  │  (supera umbral)
                  │         │
                  │         ▼
                  │     [Default]
                  │
                  └─────────┐
                  ApplyPayment()
                  (saldo = 0)
                            │
                            ▼
                        [PaidOff]
```

---

## Las líneas de crédito rotativas y su ciclo de vida

```
    Create() → [Pending] → Activate() → [Active]
                                           │
                              ┌────────────┼────────────┐
                              │            │            │
                         DrawFunds()  ApplyPayment() Freeze()
                         AccrueInterest()              │
                         GenerateStatement()            ▼
                                                    [Frozen]
                                                        │
                                                   Unfreeze()
                                                        │
                                                        ▼
                                                    [Active]
                                                        │
                                                    Close()
                                                        │
                                                        ▼
                                                    [Closed]
```

---

## Motor de evaluación de crédito

Cuando se crea un préstamo, el sistema evalúa automáticamente una serie de reglas:

| Regla | Qué evalúa |
|-------|-----------|
| `CreditScoreRule` | Score crediticio mínimo del cliente |
| `DebtToIncomeRule` | Ratio deuda/ingreso (DTI) |
| `CollateralRule` | Valor de las garantías ofrecidas |
| `MaxLoanAmountRule` | Monto máximo permitido por la política |
| `ActiveLoansRule` | Si el cliente ya tiene préstamos activos |
| `MemberSharesRule` | Aportes del socio en la cooperativa |
| `PaymentCapacityRule` | Capacidad de pago mensual |

Algunas reglas son de **parada inmediata** (si fallan, se rechaza el préstamo sin evaluar las demás). Las demás acumulan ajustes a la tasa base. El resultado final es `Aprobado` con la tasa definitiva, o `Rechazado` con las razones.

La **política de underwriting** es configurable por producto (`underwriting_policies`), lo que permite tener reglas distintas para préstamos personales, hipotecarios, etc.

---

## Mensajería y eventos externos

El sistema publica eventos a RabbitMQ cuando suceden cosas importantes:

```
Evento en stored_events
       │
       ▼
  event_outbox
       │
OutboxPublisherWorker (cada pocos segundos)
       │
       ▼
   RabbitMQ
       │
  ┌────┴────┐
  │         │
  ▼         ▼
Otros    Webhooks
sistemas (notificaciones
         a sistemas externos
         registrados)
```

El patrón **Outbox** garantiza que si el sistema se cae justo después de guardar un evento pero antes de publicarlo, el evento no se pierde — queda en la tabla `event_outbox` y se publica cuando el sistema vuelve.

---

## Workers en segundo plano

| Worker | Frecuencia | Qué hace |
|--------|-----------|----------|
| `ProjectionDispatcherWorker` | Cada 5 segundos | Actualiza los read models con eventos nuevos |
| `InterestAccrualWorker` | Diario a las 2am | Calcula y acumula intereses en préstamos activos |
| `PaymentMissedWorker` | Diario | Detecta cuotas vencidas y aplica cargos por mora |
| `RevolvingInterestAccrualWorker` | Diario | Intereses en líneas de crédito rotativas |
| `RevolvingPaymentMissedWorker` | Diario | Pagos vencidos en líneas rotativas |
| `StatementGenerationWorker` | Diario | Genera estados de cuenta de líneas rotativas |
| `OutboxPublisherWorker` | Continuo | Publica eventos pendientes en RabbitMQ |
| `WebhookDeliveryWorker` | Continuo | Entrega notificaciones a sistemas externos |
| `RiskClassificationWorker` | Periódico | Clasifica el riesgo del portafolio |
| `RateAdjustmentWorker` | Periódico | Ajusta tasas variables según tasa de referencia |

---

## Stack tecnológico

| Componente | Tecnología |
|-----------|-----------|
| Lenguaje | C# / .NET 9 |
| Base de datos | PostgreSQL |
| ORM | Dapper (SQL directo, sin ORM pesado) |
| API | Minimal API (ASP.NET Core) |
| Mensajería | RabbitMQ vía MassTransit |
| Validación | FluentValidation |
| Observabilidad | OpenTelemetry + Serilog + Seq |
| Tests | xUnit + NSubstitute + FluentAssertions |

---

## Principio de diseño clave

> **El event store es la única fuente de verdad.**
>
> Los read models son derivados: pueden borrarse y reconstruirse en cualquier momento sin perder ningún dato de negocio. Si hay una inconsistencia en un reporte, la solución es reconstruir — no parchear la tabla directamente.
>
> Una respuesta `201 Created` significa "el comando fue aceptado y los eventos fueron persistidos". No significa "el reporte ya está actualizado". Los reportes se actualizan en los segundos siguientes de forma automática.
