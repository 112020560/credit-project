# E2E Test: Credito Flat (Loan Contract)

Flujo completo desde la evaluacion del motor de reglas hasta el desembolso y ciclo de pagos. Incluye los 5 tipos de amortizacion disponibles.

## Pre-requisito

El `CustomerReference` ya debe existir en el sistema con los datos necesarios para el motor de reglas:
- `creditScore` (opcional pero recomendado)
- `monthlyIncome` (opcional pero recomendado)
- `monthlyDebt` (opcional)

Se necesita el `externalCustomerId` (GUID del CRM).

---

## Flujo de estados

```
Draft/Approved --> Active --> Delinquent --> Default --> WrittenOff
                    |              |
                    |         Restructured --> Active
                    |
                  PaidOff
```

> Al crear el contrato, si el motor lo aprueba, el estado inicial es `Approved`.
> El desembolso (`Disburse`) lo lleva a `Active`.

---

## Motor de reglas (ContractEngine)

Antes de crear el contrato, el sistema evalua las siguientes reglas en orden de prioridad:

| Prioridad | Regla | Tipo | Criterio |
|-----------|-------|------|----------|
| 0 | `MaxLoanAmountValidation` | HardStop | Monto <= $500,000 Y monto <= 5x ingreso anual |
| 1 | `CreditScoreEvaluation` | HardStop | Score >= 500 (si se tiene score) |
| 2 | `DebtToIncomeRatio` | Soft | DTI total <= 50% |
| 3 | `CollateralEvaluation` | Soft | Colateral recomendado >= 120% del monto |
| 4 | `ActiveLoansCheck` | Soft | Penalizacion de tasa si ya tiene prestamos activos |

**Las reglas HardStop abortan la evaluacion inmediatamente si fallan.**

### Ajuste de tasa segun score

La tasa final = **8% base** + ajuste por reglas:

| Score | Ajuste |
|-------|--------|
| >= 750 | +0% |
| 700-749 | +1.5% |
| 650-699 | +3% |
| 600-649 | +5% |
| 550-599 | +8% |
| 500-549 | +12% |

Otros ajustes:
- DTI entre 40%-50%: **+2%**
- Sin colateral: **+1%**
- Colateral parcial (< 100% del monto): **+0.5%**
- Colateral >= 120%: **-1.5%**
- Tiene prestamos activos: **+1%**

---

## Tipos de amortizacion

| Valor enum | Nombre | Descripcion |
|------------|--------|-------------|
| `French` | Frances (default) | Cuota fija mensual. Capital crece, interes decrece |
| `German` | Aleman | Capital fijo por cuota, interes sobre saldo. Cuota decreciente |
| `American` | Americano | Solo intereses mensualmente + bullet de capital al final |
| `Flat` | Flat | Interes calculado sobre el capital inicial (no sobre saldo). Cuota fija simple |
| `InterestOnly` | Solo intereses | Paga solo intereses; el capital se paga en una sola cuota final |

---

## Paso 1 — Crear el contrato

```
POST /api/v1/loans
Content-Type: application/json
```

### Escenario A: Prestamo Frances (cuota fija)

```json
{
  "externalCustomerId": "{{customerId}}",
  "amount": 20000.00,
  "currency": "USD",
  "termMonths": 24,
  "collateralValue": 25000.00,
  "amortizationMethod": 0
}
```

### Escenario B: Prestamo Aleman (capital fijo)

```json
{
  "externalCustomerId": "{{customerId}}",
  "amount": 20000.00,
  "currency": "USD",
  "termMonths": 24,
  "collateralValue": 25000.00,
  "amortizationMethod": 1
}
```

### Escenario C: Prestamo Americano (bullet)

```json
{
  "externalCustomerId": "{{customerId}}",
  "amount": 20000.00,
  "currency": "USD",
  "termMonths": 12,
  "collateralValue": 25000.00,
  "amortizationMethod": 2
}
```

### Escenario D: Prestamo Flat (interes sobre capital inicial)

```json
{
  "externalCustomerId": "{{customerId}}",
  "amount": 20000.00,
  "currency": "USD",
  "termMonths": 12,
  "amortizationMethod": 3
}
```

### Escenario E: Solo intereses (Interest Only)

```json
{
  "externalCustomerId": "{{customerId}}",
  "amount": 20000.00,
  "currency": "USD",
  "termMonths": 12,
  "collateralValue": 30000.00,
  "amortizationMethod": 4
}
```

**Campos:**

| Campo | Tipo | Requerido | Descripcion |
|-------|------|-----------|-------------|
| `externalCustomerId` | GUID | Si | ID del cliente en el CRM |
| `amount` | decimal | Si | Monto solicitado. Max: $500,000 |
| `currency` | string | No | Default: `"USD"` |
| `termMonths` | int | Si | Plazo en meses |
| `collateralValue` | decimal | No | Valor del colateral. Si >= 120% del monto, reduce la tasa 1.5% |
| `amortizationMethod` | int | No | Default: `0` (French). Ver tabla de tipos |

**Valores del enum `amortizationMethod`:** `0=French`, `1=German`, `2=American`, `3=Flat`, `4=InterestOnly`

**Respuesta exitosa `201 Created`:**

```json
{
  "success": true,
  "contractId": "{{loanId}}",
  "interestRate": 9.5,
  "evaluationResults": [
    {
      "ruleName": "MaxLoanAmountValidation",
      "passed": true,
      "message": "Requested amount $20,000.00 is within acceptable limits"
    },
    {
      "ruleName": "CreditScoreEvaluation",
      "passed": true,
      "message": "Credit score 720 approved with rate adjustment of 1.5%"
    },
    {
      "ruleName": "CollateralEvaluation",
      "passed": true,
      "message": "Collateral ratio 125.00% provides security for the loan"
    }
  ]
}
```

**Respuesta rechazada `400 Bad Request`:**

```json
{
  "title": "Contract Creation Failed",
  "detail": "Contract was rejected by the rules engine",
  "status": 400,
  "errors": [
    {
      "ruleName": "CreditScoreEvaluation",
      "passed": false,
      "message": "Credit score 450 is below minimum threshold of 500"
    }
  ]
}
```

> Guardar `contractId` para los siguientes pasos.

**Estado resultante:** `Approved`
**Evento generado:** `LoanContractCreated`

---

## Paso 2 — Desembolsar el prestamo

```
POST /api/v1/loans/{{loanId}}/disburse
Content-Type: application/json
```

```json
{
  "disbursementMethod": "WIRE",
  "destinationAccount": "012345678901"
}
```

**Valores validos para `disbursementMethod`:** `WIRE`, `ACH`, `CHECK`

**Respuesta exitosa `200 OK`:**

```json
{
  "success": true,
  "loanId": "{{loanId}}",
  "disbursedAt": "2026-06-16T10:00:00Z",
  "disbursementMethod": "WIRE",
  "destinationAccount": "012345678901"
}
```

**Estado resultante:** `Active`
**Evento generado:** `LoanDisbursed`

---

## Paso 3 — Consultar resumen del prestamo

```
GET /api/v1/loans/{{loanId}}
```

**Respuesta `200 OK`:**

```json
{
  "loanId": "{{loanId}}",
  "status": "Active",
  "principal": 20000.00,
  "currency": "USD",
  "interestRate": 9.5,
  "termMonths": 24,
  "amortizationMethod": "French",
  "outstandingBalance": 20000.00,
  "nextPaymentDate": "2026-07-16T00:00:00Z",
  "nextPaymentAmount": 916.00
}
```

---

## Paso 4 — Aplicar pago mensual (sincrono)

Orden de aplicacion del pago: **fees → intereses acumulados → principal**.

```
POST /api/v1/loans/{{loanId}}/payments
Content-Type: application/json
```

```json
{
  "amount": 916.00,
  "currency": "USD",
  "paymentMethod": "ACH",
  "referenceNumber": "REF-2026-001"
}
```

**Valores validos para `paymentMethod`:** `ACH`, `Wire`, `Check`, `Card`, `Cash`

**Respuesta exitosa `200 OK`:**

```json
{
  "success": true,
  "paymentId": "{{paymentId}}",
  "amount": 916.00,
  "feesPaid": 0.00,
  "interestPaid": 158.33,
  "principalPaid": 757.67,
  "outstandingBalance": 19242.33,
  "paymentDate": "2026-07-16T00:00:00Z"
}
```

**Evento generado:** `PaymentApplied`

---

## Paso 4b — Pago asincrono (via outbox)

```
POST /api/v1/payments
Content-Type: application/json
```

```json
{
  "loanId": "{{loanId}}",
  "customerId": "{{internalCustomerId}}",
  "amount": 916.00,
  "currency": "USD",
  "paymentMethod": "ACH"
}
```

> Nota: `customerId` es el ID **interno** del sistema.

**Respuesta `202 Accepted`:**

```json
{
  "isAccepted": true,
  "paymentId": "{{paymentId}}",
  "trackingUrl": "/api/v1/payments/{{paymentId}}/status"
}
```

**Consultar estado:**

```
GET /api/v1/payments/{{paymentId}}/status
```

---

## Flujos adicionales

### Ver historial de pagos

```
GET /api/v1/loans/{{loanId}}/payments
```

### Obtener monto de liquidacion anticipada

```
GET /api/v1/loans/{{loanId}}/payoff-amount
GET /api/v1/loans/{{loanId}}/payoff-amount?asOfDate=2026-09-01
```

### Liquidar el prestamo (payoff completo)

```
POST /api/v1/loans/{{loanId}}/payoff
```
```json
{
  "paymentMethod": "Wire",
  "referenceNumber": "PAYOFF-2026-001"
}
```

Estado → `PaidOff`

### Marcar como default

```
POST /api/v1/loans/{{loanId}}/default
```
```json
{
  "reason": "90 dias sin pago"
}
```

Estado → `Default`

### Reestructurar prestamo

Disponible para prestamos en estado `Delinquent` o `Default`.

```
POST /api/v1/loans/{{loanId}}/restructure
```
```json
{
  "newInterestRate": 6.0,
  "newTermMonths": 36,
  "forgiveAmount": 500.00,
  "reason": "Acuerdo de pago por dificultad economica"
}
```

Estado → `Active` (o `Restructured`)

### Ver historial de reestructuraciones

```
GET /api/v1/loans/{{loanId}}/restructure-history
```

### Ver prestamos por cliente

```
GET /api/v1/loans/customer/{{externalCustomerId}}
```

### Listar prestamos morosos

```
GET /api/v1/delinquent-loans
GET /api/v1/delinquent-loans?minDaysOverdue=30&collectionStatus=InProgress
GET /api/v1/delinquent-loans/{{loanId}}
```

### Listar prestamos en default

```
GET /api/v1/loans/defaulted
GET /api/v1/loans/defaulted?fromDate=2026-01-01&toDate=2026-06-30
```

### Listar prestamos pagados

```
GET /api/v1/loans/paid-off
GET /api/v1/loans/paid-off?earlyPayoffOnly=true
```

---

## Resumen del flujo principal

```
[CustomerReference existe]
         |
         v
POST /api/v1/loans
    --> Motor de reglas evalua: MaxLoan, CreditScore, DTI, Collateral, ActiveLoans
    --> Si aprobado: Estado = Approved, tasa calculada automaticamente
         |
         v
POST /api/v1/loans/{id}/disburse
    --> Estado: Active
    --> Se genera el PaymentSchedule segun metodo de amortizacion
         |
         v
POST /api/v1/loans/{id}/payments  (mensualmente)
    --> fees --> intereses --> principal
         |
         v
     (si se acumulan pagos perdidos: workers marcan Delinquent/Default)
         |
         v
POST /api/v1/loans/{id}/payoff   (cuando el saldo es 0)
    --> Estado: PaidOff
```

---

## Comportamiento por tipo de amortizacion

### French (0) — Cuota fija

Cada cuota es igual. Al inicio se paga mas interes, al final mas capital. Ideal para planificacion del cliente.

```
Cuota 1:  Interes = $158.33, Capital = $757.67  --> Saldo: $19,242.33
Cuota 2:  Interes = $152.34, Capital = $763.66  --> Saldo: $18,478.67
...
Cuota 24: Interes = $7.17,   Capital = $908.83  --> Saldo: $0
```

### German (1) — Capital fijo

El capital que se amortiza es fijo cada mes. El interes va decreciendo. La cuota inicial es mas alta y va bajando.

```
Capital fijo por cuota = $20,000 / 24 = $833.33
Cuota 1:  Interes = $158.33, Capital = $833.33, Total = $991.67
Cuota 2:  Interes = $151.74, Capital = $833.33, Total = $985.07
...
Cuota 24: cuota minima
```

### American (2) — Bullet

Paga solo intereses durante el plazo. El capital completo se paga en la ultima cuota.

```
Cuotas 1-11: Solo interes = $158.33/mes
Cuota 12:    Interes + $20,000 capital = $20,158.33
```

### Flat (3) — Interes sobre capital inicial

El interes se calcula siempre sobre el capital original (no sobre el saldo). Cuota fija y simple.

```
Interes total = $20,000 * 9.5% * (12/12) = $1,900
Cuota mensual = ($20,000 + $1,900) / 12 = $1,825.00
```

### InterestOnly (4) — Solo intereses

Similar al Americano. Paga solo intereses durante todos los periodos, capital al vencimiento.

```
Cuotas 1-11: $158.33/mes
Cuota 12:    $158.33 + $20,000 = $20,158.33
```

---

## Webhooks (opcional)

```
POST /api/v1/webhooks/subscribe
```
```json
{
  "customerId": "{{internalCustomerId}}",
  "eventType": "payment.completed",
  "callbackUrl": "https://mi-sistema.com/webhooks/pagos",
  "secretKey": "mi-clave-secreta-de-32-caracteres-minimo"
}
```

**Tipos de evento disponibles:** `payment.completed`, `payment.failed`, `payment.rejected`, `revolving_payment.completed`, `revolving_payment.failed`, `loan.paid_off`
