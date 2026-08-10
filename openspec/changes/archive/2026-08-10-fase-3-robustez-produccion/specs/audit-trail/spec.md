## Purpose

Registrar quién ejecutó cada operación financiera relevante, para cumplir con los requerimientos de trazabilidad operativa de una cooperativa supervisada. El audit log es inmutable (append-only) y sirve de evidencia ante revisiones regulatorias.

## Requirements

### Requirement: Header X-User-Id en endpoints de escritura

Los endpoints de escritura financiera SHALL aceptar el header opcional `X-User-Id` (string libre, típicamente el ID del usuario en el sistema de autenticación externo). Los endpoints afectados son: `POST /api/v1/loans` (crear contrato), `POST /api/v1/loans/{loanId}/disburse`, `POST /api/v1/payments/{loanId}/apply`, `POST /api/v1/revolving/{creditId}/payment`, `PUT /api/v1/loans/{loanId}/risk-category`.

#### Scenario: Operación con X-User-Id presente

- **WHEN** se ejecuta una operación financiera con el header `X-User-Id: usr-123`
- **THEN** se registra una entrada en `audit_log` con `user_id = "usr-123"`

#### Scenario: Operación sin X-User-Id

- **WHEN** se ejecuta una operación financiera sin el header `X-User-Id`
- **THEN** se registra la entrada en `audit_log` con `user_id = null`
- **THEN** la operación se procesa normalmente (el header es opcional)

### Requirement: Tabla audit_log append-only

El sistema SHALL registrar cada operación financiera en una tabla `audit_log` con los campos: `id` (UUID), `occurred_at` (timestamptz, default NOW()), `user_id` (varchar, nullable), `action` (varchar, p.ej. `contract.created`, `payment.applied`), `entity_type` (varchar, p.ej. `LoanContract`), `entity_id` (UUID), `details` (jsonb, resumen de la operación). La tabla SHALL no tener operaciones UPDATE ni DELETE — solo INSERT.

#### Scenario: Registro de creación de contrato

- **WHEN** se crea un contrato exitosamente
- **THEN** se inserta en `audit_log`: `action = "contract.created"`, `entity_type = "LoanContract"`, `entity_id = loanId`, `details` con el monto y moneda

#### Scenario: Registro de pago aplicado

- **WHEN** se aplica un pago exitosamente
- **THEN** se inserta en `audit_log`: `action = "payment.applied"`, `entity_type = "LoanContract"`, `entity_id = loanId`, `details` con el monto pagado

#### Scenario: No se registra si la operación falla

- **WHEN** la operación falla (validación, negocio, excepción)
- **THEN** no se inserta ningún registro en `audit_log`

### Requirement: IAuditLogRepository append-only

El sistema SHALL exponer una abstracción `IAuditLogRepository` con un único método `LogAsync(AuditEntry entry, CancellationToken)`. No SHALL existir métodos de lectura, actualización ni borrado en esta interfaz.

#### Scenario: Inserción exitosa no bloquea la respuesta

- **WHEN** el audit log falla al insertar (excepción de DB)
- **THEN** el sistema registra el error en el logger pero NO falla la respuesta al cliente
- **THEN** la operación financiera ya fue procesada y su resultado es el que se retorna
