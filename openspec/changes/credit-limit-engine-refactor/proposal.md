## Why

El motor de reglas de crédito calcula límites de crédito de forma incorrecta: tiene un monto absoluto de ₡500,000 hardcodeado en código fuente, no rechaza solicitudes con DTI insostenible, y no calcula el techo financiero real desde la capacidad de pago del solicitante. Un sistema de crédito que no modela correctamente la capacidad de pago aprueba préstamos que el cliente no puede pagar o rechaza clientes solventes por razones arbitrarias.

## What Changes

- **ELIMINAR** `MaxLoanAmountRule` — contiene un límite absoluto de ₡500,000 hardcodeado sin justificación financiera. El techo de negocio ya lo maneja `ProductEligibilityRule` via `product.maxAmount`
- **MODIFICAR** `DebtToIncomeRule`: convertir en `IHardStopRule` (DTI > `max_dti_ratio` rechaza, no solo ajusta tasa). Usar la tasa real del producto para estimar la cuota en lugar del 12% fijo hardcodeado
- **CREAR** `PaymentCapacityRule` (`IHardStopRule`, Priority=2): calcula el monto máximo financiable usando valor presente (`PV`) a partir de la cuota máxima posible del solicitante. Si el monto solicitado supera ese techo, rechaza
- **MODIFICAR** `UnderwritingPolicy`: agregar `MaxDtiRatio` (default `0.50`) configurable en BD — ninguna regla debe tener porcentajes hardcodeados
- **CREAR** migración SQL: columna `max_dti_ratio NUMERIC(4,2) NOT NULL DEFAULT 0.50` en `underwriting_policies`

## Capabilities

### New Capabilities
- `payment-capacity-rule`: Regla que calcula el monto máximo financiable usando valor presente a partir de la capacidad de pago real (ingreso × DTI_max − deuda existente), actuando como hard stop si el monto solicitado lo supera

### Modified Capabilities
- `underwriting-policy`: Se agrega `MaxDtiRatio` como parámetro configurable de la política; las reglas del motor deben leerlo en lugar de usar constantes internas
- `loan-contract`: El motor de evaluación cambia su comportamiento — `DebtToIncomeRule` ahora rechaza en lugar de solo ajustar tasa cuando DTI es insostenible

## Impact

- `CreditSystem.Domain`: eliminar `MaxLoanAmountRule`, modificar `DebtToIncomeRule`, crear `PaymentCapacityRule`, modificar `UnderwritingPolicy`
- `CreditSystem.Infrastructure`: `UnderwritingPolicyRepository` (nuevo campo), nueva migración SQL
- `CreditSystem.Domain/DependencyInjection.cs`: desregistrar `MaxLoanAmountRule`, registrar `PaymentCapacityRule`
- Sin cambios en API ni contratos externos — el cambio es interno al motor de reglas
- **Efecto en comportamiento**: préstamos antes bloqueados por el cap de ₡500,000 podrán aprobarse si el cliente tiene ingreso suficiente; préstamos con DTI > 50% que antes se aprobaban con penalización ahora se rechazan
