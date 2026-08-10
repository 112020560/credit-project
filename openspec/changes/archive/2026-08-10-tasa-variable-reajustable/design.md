## Context

El sistema actual trata `InterestRate` como un value object inmutable con un único campo `AnnualRate`. No existe concepto de tipo de tasa, tasa de referencia ni spread. El aggregate `LoanContractAggregate` ya tiene el método `Restructure()` que recalcula el schedule y lo emite en un evento — ese patrón es la base de esta implementación.

La arquitectura elegida es **Opción A: recalcular y reemplazar el schedule en el evento** `RateAdjusted`. El event store preserva el historial completo de schedules, y `State.PaymentSchedule` siempre refleja el schedule vigente.

## Goals / Non-Goals

**Goals:**
- Soportar `RateType = Variable` en la creación de préstamos con `Spread` y `ReferenceRateId`
- Agregar `AdjustRate()` al aggregate con emisión de `RateAdjusted` + schedule residual
- Worker mensual que detecta préstamos variables y aplica el reajuste si la tasa cambió
- Endpoints para consultar y actualizar tasas de referencia externas (TBP, PRIME)
- Mantener compatibilidad total con préstamos `Fixed` existentes

**Non-Goals:**
- Integración directa con la API del BCCR (la TBP se ingresa manualmente vía endpoint PUT)
- Notificación al socio del cambio de tasa (queda para la fase de documentos/notificaciones)
- Tasa variable en crédito revolvente (solo préstamos a plazo en esta iteración)

## Decisions

### D1: Extender InterestRate vs. crear nuevo value object

**Decisión**: Extender `InterestRate` con `RateType`, `Spread` y `ReferenceRateId` como propiedades opcionales.

**Alternativa descartada**: Crear `VariableInterestRate` como tipo separado. Requeriría cambios en todas las firmas que reciben `InterestRate` y duplicaría lógica de cálculo de tasa diaria/mensual.

**Rationale**: Con valores por defecto (`RateType = Fixed`, `Spread = 0`, `ReferenceRateId = null`), todos los préstamos existentes son backward-compatible sin cambios en sus eventos almacenados.

### D2: Schedule embebido en RateAdjusted vs. recalcular on-demand

**Decisión**: El schedule residual completo se incluye en el evento `RateAdjusted`.

**Alternativa descartada**: No guardar el schedule en State y calcularlo siempre desde el query handler. Requeriría eliminar `PaymentSchedule` del State, rompiendo código existente y los projectors.

**Rationale**: Mismo patrón que `Restructure()`. El event store actúa como archivo histórico de schedules. Los préstamos de cooperativa tienen plazos de 1-10 años (12-120 entradas), tamaño manejable en JSON.

### D3: RateAdjustmentWorker — detección de cambio

**Decisión**: El job compara `newEffectiveRate != State.InterestRate.AnnualRate` con tolerancia de 0.0001 para evitar ruido por floating point. Si no hay diferencia, no emite evento.

**Rationale**: Idempotencia — si el admin actualizó la TBP al mismo valor del mes anterior (o si el worker corre dos veces), no se generan eventos fantasma.

### D4: RemainingMonths calculado desde PaymentSchedule

**Decisión**: `RemainingMonths` = cantidad de entradas en `State.PaymentSchedule` con `DueDate >= today` que aún no están pagadas.

**Alternativa**: Calcular como `TermMonths - monthsElapsed`. Menos preciso si hubo pagos adelantados o reestructuraciones.

**Rationale**: El schedule actual ya refleja el estado real del préstamo, incluyendo reestructuraciones previas.

### D5: WorkerLockId.RateAdjustment = 1007

**Decisión**: Agregar la constante `1007` al enum `WorkerLockId` existente.

**Rationale**: Consistencia con el patrón de distributed locking implementado en Fase 3. El worker corre una vez al mes — el lock es especialmente importante para evitar doble ajuste.

## Risks / Trade-offs

- **Tamaño de eventos**: un préstamo de 10 años genera 120 entradas en el schedule. Serializado en JSON ~18KB por evento `RateAdjusted`. Aceptable a escala de cooperativa. → Si en el futuro el event store muestra presión, considerar comprimir el campo `schedule` en JSONB.

- **Fecha de ejecución del worker**: "primer día del mes" puede caer en fin de semana o feriado. Por ahora el worker corre cuando el primer día del mes llega según UTC. → Fase futura: calendario de días hábiles BCCR.

- **Consistencia del ReferenceRate**: si el admin no actualiza la TBP antes del primer día del mes, el worker usará el valor del mes anterior y no ajustará la tasa. → Documentar proceso operativo: actualizar TBP antes del día 1.

- **Meses restantes en cero**: si el préstamo está en su último mes y se emite `RateAdjusted`, el schedule residual tiene 1 entrada. El aggregate no debe llamar `AdjustRate` si `RemainingMonths <= 1`. → El job filtra préstamos con menos de 2 meses restantes.

## Migration Plan

1. Aplicar migración SQL `20260810_AddReferenceRatesTable.sql` (nueva tabla)
2. Aplicar migración SQL `20260810_AddRatTypeColumnsToLoanSummary.sql` (columnas read model)
3. Insertar registro inicial: `INSERT INTO reference_rates VALUES ('TBP_CRC', 'Tasa Básica Pasiva', ...)` con el valor vigente
4. Deploy de la aplicación — los préstamos existentes mantienen `RateType = Fixed` implícito (valor por defecto en deserialization)
5. Rollback: las columnas nuevas son nullable — la aplicación anterior ignora las columnas adicionales

## Open Questions

- ¿Se requiere un `PUT /api/reference-rates` para crear nuevas tasas de referencia (USD/PRIME) o solo actualizar las existentes? → Por ahora el PUT hace upsert.
- ¿El primer reajuste de un préstamo variable debe enviarse como notificación al socio? → Pendiente para fase de documentos/notificaciones.
