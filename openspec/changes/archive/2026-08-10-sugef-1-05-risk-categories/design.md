## Context

La normativa SUGEF 1-05 exige que toda entidad supervisada clasifique su cartera de crédito en categorías de riesgo y mantenga estimaciones (provisiones) mínimas. El sistema actual no tiene ningún mecanismo de clasificación de riesgo. La clasificación es un proceso de lectura (no modifica el aggregate) que opera sobre el estado proyectado en `rm_loan_summaries`.

Arquitectura actual relevante:
- Los préstamos viven en `LoanContractAggregate` con estado proyectado en `rm_loan_summaries`
- Workers de background ejecutan tareas diarias (InterestAccrualWorker, PaymentMissedWorker)
- Eventos de dominio fluyen por `IProjectionEngine` a projectors registrados

## Goals / Non-Goals

**Goals:**
- Clasificar cada préstamo activo/moroso en A1–E según días de mora (SUGEF 1-05 tabla 1)
- Calcular estimación mínima = saldo actual × porcentaje de categoría
- Reclasificar automáticamente cada noche con un job dedicado
- Permitir que un analista deglade manualmente la categoría
- Exponer resumen de cartera por categoría vía endpoint REST
- Emitir evento `LoanRiskCategoryChanged` cuando la categoría cambia

**Non-Goals:**
- Clasificación por capacidad de pago subjetiva (requiere modelo de scoring externo)
- Provisiones dinámicas del Artículo 61 Ley SUGEF (fase posterior)
- Integración con sistemas contables para registrar las provisiones
- Clasificación de créditos revolventes (scope solo préstamos a plazo)

## Decisions

### D1 — La clasificación vive en el Read Model, no en el Aggregate

**Decisión**: `RiskCategory` y `EstimatedProvision` se almacenan en `rm_loan_summaries`, no como eventos en el aggregate.

**Rationale**: La categoría de riesgo es una **proyección derivada** del estado del préstamo (días de mora), no una transición del ciclo de vida del contrato. Meter eventos de reclasificación en el aggregate inflaría el event log con eventos puramente operativos que no cambian el estado financiero del contrato.

**Alternativa descartada**: Emitir `LoanRiskCategoryChanged` como evento del aggregate → rechazado porque la categoría no afecta el comportamiento del dominio (no cambia cómo se aplican pagos, intereses, ni defaults).

**Nota**: Se emite `LoanRiskCategoryChanged` como evento de notificación (para webhooks/outbox) pero NO se persiste en el event store del aggregate.

### D2 — RiskClassificationService es un domain service puro

**Decisión**: `RiskClassificationService` es un servicio sin dependencias de infraestructura que recibe `daysOverdue` y devuelve `LoanRiskCategory`. La tabla de categorías y porcentajes es una constante del dominio (normativa, no configurable por usuario).

**Rationale**: Las tablas de SUGEF 1-05 son parte de la regulación, no de la configuración del negocio. No deben poder ser modificadas por el operador.

### D3 — Reclasificación manual solo permite degradación

**Decisión**: El endpoint `PUT /api/loans/{loanId}/risk-category` solo acepta categorías peores o iguales a la actual. La mejora es exclusivamente automática (por reducción de días de mora).

**Rationale**: La normativa SUGEF 1-05 establece que un analista puede ser más conservador que el modelo automático, pero no puede "limpiar" una cartera de forma discrecional. Permitir mejora manual abriría la puerta a manipulación de provisiones.

### D4 — El job usa `rm_loan_summaries` como fuente, no el event store

**Decisión**: `RiskClassificationJob` consulta `rm_loan_summaries` para obtener `days_overdue` calculado, no recorre el event store ni reconstituye aggregates.

**Rationale**: Los días de mora ya están calculados en el read model (campo `DaysOverdue` derivado de `next_payment_date`). Reconstituir cada aggregate para reclasificar sería O(n×eventos) y no escalable.

### D5 — Días de mora calculados en SQL, no en código C#

**Decisión**: `EXTRACT(DAY FROM NOW() - next_payment_date)::INT` se calcula en la query, no en la aplicación.

**Rationale**: Consistencia con el patrón ya establecido en `GetLoansWithOverduePaymentsAsync`. Evita drift de zona horaria entre servidor y DB.

## Risks / Trade-offs

- **Risk**: `rm_loan_summaries.next_payment_date` puede ser NULL (contrato Approved sin desembolsar) → Mitigation: el job filtra `status IN ('Active', 'Delinquent', 'Default')` y `next_payment_date IS NOT NULL`
- **Risk**: Sin distributed locking, 2 instancias reclasifican simultáneamente → Mitigation: la reclasificación es idempotente (misma entrada → mismo resultado); duplicar no causa corrupción. El locking se aborda en Fase 3.
- **Risk**: El campo `days_overdue` del read model puede estar desactualizado si `PaymentMissedWorker` no corrió → Mitigation: el job recalcula `days_overdue` directamente en la query con `NOW() - next_payment_date`, no confía en el campo almacenado.
- **Trade-off**: Emitir `LoanRiskCategoryChanged` como evento de notificación sin persistirlo en el event store significa que el historial de cambios de categoría no es reconstruible desde eventos → Aceptado para esta fase; si se necesita auditoría de categorías, se agrega tabla dedicada en fase posterior.

## Migration Plan

1. Ejecutar migración SQL (`ALTER TABLE rm_loan_summaries ADD COLUMN risk_category`, `ADD COLUMN estimated_provision`)
2. El job de reclasificación en su primera ejecución populará los campos para toda la cartera existente (valores por defecto NULL → categorizados)
3. Sin rollback destructivo — las columnas pueden quedar con NULL si se revierte el código
