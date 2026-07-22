# E2E Test: Línea de Crédito Revolvente

Flujo completo desde la creación hasta el primer retiro de fondos y ciclo de pagos.

## Pre-requisito

El `CustomerReference` ya debe existir en el sistema (creado vía evento `CustomerCreated` de RabbitMQ o por el consumer). Se necesita el `externalCustomerId` (GUID del CRM).

---

## Flujo de estados

```
Pending --> Active --> (Frozen) --> Active --> Closed
              |
           DrawFunds (N veces mientras haya crédito disponible)
```

---

## Paso 1 — Crear la línea de crédito

**No pasa por el motor de reglas.** La aprobación es implícita al crear.

```
POST /api/v1/revolving-credits
Content-Type: application/json
```

```json
{
  "externalCustomerId": "{{customerId}}",
  "creditLimit": 10000.00,
  "currency": "USD",
  "interestRate": 18.5,
  "minimumPaymentPercentage": 5,
  "minimumPaymentAmount": 25.00,
  "billingCycleDay": 15,
  "gracePeriodDays": 20
}
```

**Campos:**

| Campo | Tipo | Requerido | Descripcion |
|-------|------|-----------|-------------|
| `externalCustomerId` | GUID | Si | ID del cliente en el CRM |
| `creditLimit` | decimal | Si | Limite de credito total |
| `currency` | string | No | Default: `"USD"` |
| `interestRate` | decimal | No | Tasa anual en %. Si no se envia, se usa la configurada en el handler |
| `minimumPaymentPercentage` | decimal | No | Default: `5` (5% del saldo) |
| `minimumPaymentAmount` | decimal | No | Default: `25.00` (pago minimo absoluto) |
| `billingCycleDay` | int | No | Dia del mes para corte. Default: `15`. Rango: 1-28 |
| `gracePeriodDays` | int | No | Dias de gracia tras el corte. Default: `20` |

**Respuesta exitosa `201 Created`:**

```json
{
  "success": true,
  "creditLineId": "{{creditLineId}}",
  "message": "Credit line created successfully"
}
```

> Guardar `creditLineId` para los siguientes pasos.

**Estado resultante:** `Pending`
**Evento generado:** `CreditLineCreated`

---

## Paso 2 — Activar la línea de crédito

```
POST /api/v1/revolving-credits/{{creditLineId}}/activate
```

Sin body.

**Respuesta exitosa `200 OK`:**

```json
{
  "success": true,
  "creditLineId": "{{creditLineId}}",
  "activatedAt": "2026-06-16T10:00:00Z",
  "nextStatementDate": "2026-07-15T00:00:00Z"
}
```

> `nextStatementDate` se calcula a partir del `billingCycleDay` configurado en la creacion.

**Estado resultante:** `Active`
**Evento generado:** `CreditLineActivated`

---

## Paso 3 — Disponer fondos (DrawFunds)

Equivalente al "desembolso". Se puede llamar multiples veces mientras `availableCredit > 0`.

```
POST /api/v1/revolving-credits/{{creditLineId}}/draw
Content-Type: application/json
```

```json
{
  "amount": 2500.00,
  "currency": "USD",
  "description": "Compra de inventario"
}
```

**Validaciones del dominio:**
- La linea debe estar en estado `Active`
- `amount > 0`
- `amount <= availableCredit`

**Respuesta exitosa `200 OK`:**

```json
{
  "success": true,
  "drawId": "{{drawId}}",
  "amount": 2500.00,
  "newBalance": 2500.00,
  "availableCredit": 7500.00,
  "drawnAt": "2026-06-16T10:05:00Z"
}
```

**Estado resultante:** `Active` (sin cambio de estado)
**Evento generado:** `FundsDrawn`

---

## Paso 4 — Consultar estado de la linea

```
GET /api/v1/revolving-credits/{{creditLineId}}
```

**Respuesta `200 OK`:**

```json
{
  "creditLineId": "{{creditLineId}}",
  "status": "Active",
  "creditLimit": 10000.00,
  "currentBalance": 2500.00,
  "availableCredit": 7500.00,
  "accruedInterest": 0.00,
  "pendingFees": 0.00,
  "interestRate": 18.5,
  "nextStatementDate": "2026-07-15T00:00:00Z",
  "minimumPaymentPercentage": 5,
  "minimumPaymentAmount": 25.00
}
```

---

## Paso 5 — Aplicar pago (sincrono)

Orden de aplicacion del pago: **fees → intereses acumulados → principal**.

```
POST /api/v1/revolving-credits/{{creditLineId}}/payments
Content-Type: application/json
```

```json
{
  "amount": 500.00,
  "currency": "USD",
  "paymentMethod": "ACH"
}
```

**Valores validos para `paymentMethod`:** `ACH`, `Wire`, `Check`, `Card`, `Cash`

**Respuesta exitosa `200 OK`:**

```json
{
  "success": true,
  "paymentId": "{{paymentId}}",
  "totalAmount": 500.00,
  "feesPaid": 0.00,
  "interestPaid": 0.00,
  "principalPaid": 500.00,
  "newBalance": 2000.00,
  "availableCredit": 8000.00
}
```

**Evento generado:** `RevolvingPaymentApplied`

> Si el pago es igual o mayor al pago minimo y la linea estaba en `Frozen`, se desbloquea automaticamente a `Active`.

---

## Paso 5b — Pago asincrono (via outbox)

Para pagos que se procesan a traves del outbox/RabbitMQ:

```
POST /api/v1/payments/revolving
Content-Type: application/json
```

```json
{
  "creditLineId": "{{creditLineId}}",
  "customerId": "{{internalCustomerId}}",
  "amount": 500.00,
  "currency": "USD",
  "paymentMethod": "ACH"
}
```

> Nota: `customerId` aqui es el ID **interno** del sistema, no el `externalCustomerId` del CRM.

**Respuesta `202 Accepted`:**

```json
{
  "isAccepted": true,
  "paymentId": "{{paymentId}}",
  "trackingUrl": "/api/v1/payments/{{paymentId}}/status"
}
```

**Consultar estado del pago:**

```
GET /api/v1/payments/{{paymentId}}/status
```

---

## Flujos adicionales

### Segundo retiro

```
POST /api/v1/revolving-credits/{{creditLineId}}/draw
```
```json
{
  "amount": 3000.00,
  "currency": "USD",
  "description": "Segundo retiro"
}
```

### Congelar linea

```
POST /api/v1/revolving-credits/{{creditLineId}}/freeze
```
```json
{
  "reason": "Pago minimo no recibido"
}
```

Estado → `Frozen`. Los DrawFunds quedan bloqueados. Los pagos siguen siendo aceptados.

### Descongelar linea

```
POST /api/v1/revolving-credits/{{creditLineId}}/unfreeze
```
```json
{
  "reason": "Pago recibido"
}
```

Estado → `Active`.

### Cambiar limite de credito

```
POST /api/v1/revolving-credits/{{creditLineId}}/change-limit
```
```json
{
  "newLimit": 15000.00,
  "currency": "USD",
  "reason": "Ampliacion por buen comportamiento"
}
```

> El nuevo limite no puede ser menor al saldo actual.

### Cerrar linea

```
POST /api/v1/revolving-credits/{{creditLineId}}/close
```
```json
{
  "reason": "Solicitud del cliente"
}
```

> Solo se puede cerrar si el saldo es `0`. Estado → `Closed`.

### Ver transacciones

```
GET /api/v1/revolving-credits/{{creditLineId}}/transactions?limit=50
```

### Ver estados de cuenta

```
GET /api/v1/revolving-credits/{{creditLineId}}/statements
```

### Ver lineas por cliente

```
GET /api/v1/revolving-credits/customer/{{externalCustomerId}}
```

---

## Resumen del flujo principal

```
[CustomerReference existe]
         |
         v
POST /api/v1/revolving-credits
    --> Estado: Pending
         |
         v
POST /api/v1/revolving-credits/{id}/activate
    --> Estado: Active
         |
         v
POST /api/v1/revolving-credits/{id}/draw
    --> balance sube, availableCredit baja
         |
         v
POST /api/v1/revolving-credits/{id}/payments
    --> balance baja, availableCredit sube
         |
       (ciclo mensual: el worker genera statements automaticamente)
```

---

## Webhooks (opcional)

Para recibir notificaciones cuando se procesa un pago:

```
POST /api/v1/webhooks/subscribe
```
```json
{
  "customerId": "{{internalCustomerId}}",
  "eventType": "revolving_payment.completed",
  "callbackUrl": "https://mi-sistema.com/webhooks/credito",
  "secretKey": "mi-clave-secreta-de-32-caracteres-minimo"
}
```

**Tipos de evento disponibles:** `payment.completed`, `payment.failed`, `payment.rejected`, `revolving_payment.completed`, `revolving_payment.failed`, `loan.paid_off`
