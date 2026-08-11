## ADDED Requirements

### Requirement: Endpoints de descarga de documentos del préstamo
El sistema SHALL exponer tres endpoints de descarga de documentos bajo el recurso de préstamo, que retornen el archivo binario correspondiente con los headers HTTP correctos.

Endpoints:
- `GET /api/loans/{id}/documents/payment-receipt/{paymentId}` → comprobante de pago PDF
- `GET /api/loans/{id}/documents/balance-letter` → carta de saldo PDF
- `GET /api/loans/{id}/documents/amortization-table?format=pdf|xlsx` → tabla de amortización

#### Scenario: Descarga con headers correctos
- **WHEN** cualquiera de los tres endpoints retorna exitosamente
- **THEN** la respuesta incluye `Content-Disposition: attachment; filename="<nombre-descriptivo>"` y el `Content-Type` correspondiente al formato

#### Scenario: Préstamo no pertenece al recurso solicitado
- **WHEN** el `loanId` del path no coincide con el `paymentId` solicitado (pago de otro préstamo)
- **THEN** el sistema retorna HTTP 404

#### Scenario: Formato no soportado en tabla de amortización
- **WHEN** el parámetro `format` no es `pdf` ni `xlsx`
- **THEN** el sistema retorna HTTP 400 con mensaje de formatos válidos
