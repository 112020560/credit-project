## ADDED Requirements

### Requirement: Generación de carta de saldo
El sistema SHALL generar una carta de saldo formal en formato PDF para un préstamo activo, accesible mediante un endpoint GET por `loanId`.

La carta SHALL incluir:
- Nombre de la cooperativa y fecha de emisión del documento
- Nombre e identificación del cliente
- Número del préstamo y fecha de desembolso
- Monto original del préstamo y moneda
- Saldo actual (capital pendiente)
- Tasa de interés vigente (y tipo: fija o variable + spread si aplica)
- Próxima fecha de pago y monto de la cuota
- Fecha de vencimiento del préstamo (última cuota)
- Estado del préstamo (Activo, en Mora, etc.)

#### Scenario: Carta generada exitosamente
- **WHEN** `GET /api/loans/{id}/documents/balance-letter` es invocado con un `loanId` válido
- **THEN** el sistema retorna HTTP 200 con `Content-Type: application/pdf` y `Content-Disposition: attachment; filename="carta-saldo-{loanId}.pdf"`

#### Scenario: Préstamo no encontrado
- **WHEN** el `loanId` no existe en `rm_loan_summaries`
- **THEN** el sistema retorna HTTP 404

#### Scenario: Préstamo con tasa variable
- **WHEN** el préstamo tiene `rate_type = 'Variable'`
- **THEN** la carta indica la tasa efectiva vigente, el spread y el identificador de tasa de referencia

---

### Requirement: Data source de la carta de saldo
El sistema SHALL construir el `BalanceLetterData` exclusivamente desde `rm_loan_summaries` sin rehydratar el aggregate, dado que ese read model contiene el saldo actual, tasa vigente y fechas de pago actualizados por los projectors.

#### Scenario: Datos completos en rm_loan_summaries
- **WHEN** `rm_loan_summaries` contiene registro para el `loanId`
- **THEN** `BalanceLetterData` se construye en una sola consulta SQL sin acceso al event store

#### Scenario: Campos de tasa variable presentes
- **WHEN** `rate_type = 'Variable'` y `spread` y `reference_rate_id` no son nulos
- **THEN** `BalanceLetterData` incluye `Spread` y `ReferenceRateId` para mostrar en la carta
