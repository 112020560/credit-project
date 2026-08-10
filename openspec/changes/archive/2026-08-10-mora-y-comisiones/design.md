## Context

El sistema tiene tres problemas regulatorios independientes pero relacionados con el costo del crédito:

1. `GracePeriodDays = 5` existe en `LateFeeConfiguration` (appsettings) pero `PaymentMissedJob` lo ignora — cualquier préstamo devuelto por `GetLoansWithOverduePaymentsAsync` se procesa sin importar si está dentro del período de gracia.

2. `RecordMissedPayment` agrega un `LateFee` (cargo fijo calculado en Application) a `TotalFees`. No existe interés moratorio separado — una tasa que corra sobre el saldo vencido mientras dura la mora, distinto del cargo administrativo.

3. `LoanContractAggregate.Create()` no recibe ni almacena comisión de originación. `TotalFees` arranca en cero. La cooperativa pierde el ingreso por formalización y no puede reportarlo al regulador.

## Goals / Non-Goals

**Goals:**
- Activar el período de gracia moviendo la configuración a `UnderwritingPolicy` y añadiendo el chequeo en el job
- Agregar `AccruedPenaltyInterest` al estado del agregado y acumular interés moratorio en `PaymentMissedJob`
- Calcular y registrar comisión de originación al crear el contrato
- Actualizar el orden de aplicación de pagos
- Configurabilidad por producto con fallback a política global

**Non-Goals:**
- Cambiar el mecanismo de cargo fijo por mora (`LateFeeConfiguration`) — sigue igual
- Capitalización de intereses moratorios (anatocismo) — fuera de alcance
- Condonación específica de interés moratorio (se usa el mecanismo general de reestructuración)

## Decisions

### 1. GracePeriodDays se mueve a UnderwritingPolicy, no queda en LateFeeConfiguration

`LateFeeConfiguration` es una config de aplicación (appsettings). `UnderwritingPolicy` es una entidad de dominio almacenada en DB que ya agrupa parámetros como `AutoDefaultThresholdDays`. El período de gracia es una política de negocio, no una config técnica. Se elimina de `LateFeeConfiguration` para evitar duplicación.

**El job filtra por gracia antes de procesar**: `if (loan.DaysOverdue <= _policy.GracePeriodDays) continue;`

### 2. AccruedPenaltyInterest se acumula en PaymentMissedJob, no en un worker separado

El interés moratorio se calcula al registrar el pago perdido: `penaltyInterest = overduePrincipal × (PenaltyRate/100/365) × daysOverdue`. Se pasa a `RecordMissedPayment` como parámetro adicional. El agregado lo acumula en `AccruedPenaltyInterest` (nuevo campo en `LoanContractState`).

Alternativa descartada: worker de acumulación diaria de penalidad separado. Descartado porque el interés moratorio no crece diariamente de forma continua — nace cuando se registra el pago perdido y se establece en ese momento. En reestructuraciones se resetea.

### 3. PenaltyRate y OriginationFeeRate en UnderwritingPolicy + CreditProduct (fallback)

El producto define la tasa específica para ese tipo de préstamo. Si es null, se usa la tasa global de la política. Mismo patrón que `BaseInterestRate` ya implementado.

```
effectivePenaltyRate = product.PenaltyRate ?? policy.PenaltyRate
effectiveOriginationFeeRate = product.OriginationFeeRate ?? policy.OriginationFeeRate
```

### 4. OriginationFee pasa como parámetro a LoanContractAggregate.Create()

El handler calcula `OriginationFee = Amount × effectiveOriginationFeeRate` y lo pasa al factory method. El agregado lo almacena como `TotalFees` inicial y lo incluye en `ContractCreated`. Así el event log tiene la comisión registrada permanentemente.

### 5. Nuevo orden de aplicación de pagos

Orden actual: `fees → interés corriente → capital`
Nuevo orden: `fees (lateFee) → interés moratorio (AccruedPenaltyInterest) → interés corriente (AccruedInterest) → capital`

El interés moratorio tiene prioridad sobre el corriente porque representa una deuda más "cara" y su cobro preferente es una práctica estándar en banca.

## Risks / Trade-offs

- **Breaking change en ApplyPayment**: contratos existentes con `AccruedPenaltyInterest = 0` no se ven afectados — la lógica nueva simplemente salta ese paso. Sin migración de datos.
- **GracePeriodDays en UnderwritingPolicy requiere migración SQL**: ALTER TABLE en `underwriting_policies`. Valor default = 5 días (mismo que tenía `LateFeeConfiguration`).
- **OriginationFee en contratos históricos = 0**: contratos creados antes de esta migración tendrán `OriginationFee = 0` en el read model. Aceptable — el dato histórico correcto estaría en el contrato físico, no en el sistema.
- **PenaltyRate = 0 por defecto**: si no se configura, el interés moratorio será cero — el comportamiento es idéntico al actual. Sin riesgo de regresión.

## Migration Plan

1. Ejecutar `20260809_AddMoraYComisionesColumns.sql` (ALTER TABLE en `underwriting_policies` y `credit_products`)
2. Desplegar el nuevo servicio
3. Configurar `PenaltyRate` y `OriginationFeeRate` en la tabla `underwriting_policies` según política de la cooperativa
