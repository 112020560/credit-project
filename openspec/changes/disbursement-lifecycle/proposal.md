## Why

El desembolso actual es un paso único: `POST /loans/{id}/disburse` registra la instrucción y activa el préstamo en el mismo acto, asumiendo que el dinero ya llegó al cliente. Esto no captura el estado intermedio entre "instrucción emitida" y "fondos confirmados en cuenta del cliente", impidiendo manejar fallos de transferencia y bloqueando la futura integración con ACH/SINPE para desembolsos automáticos.

## What Changes

- **BREAKING** `LoanDisbursed` ya no activa el préstamo directamente: el estado pasa a `Disbursing` (nuevo) en lugar de `Active`
- Nuevo estado `Disbursing` en `LoanContractAggregate` entre `Approved` y `Active`
- Nuevo evento `DisbursementConfirmed`: el préstamo pasa de `Disbursing` a `Active`
- Nuevo evento `DisbursementFailed`: el préstamo regresa de `Disbursing` a `Approved` (reintentable)
- Nuevos métodos en el aggregate: `ConfirmDisbursement(confirmedBy)` y `FailDisbursement(reason)`
- Nueva tabla `rm_pending_disbursements` con todos los préstamos en estado `Approved` o `Disbursing`
- Nuevo proyector `PendingDisbursementsProjector`
- Nuevos comandos y handlers: `ConfirmDisbursementCommand`, `FailDisbursementCommand`
- Nuevos endpoints: `GET /loans/pending-disbursement`, `POST /loans/{id}/confirm-disbursement`, `POST /loans/{id}/fail-disbursement`
- `LoanSummaryProjector` actualizado para manejar `DisbursementConfirmed` y `DisbursementFailed`

## Capabilities

### New Capabilities

- `disbursement-lifecycle`: Ciclo de vida completo del desembolso con estado intermedio `Disbursing`, eventos de confirmación y fallo, y read model de desembolsos pendientes para gestión manual y futura automatización ACH/SINPE.

### Modified Capabilities

- `loan-contract`: El estado `Disbursing` se agrega al ciclo de vida; `LoanDisbursed` ya no implica activación inmediata del préstamo.

## Impact

- **Dominio**: `LoanContractAggregate`, `ContractStatus` enum, nuevos eventos de dominio
- **Infraestructura**: nuevo proyector, nueva tabla en BD, migración SQL
- **Application**: dos nuevos comandos + handlers + validators
- **API**: tres nuevos endpoints en `LoanContractEndpoints`
- **Tests existentes**: cualquier test que llame `Disburse()` y espere estado `Active` debe actualizarse para llamar también `ConfirmDisbursement()`
