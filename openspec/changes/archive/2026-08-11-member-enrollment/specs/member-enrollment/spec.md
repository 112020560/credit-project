## ADDED Requirements

### Requirement: Enrolamiento explícito de socio cooperativo
El sistema SHALL permitir enrolar un cliente existente como socio cooperativo mediante una acción deliberada. El cliente MUST existir en `customer_credit_profiles` antes de poder ser enrolado. El sistema MUST verificar que el cliente no sea ya un socio activo (idempotencia). El enrolamiento SHALL generar un número de socio único gestionado por el CreditSystem.

#### Scenario: Enrolamiento exitoso
- **WHEN** se recibe `POST /api/v1/members/enroll` con un `externalCustomerId` que existe en `customer_credit_profiles` y no tiene registro en `cooperative_members`
- **THEN** el sistema genera un número de socio único, crea el `CooperativeMemberAggregate`, persiste el registro y retorna `201 Created` con `{ memberId, memberNumber, externalCustomerId, joinedAt }`

#### Scenario: Cliente no existe
- **WHEN** se recibe `POST /api/v1/members/enroll` con un `externalCustomerId` que NO existe en `customer_credit_profiles`
- **THEN** el sistema retorna `400 Bad Request` con mensaje "Customer not found. The customer must be registered in the credit system before enrollment."

#### Scenario: Ya es socio
- **WHEN** se recibe `POST /api/v1/members/enroll` con un `externalCustomerId` que ya tiene un registro en `cooperative_members`
- **THEN** el sistema retorna `400 Bad Request` con mensaje "Customer is already a cooperative member."

#### Scenario: Datos mínimos requeridos
- **WHEN** se recibe `POST /api/v1/members/enroll` sin `joinedAt` o `initialSharesAmount`
- **THEN** el sistema retorna `400 Bad Request` con los errores de validación correspondientes

### Requirement: El CRM no es fuente del número de socio
El sistema SHALL rechazar cualquier flujo en el que el número de socio provenga del CRM. El número de socio MUST ser generado internamente por el CreditSystem al momento del enrolamiento.

#### Scenario: Eliminación del flujo MemberSynced
- **WHEN** el sistema está en operación
- **THEN** no existe ningún consumer ni handler que acepte un número de socio desde el CRM como fuente autoritativa para crear socios cooperativos
