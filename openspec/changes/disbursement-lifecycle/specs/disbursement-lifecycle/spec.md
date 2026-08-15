# Spec: Disbursement Lifecycle

Ciclo de vida completo del desembolso de préstamos con estado intermedio `Disbursing`, confirmación manual por el oficial de crédito, registro de fallos y read model de desembolsos pendientes. Diseñado para operar manualmente hoy y con integración ACH/SINPE en el futuro sin cambios de dominio.

---

## ADDED Requirements

### Requirement: Estado intermedio Disbursing

El sistema SHALL introducir el estado `Disbursing` en el ciclo de vida de `LoanContractAggregate`, ubicado entre `Approved` y `Active`. Un préstamo en `Disbursing` tiene una instrucción de desembolso registrada pero aún no confirmada por el destinatario ni por el proveedor de pagos.

Transiciones válidas desde `Disbursing`:
- `ConfirmDisbursement()` → `Active`
- `FailDisbursement()` → `Approved` (reintentable)

#### Scenario: Préstamo entra en Disbursing al registrar instrucción

- **WHEN** se llama `POST /loans/{id}/disburse` sobre un préstamo en estado `Approved`
- **THEN** se emite `LoanDisbursed` con método, cuenta de destino y timestamp
- **THEN** el préstamo transita a estado `Disbursing`
- **THEN** el préstamo aparece en `GET /loans/pending-disbursement` con status `Disbursing`

#### Scenario: No se puede volver a instruir un desembolso ya en curso

- **WHEN** se llama `POST /loans/{id}/disburse` sobre un préstamo en estado `Disbursing`
- **THEN** el sistema retorna error indicando que el préstamo ya tiene un desembolso en curso
- **THEN** no se emite ningún evento

---

### Requirement: Confirmación de desembolso

El sistema SHALL permitir confirmar que los fondos llegaron al cliente mediante `POST /loans/{id}/confirm-disbursement`. Solo es válido para préstamos en estado `Disbursing`.

El campo `confirmedBy` identifica al oficial o sistema que confirma (string libre: nombre, email, ID de empleado, o `"achinpe-webhook"` para automatizaciones futuras).

#### Scenario: Confirmación exitosa por oficial

- **WHEN** `POST /loans/{id}/confirm-disbursement` con `{ "confirmedBy": "oficial-001" }` y el préstamo está en `Disbursing`
- **THEN** se emite `DisbursementConfirmed` con `confirmedBy` y timestamp
- **THEN** el préstamo transita a estado `Active`
- **THEN** el préstamo desaparece de `GET /loans/pending-disbursement`
- **THEN** `rm_loan_summaries` muestra status `Active`

#### Scenario: Confirmación rechazada si no está en Disbursing

- **WHEN** `POST /loans/{id}/confirm-disbursement` y el préstamo está en estado distinto a `Disbursing`
- **THEN** el sistema retorna error indicando el estado actual
- **THEN** no se emite ningún evento

#### Scenario: confirmedBy es requerido

- **WHEN** `POST /loans/{id}/confirm-disbursement` sin campo `confirmedBy` o con string vacío
- **THEN** el sistema retorna error de validación
- **THEN** no se ejecuta el handler

---

### Requirement: Registro de fallo de desembolso

El sistema SHALL permitir reportar que un desembolso falló mediante `POST /loans/{id}/fail-disbursement`. El préstamo regresa a `Approved` para permitir un nuevo intento con datos corregidos.

#### Scenario: Fallo registrado correctamente

- **WHEN** `POST /loans/{id}/fail-disbursement` con `{ "reason": "cuenta de destino inválida" }` y el préstamo está en `Disbursing`
- **THEN** se emite `DisbursementFailed` con `reason` y timestamp
- **THEN** el préstamo regresa a estado `Approved`
- **THEN** el préstamo aparece en `GET /loans/pending-disbursement` con status `Approved`
- **THEN** se puede reintentar con `POST /loans/{id}/disburse`

#### Scenario: Fallo rechazado si no está en Disbursing

- **WHEN** `POST /loans/{id}/fail-disbursement` y el préstamo está en estado distinto a `Disbursing`
- **THEN** el sistema retorna error indicando el estado actual
- **THEN** no se emite ningún evento

#### Scenario: reason es requerido

- **WHEN** `POST /loans/{id}/fail-disbursement` sin campo `reason` o con string vacío
- **THEN** el sistema retorna error de validación
- **THEN** no se ejecuta el handler

---

### Requirement: Read model de desembolsos pendientes

El sistema SHALL mantener el read model `rm_pending_disbursements` con todos los préstamos que requieren acción de desembolso: los que están en `Approved` (instrucción no enviada) o `Disbursing` (instrucción enviada, esperando confirmación).

Columnas: `loan_id`, `customer_id`, `customer_name`, `principal`, `currency`, `disbursement_method`, `destination_account`, `approved_at`, `disbursement_instructed_at`, `status`, `updated_at`.

Un préstamo abandona el read model cuando:
- Se confirma el desembolso (`DisbursementConfirmed`)
- El préstamo entra en default (`ContractDefaulted`)
- El préstamo se cancela anticipadamente (`ContractPaidOff`)

#### Scenario: Préstamo aparece al ser aprobado

- **WHEN** se emite `ContractCreated` (que incluye aprobación)
- **THEN** el préstamo aparece en `rm_pending_disbursements` con `status = Approved` y `disbursement_instructed_at = NULL`

#### Scenario: Préstamo actualiza estado al instruir desembolso

- **WHEN** se emite `LoanDisbursed`
- **THEN** `rm_pending_disbursements` actualiza el registro con `status = Disbursing`, `disbursement_method`, `destination_account`, `disbursement_instructed_at`

#### Scenario: Préstamo eliminado al confirmar desembolso

- **WHEN** se emite `DisbursementConfirmed`
- **THEN** el registro es eliminado de `rm_pending_disbursements`

#### Scenario: Préstamo regresa a Approved tras fallo

- **WHEN** se emite `DisbursementFailed`
- **THEN** `rm_pending_disbursements` actualiza el registro con `status = Approved`, borra `disbursement_method`, `destination_account`, `disbursement_instructed_at`

---

### Requirement: Endpoint de listado de desembolsos pendientes

El sistema SHALL exponer `GET /loans/pending-disbursement` que retorna todos los registros de `rm_pending_disbursements` ordenados por `disbursement_instructed_at ASC NULLS LAST` (los más urgentes primero, los que aún no se instruyeron al final).

#### Scenario: Lista con mezcla de estados

- **WHEN** `GET /loans/pending-disbursement`
- **THEN** retorna `200` con array de préstamos en `Disbursing` primero, luego en `Approved`
- **THEN** cada elemento incluye `loanId`, `customerId`, `customerName`, `principal`, `currency`, `disbursementMethod`, `destinationAccount`, `approvedAt`, `disbursementInstructedAt`, `status`

#### Scenario: Lista vacía

- **WHEN** no hay préstamos en `Approved` ni `Disbursing`
- **THEN** retorna `200` con array vacío
