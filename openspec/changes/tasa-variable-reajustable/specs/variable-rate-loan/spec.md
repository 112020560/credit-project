## ADDED Requirements

### Requirement: Creación de préstamo con tasa variable
El sistema SHALL permitir crear un contrato de préstamo con `RateType = Variable`, especificando un `Spread` (margen fijo en puntos porcentuales) y un `ReferenceRateId` que identifique la tasa de referencia externa (ej: `TBP_CRC`). La tasa efectiva inicial del préstamo SHALL calcularse como `ReferenceRate.CurrentValue + Spread`.

#### Scenario: Préstamo variable creado exitosamente
- **WHEN** se envía `RateType = "Variable"`, `Spread = 8.0`, `ReferenceRateId = "TBP_CRC"` y la tasa de referencia existe con valor 4.25
- **THEN** el préstamo se crea con tasa efectiva inicial de 12.25%
- **THEN** el schedule de amortización se calcula con esa tasa efectiva

#### Scenario: ReferenceRateId inválido rechazado
- **WHEN** se envía `RateType = "Variable"` con un `ReferenceRateId` que no existe en el sistema
- **THEN** el sistema retorna error de validación indicando que la tasa de referencia no existe

#### Scenario: Spread negativo rechazado
- **WHEN** se envía `Spread < 0`
- **THEN** el sistema retorna error de validación

#### Scenario: Préstamo fijo sin cambios
- **WHEN** se crea un préstamo con `RateType = "Fixed"` (o sin RateType)
- **THEN** el comportamiento es idéntico al actual — `AnnualRate` se usa directamente

---

### Requirement: Reajuste automático mensual de tasa
El sistema SHALL ejecutar un job mensual (`RateAdjustmentJob`) el primer día de cada mes para todos los préstamos activos con `RateType = Variable`. El job SHALL calcular la nueva tasa efectiva (`ReferenceRate.CurrentValue + Spread`) y, si difiere de la tasa vigente del préstamo, emitir el evento `RateAdjusted` con el schedule residual recalculado. Si la tasa no cambió, no SHALL emitirse ningún evento.

#### Scenario: TBP sube — tasa del préstamo se ajusta
- **WHEN** la TBP cambia de 4.25% a 5.00% y el spread es 8.00%
- **THEN** la nueva tasa efectiva es 13.00%
- **THEN** el aggregate emite `RateAdjusted` con `OldRate = 12.25`, `NewRate = 13.00`
- **THEN** el schedule residual (saldo actual + meses restantes) se recalcula con la nueva tasa
- **THEN** el nuevo schedule reemplaza al anterior en el State

#### Scenario: TBP no cambia — no se emite evento
- **WHEN** el job corre y la tasa de referencia tiene el mismo valor que el mes anterior
- **THEN** no se emite `RateAdjusted` para ningún préstamo

#### Scenario: Reajuste no afecta préstamos fijos
- **WHEN** el job corre en el primer día del mes
- **THEN** los préstamos con `RateType = Fixed` no son procesados ni modificados

#### Scenario: Distributed lock garantiza ejecución única
- **WHEN** múltiples instancias del servicio están corriendo
- **THEN** solo una instancia ejecuta el job por ciclo mensual gracias al advisory lock `1007`

---

### Requirement: Evento RateAdjusted con schedule embebido
El sistema SHALL emitir el evento `RateAdjusted` conteniendo el schedule de amortización residual completo. El event store SHALL preservar cada schedule histórico, permitiendo auditar qué schedule estuvo vigente en cualquier período.

#### Scenario: Schedule residual calculado desde saldo actual
- **WHEN** se emite `RateAdjusted` en el mes 13 de un préstamo de 60 meses
- **THEN** el nuevo schedule contiene 47 entradas (meses restantes)
- **THEN** cada entrada refleja la nueva tasa efectiva

#### Scenario: AccrueInterest usa la tasa vigente post-ajuste
- **WHEN** se acumula interés después de un `RateAdjusted`
- **THEN** `State.InterestRate` refleja la nueva tasa y el cálculo diario es correcto

---

### Requirement: AdjustRate rechazado en préstamos fijos
El sistema SHALL lanzar una excepción de dominio si se intenta llamar `AdjustRate()` sobre un préstamo con `RateType = Fixed`.

#### Scenario: AdjustRate en préstamo fijo lanza excepción
- **WHEN** se llama `aggregate.AdjustRate()` sobre un préstamo con `RateType = Fixed`
- **THEN** el sistema lanza `DomainException` con mensaje descriptivo
- **THEN** no se emite ningún evento
