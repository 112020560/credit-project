# Flujo de Pagos — Credit System

Existen dos rutas de pago en el sistema. La elección depende del caso de uso.

---

## Ruta 1 — Pago Síncrono (con Idempotencia)

Usada cuando el cliente necesita confirmación inmediata y control de duplicados.

```
Cliente
  │
  │  POST /api/loans/{id}/payments
  │  Header: Idempotency-Key: <uuid>
  │  Header: X-User-Id: <userId>
  ▼
LoanContractEndpoints
  │
  ├─► IIdempotencyRepository.FindAsync(key)
  │     ├─ [existe] ──► 200 OK (respuesta cacheada)
  │     │               Header: Idempotency-Replayed: true
  │     │
  │     └─ [no existe]
  │           │
  │           ▼
  │       IMediator.Send(ApplyPaymentCommand)
  │           │
  │           ▼
  │       ApplyPaymentCommandHandler
  │           │
  │           ├─► ILoanContractRepository.GetByIdAsync()
  │           │       └─ reconstruye LoanContractAggregate desde event store
  │           │
  │           ├─► aggregate.ApplyPayment(paymentId, amount, method)
  │           │       └─ emite PaymentApplied (domain event)
  │           │          orden: fees → interest → principal
  │           │
  │           ├─► ILoanContractRepository.SaveAsync()
  │           │       └─ persiste eventos en PostgreSQL (event store)
  │           │
  │           └─► IProjectionEngine.ProjectEventsAsync()
  │                   ├─ PaymentHistoryProjector  → rm_payment_history
  │                   └─ PaymentTrackingProjector → rm_payment_tracking
  │
  ├─► IIdempotencyRepository.SaveAsync(key, response, expiresAt+24h)
  │
  └─► IAuditLogRepository.LogAsync(action="payment.applied", userId, ...)
            └─ INSERT audit_log (fire-and-forget, absorbe excepciones)

  Respuesta: 200 OK { loanId, amountApplied, newBalance, ... }
```

---

## Ruta 2 — Pago Asíncrono (Outbox + RabbitMQ)

Usada cuando el origen del pago es externo (canal bancario, SINPE, caja) y se necesita desacoplar la recepción del procesamiento.

```
Cliente
  │
  │  POST /api/payments
  │  { loanId, customerId, amount, currency, paymentMethod }
  ▼
PaymentsEndpoints
  │
  ▼
SubmitPaymentCommandHandler
  │
  ├─► ILoanContractRepository.GetByIdAsync()   ← validación rápida (activo?)
  │
  ├─► IPaymentTrackingRepository.CreateAsync() ← rm_payment_tracking: PENDING
  │
  └─► IOutboxRepository.SaveAsync()            ← outbox: ProcessPaymentMessage

  Respuesta inmediata: 202 Accepted { paymentId, trackingUrl }


  [Background — OutboxPublisherWorker]
  │
  ├─ Polling outbox table (Postgres)
  └─ Publica ProcessPaymentMessage → RabbitMQ
                                        queue: credit-service-payments


  [Consumer — ProcessPaymentConsumer]
  │
  ├─► rm_payment_tracking: PROCESSING
  │
  ├─► ILoanContractRepository.GetByIdAsync()
  │       └─ reconstruye LoanContractAggregate
  │
  ├─► aggregate.ApplyPayment(...)
  │       └─ emite PaymentApplied
  │
  ├─► ILoanContractRepository.SaveAsync()
  │       └─ persiste eventos en event store
  │
  ├─► IProjectionEngine.ProjectEventsAsync()
  │       ├─ PaymentHistoryProjector
  │       └─ PaymentTrackingProjector
  │
  ├─► rm_payment_tracking: COMPLETED (con breakdown principal/interés/fees)
  │
  └─► Publica resultado → RabbitMQ
        ├─ PaymentProcessed   (éxito)
        ├─ PaymentRejected    (regla de negocio)
        └─ PaymentFailed      (error técnico, dispara retry)


  [Background — WebhookDeliveryWorker]
  │
  └─ Escucha PaymentProcessed / PaymentRejected / PaymentFailed
     └─ HTTP POST a los callbacks registrados en webhook_subscriptions


  [Cliente polling]
  GET /api/payments/{paymentId}/status
  └─ Consulta rm_payment_tracking → PENDING | PROCESSING | COMPLETED | REJECTED | FAILED
```

---

## Workers de Mantenimiento

Complementan el flujo con detección automática de morosidad e intereses.

```
[Cada noche — PaymentMissedWorker]
  │
  ├─ pg_try_advisory_lock(1002)   ← distributed lock (una sola instancia en HA)
  ├─ PaymentMissedJob.ExecuteAsync()
  │     └─ detecta cuotas vencidas → aggregate.RecordMissedPayment()
  │        → emite PaymentMissed → persiste → proyecta → DelinquentLoansProjector
  └─ pg_advisory_unlock(1002)

[Cada noche — InterestAccrualWorker]
  │
  ├─ pg_try_advisory_lock(1001)
  ├─ InterestAccrualJob.ExecuteAsync()
  │     └─ aggregate.AccrueInterest() → emite InterestAccrued → persiste
  └─ pg_advisory_unlock(1001)
```

---

## Componentes y Responsabilidades

| Componente | Capa | Responsabilidad |
|---|---|---|
| `PaymentsEndpoints` | Api | Intake async — 202 Accepted |
| `LoanContractEndpoints` | Api | Pago sync con idempotencia |
| `SubmitPaymentCommandHandler` | Application | Validación, tracking, outbox |
| `ApplyPaymentCommandHandler` | Application | Aplicar pago al agregado |
| `OutboxPublisherWorker` | Infrastructure | Relay outbox → RabbitMQ |
| `ProcessPaymentConsumer` | Infrastructure | Procesar mensaje de pago |
| `LoanContractAggregate` | Domain | Lógica de negocio del pago |
| `IProjectionEngine` | Infrastructure | Fan-out a read models |
| `WebhookDeliveryWorker` | Infrastructure | Notificaciones a suscriptores |
| `PaymentMissedWorker` | Infrastructure | Detección de morosidad |
| `IIdempotencyRepository` | Infrastructure | Cache de respuestas (24h TTL) |
| `IAuditLogRepository` | Infrastructure | Trazabilidad de usuario |

---

## Por qué NO es necesario un microservicio de pagos separado

El sistema ya tiene separación real a nivel de protocolo:

- **Recepción** (`POST /api/payments`) y **procesamiento** (`ProcessPaymentConsumer`) corren en procesos lógicos distintos, desacoplados por RabbitMQ
- **Escalabilidad horizontal**: múltiples instancias del mismo proceso son seguras gracias a los advisory locks (workers) y `ON CONFLICT DO NOTHING` (idempotency)
- **Resiliencia**: el outbox garantiza at-least-once delivery aunque RabbitMQ esté caído al momento del submit
- Un `PaymentGateway.Api` independiente tendría sentido si hubiera **múltiples canales de pago externos** (SINPE, tarjetas, caja presencial) que necesitan normalizar formatos distintos antes de llegar a la queue
