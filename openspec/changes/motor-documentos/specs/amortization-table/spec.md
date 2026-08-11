## ADDED Requirements

### Requirement: Generación de tabla de amortización exportable
El sistema SHALL generar la tabla de amortización completa de un préstamo en formato PDF o Excel, accesible mediante un endpoint GET con parámetro `format`.

La tabla SHALL incluir por cada cuota:
- Número de cuota
- Fecha de vencimiento
- Monto total de la cuota
- Interés de la cuota
- Capital de la cuota
- Saldo restante tras el pago

Adicionalmente SHALL mostrar una fila de totales al final (suma de capital, intereses y pagos totales).

#### Scenario: Tabla en PDF generada exitosamente
- **WHEN** `GET /api/loans/{id}/documents/amortization-table?format=pdf` es invocado con un `loanId` válido
- **THEN** el sistema retorna HTTP 200 con `Content-Type: application/pdf` y `Content-Disposition: attachment; filename="tabla-amortizacion-{loanId}.pdf"`

#### Scenario: Tabla en Excel generada exitosamente
- **WHEN** `GET /api/loans/{id}/documents/amortization-table?format=xlsx` es invocado con un `loanId` válido
- **THEN** el sistema retorna HTTP 200 con `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` y `Content-Disposition: attachment; filename="tabla-amortizacion-{loanId}.xlsx"`

#### Scenario: Formato inválido
- **WHEN** `format` no es `pdf` ni `xlsx`
- **THEN** el sistema retorna HTTP 400 con mensaje indicando los formatos permitidos

#### Scenario: Préstamo no encontrado
- **WHEN** el `loanId` no existe
- **THEN** el sistema retorna HTTP 404

---

### Requirement: Data source de la tabla de amortización
El sistema SHALL obtener el schedule de amortización rehydratando el `LoanContractAggregate` desde el event store, de modo que el schedule siempre refleje el estado actual (incluyendo reestructuraciones y reajustes de tasa).

#### Scenario: Schedule obtenido desde aggregate rehydratado
- **WHEN** se solicita la tabla de amortización de un préstamo con reestructuraciones previas
- **THEN** el schedule retornado corresponde al último `State.PaymentSchedule` del aggregate (post-reestructuración)

#### Scenario: Préstamo sin pagos realizados
- **WHEN** el préstamo está activo pero sin ningún pago aplicado
- **THEN** la tabla muestra el schedule completo original con todas las cuotas pendientes

#### Scenario: Préstamo con pagos parciales realizados
- **WHEN** el préstamo tiene pagos aplicados
- **THEN** la tabla muestra el schedule completo (cuotas pagadas y pendientes) tal como está en `State.PaymentSchedule`
