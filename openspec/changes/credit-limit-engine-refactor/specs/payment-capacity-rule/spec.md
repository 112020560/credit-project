# Spec: Payment Capacity Rule

Regla del motor de evaluación crediticia que calcula el monto máximo financiable a partir de la capacidad de pago real del solicitante usando valor presente (PV). Actúa como hard stop: si el monto solicitado supera el techo calculado desde el ingreso, el préstamo es rechazado.

---

## ADDED Requirements

### Requirement: Cálculo de monto máximo desde capacidad de pago
El sistema SHALL calcular el monto máximo financiable como el valor presente de la cuota máxima posible del solicitante, usando la tasa real del producto y el plazo solicitado. Esta regla es un **hard stop** con `Priority = 2`.

Fórmulas:
```
cuota_max = ingreso_mensual × MaxDtiRatio − deuda_mensual_existente
tasa_mensual = tasa_anual_efectiva / 12 / 100
monto_máx = cuota_max × [(1 − (1 + r)^−n) / r]
```

donde `tasa_anual_efectiva = product.BaseInterestRate ?? policy.BaseInterestRate` y `n = termMonths`.

Si no hay datos de ingreso disponibles, la regla SHALL omitirse sin bloquear (`Skipped = true`).

#### Scenario: Monto solicitado dentro de la capacidad de pago
- **WHEN** el monto solicitado es menor o igual al monto máximo calculado desde el ingreso
- **THEN** la regla pasa con mensaje indicando el techo calculado y el monto solicitado

#### Scenario: Monto solicitado supera la capacidad de pago — hard stop
- **WHEN** el monto solicitado supera el monto máximo calculado desde la cuota máxima del solicitante
- **THEN** la regla falla como hard stop con mensaje que indica el monto solicitado, el monto máximo financiable y la cuota máxima disponible

#### Scenario: Sin datos de ingreso — regla omitida
- **WHEN** `MonthlyIncome` es null o cero
- **THEN** la regla retorna `Pass` con `Skipped = true` y no bloquea el préstamo

#### Scenario: Cuota máxima calculada es negativa o cero
- **WHEN** la deuda mensual existente ya supera o iguala `ingreso × MaxDtiRatio`
- **THEN** la regla falla con mensaje indicando que el cliente no tiene capacidad de pago disponible para nueva deuda

#### Scenario: Tasa cero — cuota dividida linealmente
- **WHEN** la tasa efectiva del producto es cero
- **THEN** el monto máximo se calcula como `cuota_max × n` (división lineal sin interés)
