## MODIFIED Requirements

### Requirement: Validación de entrada al crear un contrato
El sistema SHALL rechazar solicitudes de creación de contrato que no cumplan con los parámetros mínimos de entrada antes de ejecutar cualquier evaluación crediticia.

Parámetros requeridos:
- `ExternalCustomerId`: GUID no vacío del cliente en el CRM
- `Amount`: decimal > 0 y <= 1,000,000
- `Currency`: uno de `USD`, `EUR`, `CRC`
- `TermMonths`: entero entre 1 y 360 (inclusive)
- `CollateralValue`: si se provee, MUST ser > 0
- `AmortizationMethod`: valor válido del enum (default: `French`)
- `RateType`: `"Fixed"` o `"Variable"` (default: `"Fixed"`)
- `Spread`: decimal >= 0, requerido si `RateType = "Variable"`
- `ReferenceRateId`: string, requerido si `RateType = "Variable"`, MUST existir en `reference_rates`

#### Scenario: Monto fuera de rango rechazado en validación
- **WHEN** se envía `Amount = 0` o `Amount > 1,000,000`
- **THEN** el sistema retorna error de validación sin ejecutar el motor de reglas

#### Scenario: Moneda no soportada rechazada
- **WHEN** se envía `Currency = "MXN"` u otra no listada
- **THEN** el sistema retorna error de validación indicando las monedas permitidas

#### Scenario: Plazo fuera de rango rechazado
- **WHEN** `TermMonths < 1` o `TermMonths > 360`
- **THEN** el sistema retorna error de validación

#### Scenario: Colateral con valor negativo rechazado
- **WHEN** se provee `CollateralValue <= 0`
- **THEN** el sistema retorna error de validación

#### Scenario: Variable sin Spread rechazado
- **WHEN** se envía `RateType = "Variable"` sin `Spread`
- **THEN** el sistema retorna error de validación indicando que Spread es requerido para tasa variable

#### Scenario: Variable con ReferenceRateId inexistente rechazado
- **WHEN** se envía `ReferenceRateId` que no existe en la tabla `reference_rates`
- **THEN** el sistema retorna error de validación
