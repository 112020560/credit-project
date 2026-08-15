## ADDED Requirements

### Requirement: Listar políticas de underwriting disponibles
El sistema SHALL exponer un endpoint de solo lectura `GET /api/v1/underwriting-policies` que retorna todas las filas de la tabla `underwriting_policies`. Este endpoint permite al operador conocer qué IDs existen antes de asignar una policy a un producto financiero.

El endpoint NO requiere autenticación diferenciada — es información de configuración de negocio, no de clientes.

La respuesta incluye por cada política: `id`, `baseInterestRate`, `maxDtiRatio`, `sharesMultiplierLimit`, `enforceSharesCapacityLimit`, `requireActiveMembership`, `noScoreBehavior`, `gracePeriodDays`, `penaltyRate`, `originationFeeRate`, `autoDefaultThresholdDays`.

#### Scenario: Listado exitoso
- **WHEN** `GET /api/v1/underwriting-policies` es invocado
- **THEN** el sistema retorna HTTP 200 con array JSON de todas las políticas registradas
- **THEN** cada elemento incluye el campo `id` (ej: `"default"`) y todos los parámetros configurables

#### Scenario: Al menos una política siempre existe
- **WHEN** `GET /api/v1/underwriting-policies` es invocado en un sistema correctamente inicializado
- **THEN** el array contiene al menos un elemento con `id = "default"`

#### Scenario: Sin paginación
- **WHEN** el sistema tiene múltiples políticas registradas
- **THEN** el endpoint retorna todas en un único array sin cursor ni `page` params
