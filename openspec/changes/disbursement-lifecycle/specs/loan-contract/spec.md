# Spec: Loan Contract (delta)

Delta spec — modifica únicamente el requisito de desembolso afectado por el ciclo de vida en dos fases.

---

## MODIFIED Requirements

### Requirement: Desembolso del préstamo

El sistema SHALL permitir registrar la instrucción de desembolso de un préstamo que esté en estado `Approved`, transitando al estado `Disbursing`. La activación definitiva del préstamo a estado `Active` ocurre únicamente al confirmar que los fondos llegaron al cliente.

Ciclo completo:
- `Approved` → `Disburse(method, destinationAccount)` → `Disbursing`
- `Disbursing` → `ConfirmDisbursement(confirmedBy)` → `Active`
- `Disbursing` → `FailDisbursement(reason)` → `Approved`

#### Scenario: Instrucción de desembolso registrada

- **WHEN** se desembolsa un contrato en estado `Approved` con método y cuenta de destino
- **THEN** se emite `LoanDisbursed` con monto, método, cuenta y timestamp
- **THEN** el contrato transita a estado `Disbursing` (no a `Active`)

#### Scenario: Desembolso rechazado si no está en Approved

- **WHEN** se intenta desembolsar un contrato en estado distinto a `Approved`
- **THEN** el sistema lanza `DomainException` indicando el estado actual
- **THEN** no se emite ningún evento

#### Scenario: Desembolso confirmado — préstamo activo

- **WHEN** se confirma el desembolso de un contrato en estado `Disbursing`
- **THEN** se emite `DisbursementConfirmed` con `confirmedBy` y timestamp
- **THEN** el contrato transita a estado `Active`

#### Scenario: Desembolso fallido — préstamo reintentable

- **WHEN** se reporta fallo de desembolso de un contrato en estado `Disbursing`
- **THEN** se emite `DisbursementFailed` con `reason` y timestamp
- **THEN** el contrato regresa a estado `Approved`
- **THEN** se puede reintentar con una nueva instrucción de desembolso
