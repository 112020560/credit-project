## ADDED Requirements

### Requirement: Consulta de tasa de referencia vigente
El sistema SHALL exponer un endpoint `GET /api/reference-rates/{id}` que retorne la tasa de referencia con su valor actual, fecha de vigencia y fuente.

#### Scenario: Tasa de referencia encontrada
- **WHEN** se solicita `GET /api/reference-rates/TBP_CRC`
- **THEN** el sistema retorna 200 con `{ id, name, currentValue, effectiveDate, source }`

#### Scenario: Tasa de referencia no encontrada
- **WHEN** se solicita un `id` que no existe
- **THEN** el sistema retorna 404

---

### Requirement: Actualización manual de tasa de referencia
El sistema SHALL exponer un endpoint `PUT /api/reference-rates/{id}` que permita actualizar el valor de una tasa de referencia existente o crear una nueva. Este endpoint está preparado para restricción por rol admin cuando se implemente autenticación JWT.

#### Scenario: Actualización exitosa de TBP
- **WHEN** se envía `PUT /api/reference-rates/TBP_CRC` con `{ currentValue: 5.00, effectiveDate: "2026-09-01", source: "BCCR" }`
- **THEN** el sistema persiste el nuevo valor con `updated_at = now()`
- **THEN** retorna 200 con la entidad actualizada

#### Scenario: Valor de tasa inválido rechazado
- **WHEN** se envía `currentValue < 0` o `currentValue > 100`
- **THEN** el sistema retorna 400 con error de validación

---

### Requirement: Persistencia de tasas de referencia
El sistema SHALL mantener la tabla `reference_rates` como fuente de verdad para las tasas de referencia externas. Cada fila representa una tasa de referencia identificada por un código único (ej: `TBP_CRC`, `PRIME_USD`).

#### Scenario: Datos mínimos de una tasa de referencia
- **WHEN** se crea una nueva tasa de referencia
- **THEN** el sistema almacena: `id` (PK string), `name`, `current_value`, `effective_date`, `source`, `updated_at`
