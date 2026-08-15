## Context

El desembolso actual es atómico: `POST /loans/{id}/disburse` genera `LoanDisbursed` y el contrato pasa a `Active` en un único paso. El evento `LoanDisbursed` mezcla dos hechos de negocio distintos: "se registró la instrucción de desembolso" y "el cliente recibió el dinero". En la cooperativa hoy los desembolsos son manuales (oficial hace la transferencia y luego la registra en el sistema), pero en el futuro se integrará con ACH/SINPE donde la confirmación llega de forma asíncrona desde el proveedor.

No existe actualmente ningún read model para listar préstamos pendientes de desembolso, lo que obliga a consultar el event store directamente o filtrar `rm_loan_summaries` ad-hoc.

## Goals / Non-Goals

**Goals:**
- Separar "instrucción de desembolso" de "fondos confirmados" con un estado intermedio `Disbursing`
- Permitir que un desembolso fallido regrese al estado `Approved` sin perder el historial (evento `DisbursementFailed`)
- Proveer un read model `rm_pending_disbursements` con todos los préstamos en espera de desembolso
- Endpoints para que el oficial confirme o reporte fallo manualmente
- Diseño compatible con futura automatización ACH/SINPE sin cambios de dominio

**Non-Goals:**
- Integración ACH/SINPE (queda para un change separado)
- Notificaciones automáticas al cliente al confirmar desembolso
- Límite de reintentos o expiración de desembolsos en estado `Disbursing`

## Decisions

### 1. Estado intermedio `Disbursing` en el aggregate

`Disbursing` se agrega al enum `ContractStatus` entre `Approved` y `Active`.

Transiciones válidas:
```
Approved → Disburse()             → Disbursing
Disbursing → ConfirmDisbursement() → Active
Disbursing → FailDisbursement()    → Approved
```

**Alternativa descartada**: mantener un flag `IsDisbursementPending` en `LoanContractState` sin nuevo estado. Descartado porque rompe la invariante de que el estado captura completamente el ciclo de vida; hace los guards más complejos y el estado `Disbursing` es semánticamente significativo para el negocio.

### 2. `LoanDisbursed` pasa a `Disbursing`, no a `Active`

El evento existente cambia de semántica: ya no activa el préstamo, solo registra la instrucción. Esto es un **breaking change** que requiere actualizar todos los tests que asuman `Active` después de `Disburse()`.

**Alternativa descartada**: evento nuevo `DisbursementInstructed` y mantener `LoanDisbursed` para confirmación. Descartado porque `LoanDisbursed` ya tiene el nombre correcto para "se instruyó el desembolso" y agregar un nuevo evento solo añade complejidad sin beneficio semántico claro.

### 3. `FailDisbursement()` regresa a `Approved`

En lugar de un estado terminal `DisbursementFailed`, el préstamo regresa a `Approved` para permitir reintentos. El historial queda en el event store mediante el evento `DisbursementFailed`.

**Alternativa descartada**: estado terminal `DisbursementFailed` con un `RetryDisbursement()`. Descartado porque aumenta la complejidad del estado sin beneficio operacional claro para una cooperativa donde el oficial simplemente corrige la cuenta y reintenta.

### 4. `PendingDisbursementsProjector` como proyector independiente

El proyector escucha `ContractCreated`, `LoanDisbursed`, `DisbursementConfirmed`, `DisbursementFailed`, `ContractDefaulted`, `ContractPaidOff`. Administra su propio read model `rm_pending_disbursements` con INSERT/UPDATE/DELETE según el evento.

**Alternativa descartada**: filtrar `rm_loan_summaries` por `status IN ('Approved', 'Disbursing')`. Descartado porque `rm_pending_disbursements` puede incluir campos específicos del desembolso (`disbursement_method`, `destination_account`, `disbursement_instructed_at`) que no pertenecen en el resumen general del préstamo.

### 5. `confirmed_by` como string libre

`ConfirmDisbursement(string confirmedBy)` recibe el identificador del oficial como string (nombre, email, ID de empleado). No se valida contra ninguna tabla de usuarios porque el sistema de crédito no gestiona usuarios internamente.

## Risks / Trade-offs

- **Breaking change en tests** → Cualquier test que llame `Disburse()` y espere estado `Active` fallará. Mitigation: identificar y actualizar todos los tests afectados como parte de las tareas.
- **Préstamos atascados en `Disbursing`** → Si el oficial se olvida de confirmar o reportar fallo, el préstamo queda en `Disbursing` indefinidamente. Mitigation: el read model `rm_pending_disbursements` muestra el campo `disbursement_instructed_at`, permitiendo identificar desembolsos con mucho tiempo sin confirmar. Alertas o expiración automática quedan para un change futuro.
- **Replay de eventos históricos** → Los eventos `LoanDisbursed` ya almacenados en el event store asumían transición a `Active`. Al hacer replay, el aggregate aplicará la nueva semántica (`Disbursing`). Mitigation: los préstamos históricos que tenían `LoanDisbursed` sin `DisbursementConfirmed` quedarán en `Disbursing` al reconstruirse. Solución: en la migración, insertar un evento sintético `DisbursementConfirmed` para cada préstamo existente con `LoanDisbursed` ya persistido que no tenga un `DisbursementConfirmed` posterior.

## Migration Plan

1. Aplicar migración SQL: crear `rm_pending_disbursements`, actualizar `LoanSummaryProjector` para el nuevo estado `Disbursing`
2. Insertar eventos `DisbursementConfirmed` sintéticos para préstamos históricos activos:
   ```sql
   -- Identificar préstamos con LoanDisbursed pero sin DisbursementConfirmed
   -- Insertar DisbursementConfirmed con confirmedBy = 'migration'
   ```
3. Ejecutar `POST /api/v1/admin/projection/rebuild` para reconstruir todos los read models con la nueva lógica
4. Verificar que `rm_loan_summaries` no tenga préstamos activos en estado `Disbursing` inesperado
