# Flujo Completo del Ciclo de Vida de un Préstamo

Base URL: `https://{host}/api/v1`

---

## Contexto del sistema

Los **clientes** llegan al sistema de crédito desde el CRM vía mensajes RabbitMQ (`CustomerCreated` / `CustomerUpdated`), consumidos por `CustomerCreatedConsumer` y almacenados en `customer_credit_profiles`.

Los **socios cooperativos** son un concepto distinto y opcional. Un cliente se convierte en socio mediante un enrolamiento explícito vía `POST /api/v1/members/enroll` — no se crean automáticamente al recibir un `CustomerCreated`. Ver flujo completo en `Docs/flows/customer-and-member-flow.md`.

Los **pagos** tienen dos vías:
- **Síncrona**: `POST /loans/{id}/payments` — responde inmediatamente con el resultado.
- **Asíncrona** (cola): `POST /payments` → RabbitMQ → `ProcessPaymentConsumer` → webhook/polling.

---

## PASO 0 — Crear un producto crediticio

El producto define las reglas del préstamo: tasas, plazos, prelación de pagos y capital social.

```http
POST /api/v1/products
Content-Type: application/json

{
  "name": "Préstamo Personal Cooperativa",
  "minAmount": 100000,
  "maxAmount": 5000000,
  "minTermMonths": 6,
  "maxTermMonths": 60,
  "baseInterestRate": 14.5,
  "maxLtv": null,
  "defaultAmortizationMethod": "French",
  "requiresCollateral": false,
  "waterfall": [
    { "priority": 1, "component": "Fees" },
    { "priority": 2, "component": "PenaltyInterest" },
    { "priority": 3, "component": "SocialCapital" },
    { "priority": 4, "component": "RegularInterest" },
    { "priority": 5, "component": "Principal" }
  ],
  "socialCapitalConfig": {
    "calculationType": "FixedAmount",
    "value": 750.00,
    "collectionMode": "IncludedInPayment"
  }
}
```

**Respuesta `201 Created`:**
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

> **`waterfall`** define el orden en que se aplica cada pago. `SocialCapital` antes de `RegularInterest` significa que de los ₡ de la cuota, primero se separa el aporte cooperativo, luego intereses, luego principal.
>
> **`socialCapitalConfig`**: `IncludedInPayment` → el socio paga un solo monto; ₡750 se redirigen al capital social, el resto amortiza el préstamo. `SeparateCollection` → el préstamo recibe el monto completo y el aporte se registra como débito aparte.

**Opciones de `calculationType`:**
| Valor | Descripción |
|-------|-------------|
| `FixedAmount` | Monto fijo por cuota (ej. ₡750 siempre) |
| `PercentageOfPayment` | % del total de la cuota |
| `PercentageOfPrincipalPaid` | % del capital amortizado en esa cuota |
| `PercentageOfOriginalAmount` | % del monto original del préstamo |

---

## PASO 1 — Verificar que el socio existe

El socio debe existir en el sistema (enrolado vía `POST /members/enroll`). Se consulta por su `externalId` (el ID del CRM).

```http
GET /api/v1/members/{externalId}
```

**Respuesta `200 OK`:**
```json
{
  "externalId": "a1b2c3d4-0000-0000-0000-000000000001",
  "memberNumber": "CM-2024-00042",
  "status": "Active",
  "joinedAt": "2022-03-15T00:00:00Z",
  "totalSharesAmount": 125000.00,
  "sharesCurrency": "CRC",
  "numberOfContributions": 24,
  "lastContributionDate": "2026-07-01T00:00:00Z"
}
```

> Si devuelve `404`, el cliente aún no ha sido enrolado como socio. Ver flujo en `Docs/flows/customer-and-member-flow.md`.

---

## PASO 2 — Crear el contrato (evaluación y aprobación)

El sistema evalúa automáticamente el perfil del socio contra las reglas del motor en orden de prioridad:

| Prioridad | Regla | Tipo | Criterio |
|---|---|---|---|
| 0 | `MemberSharesEvaluation` | HardStop si `enforce=true` | Aportaciones × multiplier |
| 1 | `ProductEligibility` | HardStop | Monto y plazo dentro del rango del producto |
| 1 | `CreditScoreEvaluation` | HardStop | Score ≥ mínimo |
| 2 | `DebtToIncomeRatio` | **HardStop** | DTI ≤ `max_dti_ratio` (default 50%) |
| 2 | `PaymentCapacityEvaluation` | **HardStop** | Monto ≤ PV(cuota_max, tasa_producto, plazo) |
| 3 | `CollateralEvaluation` | Informativa | Ajuste de tasa según cobertura |
| 4 | `ActiveLoansCheck` | Informativa | Ajuste de tasa +1% si tiene préstamos activos |

> No existe cap absoluto de monto en el código. El techo de monto lo define `maxAmount` del producto. El techo financiero real lo calcula `PaymentCapacityEvaluation` desde el ingreso del solicitante.

```http
POST /api/v1/loans
Content-Type: application/json
X-User-Id: usr-oficina-001

{
  "externalCustomerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 1500000,
  "currency": "CRC",
  "termMonths": 24,
  "amortizationMethod": "French",
  "rateType": "Fixed",
  "guarantees": [
    {
      "type": "FianzaSolidaria",
      "description": "Fiador solidario Juan Pérez",
      "appraisalValue": 2000000,
      "coverageRate": 0.80
    }
  ]
}
```

**Respuesta `201 Created` — Aprobado:**
```json
{
  "success": true,
  "contractId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "message": "Contract created successfully",
  "approvedRate": 15.25,
  "evaluationResults": [
    { "ruleName": "ProductEligibility",        "passed": true, "message": "Contract meets eligibility requirements" },
    { "ruleName": "CreditScoreEvaluation",     "passed": true, "message": "Credit score 720 approved with rate adjustment of 0%" },
    { "ruleName": "DebtToIncomeRatio",         "passed": true, "message": "DTI ratio 32.50% is within acceptable limits" },
    { "ruleName": "PaymentCapacityEvaluation", "passed": true, "message": "Requested amount within payment capacity (max financiable: ₡2,340,000)" },
    { "ruleName": "CollateralEvaluation",      "passed": true, "message": "Collateral ratio 66.67% provides security for the loan" },
    { "ruleName": "ActiveLoansCheck",          "passed": true, "message": "Customer has no active loans" }
  ]
}
```

**Respuesta `400 Bad Request` — Rechazado:**
```json
{
  "success": false,
  "message": "Contract rejected",
  "evaluationResults": [
    { "ruleName": "CreditScoreRule", "passed": false, "message": "Credit score 520 below minimum 600" }
  ],
  "errors": ["Credit score 520 below minimum 600"]
}
```

> La `approvedRate` puede ser mayor que la `baseInterestRate` del producto si alguna regla aplica un ajuste de tasa (spread por riesgo). Estado inicial del contrato: **`Approved`**.

---

## PASO 3 — Desembolsar el préstamo

Transición de `Approved` → `Active`. Solo puede desembolsarse una vez.

```http
POST /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/disburse
Content-Type: application/json
X-User-Id: usr-caja-001

{
  "disbursementMethod": "WIRE",
  "destinationAccount": "CR21015201001026284066"
}
```

**Respuesta `200 OK`:**
```json
{
  "success": true,
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "amountDisbursed": 1500000.00,
  "disbursedAt": "2026-08-11T14:32:00Z",
  "message": "Loan disbursed successfully"
}
```

---

## PASO 4 — Consultar estado del préstamo

```http
GET /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789
```

**Respuesta `200 OK`:**
```json
{
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "customerId": "...",
  "customerName": "María González Solano",
  "principal": 1500000.00,
  "currentBalance": 1500000.00,
  "accruedInterest": 0.00,
  "totalFees": 0.00,
  "interestRate": 15.25,
  "termMonths": 24,
  "status": "Active",
  "paymentsMade": 0,
  "paymentsMissed": 0
}
```

---

## PASO 5 — Devengamiento de intereses (proceso automático)

El worker `InterestAccrualWorker` corre diariamente y devengará interés sobre el saldo. No hay endpoint manual — es un proceso background. Después de un mes, el balance de intereses acumulados estará disponible en `GET /loans/{id}`.

Si el socio no paga en la fecha de vencimiento, `PaymentMissedWorker` registra el evento y el estado pasa a **`Delinquent`**.

---

## PASO 6 — Aplicar un pago (síncrono)

```http
POST /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/payments
Content-Type: application/json
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
X-User-Id: usr-caja-001

{
  "amount": 75000.00,
  "currency": "CRC",
  "paymentMethod": "WIRE",
  "referenceNumber": "TRF-2026-08-001"
}
```

> El header `Idempotency-Key` (UUID) evita aplicar el mismo pago dos veces si hay un reintento de red. Si se reenvía la misma key, se devuelve el resultado original sin procesar de nuevo.

**Respuesta `200 OK`:**
```json
{
  "success": true,
  "paymentId": "cc110000-1234-5678-abcd-000000000001",
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "totalApplied": 75000.00,
  "principalPaid": 56382.15,
  "interestPaid": 17867.85,
  "feesPaid": 0.00,
  "socialCapitalContributed": 750.00,
  "newBalance": 1443617.85,
  "isPaidOff": false,
  "message": "Payment applied successfully"
}
```

> La prelación se tomó del producto: primero capital social (₡750), luego interés corriente, luego principal. `totalApplied` = 75000, pero solo ₡74250 amortizaron el préstamo porque ₡750 fueron al capital social (`IncludedInPayment`).

---

## PASO 6-ALT — Aplicar un pago (asíncrono por cola)

Para sistemas que necesitan respuesta inmediata sin esperar el procesamiento:

```http
POST /api/v1/payments
Content-Type: application/json

{
  "paymentId": "cc110000-1234-5678-abcd-000000000002",
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "customerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "amount": 75000.00,
  "currency": "CRC",
  "paymentMethod": "WIRE"
}
```

**Respuesta `202 Accepted`** — el mensaje entra a la cola.

Consultar estado:
```http
GET /api/v1/payments/{paymentId}/status
```

---

## PASO 7 — Ver historial de pagos

```http
GET /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/payments
```

**Respuesta `200 OK`:**
```json
[
  {
    "paymentId": "cc110000-1234-5678-abcd-000000000001",
    "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
    "totalAmount": 75000.00,
    "principalPaid": 56382.15,
    "interestPaid": 17867.85,
    "feePaid": 0.00,
    "socialCapitalContributed": 750.00,
    "paidAt": "2026-09-11T10:15:00Z",
    "paymentMethod": "Wire"
  }
]
```

---

## PASO 8 — Capital social acumulado del socio

Después de cada pago, el `SocialCapitalProjector` actualiza el balance del socio:

```http
GET /api/v1/members/a1b2c3d4-0000-0000-0000-000000000001/social-capital
```

**Respuesta `200 OK`:**
```json
{
  "externalId": "a1b2c3d4-0000-0000-0000-000000000001",
  "socialCapitalBalance": 750.00
}
```

Después de 24 pagos: `socialCapitalBalance = 18000.00` (24 × ₡750).

---

## PASO 9 — Préstamo moroso (Delinquent)

Si el worker detecta pago vencido, el estado pasa a `Delinquent`. Se puede consultar la lista:

```http
GET /api/v1/delinquent-loans?minDaysOverdue=30
```

O el detalle:
```http
GET /api/v1/delinquent-loans/b7e3a1f2-1234-5678-abcd-ef0123456789
```

---

## PASO 10-A — Reestructurar (si está moroso o en default)

```http
POST /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/restructure
Content-Type: application/json

{
  "newInterestRate": 12.0,
  "newTermMonths": 36,
  "forgiveAmount": 50000.00,
  "reason": "Acuerdo de pago por dificultad económica temporal"
}
```

**Respuesta `200 OK`:**
```json
{
  "success": true,
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "message": "Loan restructured successfully"
}
```

---

## PASO 10-B — Marcar en default

```http
POST /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/default
Content-Type: application/json

{
  "reason": "Sin pago por más de 90 días. Caso enviado a cobro judicial."
}
```

---

## PASO 11 — Cancelar anticipadamente (payoff)

Obtener el monto exacto para cancelar hoy:

```http
GET /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/payoff-amount
```

**Respuesta `200 OK`:**
```json
{
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "payoffAmount": 1387234.50,
  "breakdown": {
    "remainingPrincipal": 1380000.00,
    "accruedInterest": 7234.50,
    "fees": 0.00
  },
  "asOfDate": "2026-08-11"
}
```

Ejecutar el payoff:

```http
POST /api/v1/loans/b7e3a1f2-1234-5678-abcd-ef0123456789/payoff
Content-Type: application/json

{
  "paymentMethod": "WIRE",
  "referenceNumber": "TRF-2026-PAYOFF-001"
}
```

**Respuesta `200 OK`:**
```json
{
  "success": true,
  "loanId": "b7e3a1f2-1234-5678-abcd-ef0123456789",
  "message": "Loan paid off successfully"
}
```

Estado final del contrato: **`PaidOff`**.

---

## PASO 12 — Préstamos cancelados

```http
GET /api/v1/loans/paid-off?fromDate=2026-01-01&toDate=2026-12-31&earlyPayoffOnly=false
```

---

## Resumen del ciclo de vida

```
[CRM] ──RabbitMQ──► CustomerCreated ──► customer_credit_profiles
                                                │
                                    POST /members/enroll (explícito)
                                                │
                                        cooperative_members

POST /products                  ← Definir producto (prelación + capital social)
         │
         ▼
POST /loans                     ← Crear contrato (evaluación automática)
         │  status: Approved
         ▼
POST /loans/{id}/disburse       ← Desembolsar
         │  status: Active
         ▼
[InterestAccrualWorker]         ← Devengo diario automático
         │
         ▼
POST /loans/{id}/payments  ─── o ─── POST /payments (async)
         │  (repite N veces)
         ▼
    ┌────┴────────────────┐
    │                     │
    ▼                     ▼
[Pago puntual]       [Pago vencido]
status: Active       status: Delinquent ──► POST /loans/{id}/default
    │                     │                         status: Default
    │                POST /loans/{id}/restructure
    │                         │
    └────────────┬────────────┘
                 ▼
POST /loans/{id}/payoff  ──►  status: PaidOff
```

---

## Configuración de UnderwritingPolicy

La política de suscripción se configura directamente en la base de datos (`underwriting_policies WHERE id = 'default'`):

| Columna | Tipo | Default | Descripción |
|---|---|---|---|
| `base_interest_rate` | DECIMAL | — | Tasa base referencial |
| `auto_default_threshold_days` | INT | 90 | Días de mora para pasar a Default automáticamente |
| `no_score_behavior` | VARCHAR | `approve_with_penalty` | `approve_with_penalty` o `reject` cuando no hay score |
| `shares_multiplier_limit` | INT | 5 | Múltiplo de aportaciones para calcular límite de crédito |
| `require_active_membership` | BOOLEAN | false | Si `true`, exige membresía cooperativa activa |
| `grace_period_days` | INT | 5 | Días de gracia antes de registrar mora |
| `penalty_rate` | NUMERIC | 0 | Tasa de mora (ej. `0.025` = 2.5%) |
| `origination_fee_rate` | NUMERIC | 0 | Comisión de apertura (ej. `0.01` = 1%) |
| `enforce_shares_capacity_limit` | BOOLEAN | **false** | Ver abajo |
| `max_dti_ratio` | NUMERIC | **0.50** | Ratio máximo deuda/ingreso (50%). Usado por `DebtToIncomeRule` y `PaymentCapacityRule` |

### `enforce_shares_capacity_limit`

Controla el comportamiento de la regla de aportaciones (`MemberSharesEvaluation`):

- **`false` (default)**: la regla evalúa y reporta si el monto supera `aportaciones × shares_multiplier_limit`, pero **no bloquea** el préstamo. El resultado aparece en `evaluationResults` con `passed: true` y mensaje `"Shares limit exceeded (informative only): ..."`. Los ingresos y garantías siguen siendo el factor determinante.
- **`true`**: la regla actúa como **hard stop** — si el monto supera el límite de aportaciones, el préstamo se rechaza independientemente de ingresos o garantías.

```sql
-- Activar hard stop por aportaciones
UPDATE underwriting_policies SET enforce_shares_capacity_limit = true WHERE id = 'default';

-- Volver a modo informativo (default)
UPDATE underwriting_policies SET enforce_shares_capacity_limit = false WHERE id = 'default';
```

---

## Tablas de referencia

### PaymentMethod
`ACH` | `Wire` | `Check` | `Card` | `Cash`

### AmortizationMethod
`French` (cuota fija) | `German` (capital fijo) | `American` (bullet) | `Flat` | `InterestOnly`

### RateType
`Fixed` | `Variable` (requiere `spread` y `referenceRateId`)

### GuaranteeType
`Hipoteca` | `Prenda` | `FianzaSolidaria` | `DepositoAPlazo` | `CesionDeDerecho`

> **`coverageRate`**: factor decimal entre 0 y 1 que indica qué porción del valor del bien se reconoce como respaldo. Ej: `0.80` = 80% del avalúo. Cobertura efectiva = `appraisalValue × coverageRate`.

### PaymentComponent (prelación)
`Insurance` | `Fees` | `PenaltyInterest` | `RegularInterest` | `SocialCapital` | `Principal`

### SocialCapitalCalculationType
`FixedAmount` | `PercentageOfPayment` | `PercentageOfPrincipalPaid` | `PercentageOfOriginalAmount`

### SocialCapitalCollectionMode
`IncludedInPayment` — el aporte se descuenta del monto recibido antes de amortizar el préstamo
`SeparateCollection` — el préstamo recibe el monto completo; el aporte se registra como débito separado en la cuenta del socio
