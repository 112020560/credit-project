## Purpose

Prevenir el doble procesamiento de pagos cuando el cliente reintenta una solicitud (timeout de red, fallo transitorio). El sistema almacena la respuesta del primer procesamiento exitoso y la devuelve idéntica en reintentos con la misma clave.

## Requirements

### Requirement: Header Idempotency-Key en endpoints de pago

Los endpoints `POST /api/v1/payments/{loanId}/apply` y `POST /api/v1/revolving/{creditId}/payment` SHALL aceptar el header opcional `Idempotency-Key` (UUID v4). Si el header está presente, el sistema verifica si ya existe una respuesta almacenada para esa clave antes de procesar el pago.

#### Scenario: Primera solicitud con Idempotency-Key nueva

- **WHEN** se recibe un pago con un `Idempotency-Key` que no existe en el sistema
- **THEN** el pago se procesa normalmente
- **THEN** la respuesta (status code + body) se almacena asociada a esa clave con TTL de 24 horas
- **THEN** se retorna la respuesta del procesamiento

#### Scenario: Solicitud duplicada con Idempotency-Key existente

- **WHEN** se recibe un pago con un `Idempotency-Key` que ya existe y no expiró
- **THEN** el sistema NO procesa el pago nuevamente
- **THEN** se retorna la respuesta almacenada idéntica (mismo status code y body)
- **THEN** se incluye el header `Idempotency-Replayed: true` en la respuesta

#### Scenario: Solicitud sin Idempotency-Key

- **WHEN** se recibe un pago sin el header `Idempotency-Key`
- **THEN** el pago se procesa normalmente sin guardar ningún resultado de idempotencia

### Requirement: Expiración de claves de idempotencia

El sistema SHALL eliminar las claves de idempotencia que superen las 24 horas de antigüedad. Una vez expirada la clave, una nueva solicitud con ese mismo UUID es tratada como primera solicitud.

#### Scenario: Clave expirada tratada como nueva

- **WHEN** se recibe una solicitud con un `Idempotency-Key` cuya entrada tiene más de 24 horas
- **THEN** el sistema procesa el pago normalmente
- **THEN** almacena la nueva respuesta sobreescribiendo o reinsertando la entrada

#### Scenario: Limpieza automática de claves expiradas

- **WHEN** corre el proceso de limpieza (o la propia consulta de lookup filtra por expiración)
- **THEN** las entradas con `expires_at < NOW()` no se consideran válidas
