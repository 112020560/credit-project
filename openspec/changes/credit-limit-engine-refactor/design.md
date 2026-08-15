## Context

El motor de evaluación crediticia (`ContractEngine`) ejecuta reglas ordenadas por `Priority`. Las reglas `IHardStopRule` abortan la evaluación si fallan. El estado actual tiene tres problemas graves:

1. `MaxLoanAmountRule` — límite de ₡500,000 hardcodeado, `Priority=0`, primera en ejecutar
2. `DebtToIncomeRule` — no es `IHardStopRule`; DTI > 50% solo aplica penalización de tasa pero no rechaza
3. `DebtToIncomeRule` — usa `12%` fijo para estimar cuota, ignorando la tasa real del producto
4. No existe `PaymentCapacityRule` — no hay cálculo de valor presente para determinar el techo real financiable

## Goals / Non-Goals

**Goals:**
- Eliminar `MaxLoanAmountRule` completamente — sin reemplazo de cap absoluto
- `DebtToIncomeRule` se convierte en `IHardStopRule`; usa tasa real del producto
- Nueva `PaymentCapacityRule` (`IHardStopRule`, Priority=2) calcula PV desde cuota máxima
- `MaxDtiRatio` configurable en `UnderwritingPolicy` — ninguna regla usa constantes de porcentaje internas
- Migración SQL: nueva columna `max_dti_ratio`

**Non-Goals:**
- Cambiar `CollateralRule` ni `ActiveLoansRule`
- Cambiar la API pública ni los contratos de respuesta
- Agregar UI para editar la policy
- Calcular capacidad diferenciada por moneda (se asume moneda homogénea dentro de una evaluación)

## Decisions

### Decisión 1: Eliminar MaxLoanAmountRule sin reemplazo de cap global
`ProductEligibilityRule` ya valida `amount ∈ [product.minAmount, product.maxAmount]`. Agregar un cap absoluto en código duplica esa responsabilidad y la hardcodea, lo cual es peor. El operador configura el techo en el producto.

**Alternativa descartada**: Mover el cap a `UnderwritingPolicy`. No tiene sentido financiero — el techo absoluto es por producto, no por institución.

### Decisión 2: DebtToIncomeRule como IHardStopRule
Un DTI > 50% significa que el cliente destina más de la mitad de su ingreso a deuda — ninguna institución responsable aprueba eso solo con penalización de tasa. Convertirlo en hard stop es la posición financieramente correcta.

**Alternativa descartada**: Mantener como regla normal pero con lógica de rechazo. Rompe el contrato semántico de `IHardStopRule` — si rechaza, debe ser hard stop para que el engine lo trate correctamente.

### Decisión 3: PaymentCapacityRule con fórmula PV estándar
La fórmula de valor presente `PV = PMT × [(1 − (1+r)^−n) / r]` es el estándar financiero para calcular cuánto puede prestarse dado un pago máximo. Se usa la tasa del producto (no la aprobada, que aún no se conoce en este punto) como aproximación conservadora.

Caso borde: tasa = 0 → `PV = PMT × n` (sin interés, división lineal).
Caso borde: cuota_max ≤ 0 → falla con "sin capacidad disponible para nueva deuda".

### Decisión 4: Zona de advertencia del DTI = MaxDtiRatio × 0.80
En lugar de hardcodear 40%, la zona de warning es el 80% del límite configurado. Con `MaxDtiRatio = 0.50` esto da warning a partir de 40% — compatible con el comportamiento anterior. Si la cooperativa cambia `MaxDtiRatio` a 0.45, el warning se ajusta automáticamente a 36%.

### Decisión 5: Orden de reglas resultante
```
Priority 0: MemberSharesRule      (HardStop si EnforceSharesCapacityLimit=true)
Priority 1: ProductEligibilityRule (HardStop) — techo de negocio
Priority 1: CreditScoreRule       (HardStop) — acceso sí/no
Priority 2: DebtToIncomeRule      (HardStop) — DTI como filtro de acceso
Priority 2: PaymentCapacityRule   (HardStop) — techo financiero real por PV
Priority 3: CollateralRule        (informativa) — ajuste de tasa
Priority 4: ActiveLoansRule       (informativa) — ajuste de tasa
```

## Risks / Trade-offs

- [Riesgo] Préstamos que antes pasaban por tener DTI entre 40-50% ahora son rechazados → es el comportamiento correcto; documentar como breaking change de comportamiento
- [Riesgo] `PaymentCapacityRule` usa tasa del producto antes de conocer la tasa final ajustada → el PV calculado es más conservador (tasa más baja = monto máximo más alto), lo que es una posición prudente
- [Trade-off] DTI y PaymentCapacityRule son redundantes parcialmente: ambas limitan desde el ingreso. Sin embargo, DTI es un ratio de flujo (cuota/ingreso) y PaymentCapacityRule es un techo de stock (monto total). Ambas son necesarias y complementarias.

## Migration Plan

1. Aplicar `20260812_AddMaxDtiRatio.sql` en la BD
2. Reiniciar la aplicación — `UnderwritingPolicy` cargará el nuevo campo con default `0.50`
3. No hay rollback complejo: si se hace rollback del código, la columna extra en BD es inofensiva
4. Rollback de columna: `ALTER TABLE underwriting_policies DROP COLUMN max_dti_ratio`
