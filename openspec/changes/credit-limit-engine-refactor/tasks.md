## 1. Base de datos

- [x] 1.1 Crear migración `20260812_AddMaxDtiRatio.sql`: `ALTER TABLE underwriting_policies ADD COLUMN IF NOT EXISTS max_dti_ratio NUMERIC(4,2) NOT NULL DEFAULT 0.50`

## 2. Dominio — UnderwritingPolicy

- [x] 2.1 Agregar `decimal MaxDtiRatio = 0.50m` como parámetro con default al record `UnderwritingPolicy`

## 3. Infraestructura — UnderwritingPolicyRepository

- [x] 3.1 Agregar `max_dti_ratio` al SELECT y al mapeo del record en `UnderwritingPolicyRepository.GetActiveAsync()`

## 4. Dominio — Eliminar MaxLoanAmountRule

- [x] 4.1 Eliminar el archivo `MaxLoanAmountRule.cs`
- [x] 4.2 Desregistrar `MaxLoanAmountRule` del contenedor DI en `CreditSystem.Domain/DependencyInjection.cs`

## 5. Dominio — Refactorizar DebtToIncomeRule

- [x] 5.1 Implementar `IHardStopRule` en `DebtToIncomeRule` (agregar la interfaz a la declaración de clase)
- [x] 5.2 Reemplazar la tasa fija del 12% por la tasa real del producto: `context.Product?.Rates.BaseInterestRate ?? _policy.BaseInterestRate`
- [x] 5.3 Convertir el bloque `if (dtiRatio > MaxDtiRatio)` en hard stop que retorna `Fail` (ya lo hace, confirmar que con `IHardStopRule` el engine lo trate como tal)
- [x] 5.4 Cambiar la zona de advertencia de DTI fija (40%) a `_policy.MaxDtiRatio * 0.80m`
- [x] 5.5 Reemplazar la constante `MaxDtiRatio = 0.50m` interna por `_policy.MaxDtiRatio` — inyectar `UnderwritingPolicy` en el constructor

## 6. Dominio — Crear PaymentCapacityRule

- [x] 6.1 Crear `PaymentCapacityRule.cs` en `CreditSystem.Domain/Rules/Implementations/`:
  - Implementa `IContractRule` e `IHardStopRule`
  - `Priority = 2`, `RuleName = "PaymentCapacityEvaluation"`
  - Inyecta `UnderwritingPolicy` en constructor
  - Si `MonthlyIncome` es null o 0 → retorna `Pass` con `Skipped = true`
  - `cuota_max = ingreso_mensual × _policy.MaxDtiRatio − deuda_mensual_existente`
  - Si `cuota_max <= 0` → retorna `Fail` con "no hay capacidad disponible para nueva deuda"
  - `tasa_mensual = (product.BaseInterestRate ?? policy.BaseInterestRate) / 12m / 100m`
  - Si `tasa_mensual == 0` → `monto_max = cuota_max × n`
  - Si `tasa_mensual > 0` → `monto_max = cuota_max × [(1 − (1+r)^−n) / r]` usando `Math.Pow`
  - Si `monto_solicitado > monto_max` → retorna `Fail` con mensaje descriptivo
  - Si pasa → retorna `Pass` con metadata (`MaxFinanciableAmount`, `MaxMonthlyPayment`)
- [x] 6.2 Registrar `PaymentCapacityRule` en el contenedor DI en `CreditSystem.Domain/DependencyInjection.cs`

## 7. Documentación

- [x] 7.1 Actualizar `loan-lifecycle.md`: eliminar la mención al cap de ₡500,000 en la tabla de reglas, agregar `PaymentCapacityRule` y `MaxDtiRatio` en la sección de configuración de `UnderwritingPolicy`
