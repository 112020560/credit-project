## ADDED Requirements

### Requirement: Generación de comprobante de pago
El sistema SHALL generar un comprobante de pago en formato PDF para una transacción de pago aplicada a un préstamo, accesible mediante un endpoint GET autenticado por `loanId` y `paymentId`.

El comprobante SHALL incluir:
- Nombre de la cooperativa
- Fecha y hora del pago
- Nombre e identificación del cliente
- Número del préstamo
- Desglose del pago: capital aplicado, intereses aplicados, comisiones (si aplica)
- Total pagado
- Saldo restante tras el pago
- Número de cuota correspondiente (si aplica)

#### Scenario: Comprobante generado exitosamente
- **WHEN** `GET /api/loans/{id}/documents/payment-receipt/{paymentId}` es invocado con IDs válidos
- **THEN** el sistema retorna HTTP 200 con `Content-Type: application/pdf` y `Content-Disposition: attachment; filename="comprobante-{paymentId}.pdf"`

#### Scenario: Préstamo no encontrado
- **WHEN** el `loanId` no existe en `rm_loan_summaries`
- **THEN** el sistema retorna HTTP 404

#### Scenario: Pago no encontrado
- **WHEN** el `paymentId` no existe en `rm_payment_history` para el préstamo dado
- **THEN** el sistema retorna HTTP 404

---

### Requirement: Data source del comprobante de pago
El sistema SHALL construir el `PaymentReceiptData` combinando datos de `rm_payment_history` y `rm_loan_summaries` sin rehydratar el aggregate.

Campos requeridos de `rm_payment_history`: `payment_id`, `loan_id`, `payment_date`, `amount_paid`, `principal_applied`, `interest_applied`, `fees_applied`, `remaining_balance`.

Campos requeridos de `rm_loan_summaries`: `customer_name`, `loan_number` (o `id`), `currency`.

#### Scenario: Datos completos disponibles en read models
- **WHEN** ambos read models contienen registros para el `loanId` y `paymentId` dados
- **THEN** el `PaymentReceiptData` se construye sin consultar el event store

#### Scenario: Campo fees_applied nulo
- **WHEN** `fees_applied` es null o cero en `rm_payment_history`
- **THEN** el comprobante omite la línea de comisiones sin error
