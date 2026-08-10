## Context

El sistema tiene `UnderwritingPolicy` como única fuente de parámetros del motor de evaluación — tasa base, umbral de default, comportamiento sin score. Eso funciona para políticas globales, pero una cooperativa tiene múltiples productos con parámetros diferentes: un préstamo hipotecario tiene un plazo máximo de 360 meses y requiere colateral con LTV ≤ 80%, mientras que un microcrédito tiene monto máximo de 500,000 CRC, plazo de hasta 24 meses y no requiere colateral.

`CreditProduct` no es un aggregate (no tiene ciclo de vida complejo con eventos de dominio relevantes para el event store del crédito) — es una **entidad de configuración** del dominio, similar a `UnderwritingPolicy`. Su estado cambia raramente (activar/desactivar) y no necesita event sourcing.

El `ContractEngine` actualmente lee `_policy.BaseInterestRate` internamente. Con productos, la tasa base viene del producto, no de la política global. Hay que parametrizar esta dependencia sin romper el engine.

## Goals / Non-Goals

**Goals:**
- Modelar `CreditProduct` como entidad de dominio configurable en BD
- Que el contrato quede ligado a un producto específico desde su creación
- Validar elegibilidad del contrato contra el producto antes de evaluar otras reglas
- Que la tasa base del producto sobreescriba la global para ese contrato
- Administración del catálogo vía API (sin flujo de aprobación por ahora)

**Non-Goals:**
- Event sourcing de cambios al catálogo de productos (fuera de scope)
- Versionado de productos (un cambio de tasa no afecta contratos existentes)
- Tarifas y comisiones por producto (eso es Fase 2)
- Segmentación de cartera y reportes SUGEF por tipo de producto

## Decisions

### D1: CreditProduct como entidad de dominio, no aggregate

`CreditProduct` no acumula eventos de dominio relevantes para el event store de crédito. No tiene operaciones transaccionales complejas — se lee para validar y crear contratos, y se escribe raramente (alta del producto, cambio de estado). Modelarla como entidad simple con repositorio CRUD es suficiente y evita complejidad innecesaria.

**Alternativa descartada**: Aggregate con event sourcing. Descartada porque los cambios al catálogo no necesitan audit trail en el event store del crédito — son configuración administrativa.

### D2: ProductLimits y ProductRates como value objects inmutables

Agrupar los límites (`MinAmount`, `MaxAmount`, `MinTermMonths`, `MaxTermMonths`) y las tasas (`BaseInterestRate`, `MaxLtv`) en value objects hace la entidad más legible y permite validación en el punto de construcción. Si en el futuro se agregan más parámetros, se extienden los value objects sin cambiar la firma del constructor de `CreditProduct`.

### D3: ProductEligibilityRule con Priority = 1 (después de MemberSharesRule)

La secuencia de evaluación queda:
1. `MemberSharesRule` (Priority 0) — ¿es socio? ¿tiene suficientes aportaciones?
2. `ProductEligibilityRule` (Priority 1) — ¿el contrato cumple con el producto seleccionado?
3. `MaxLoanAmountRule` (Priority 2+) — reglas de riesgo general

Si el contrato no cumple con el producto (monto fuera de rango, plazo inválido), no tiene sentido evaluar score ni DTI.

### D4: Tasa base parametrizada en ContractEngine

En lugar de que `ContractEngine.EvaluateAsync` lea `_policy.BaseInterestRate`, recibe la tasa base como parámetro opcional:

```csharp
EvaluateAsync(context, baseInterestRate: product.Rates.BaseInterestRate ?? _policy.BaseInterestRate)
```

Si el producto no define tasa base propia (null), se usa la de la política. Esto mantiene retrocompatibilidad sin romper el engine ni los tests existentes.

**Alternativa descartada**: Inyectar el producto en el engine. Descartada porque el engine no debe conocer el concepto de producto — solo procesa reglas y tasa base.

### D5: ContractEvaluationContext incluye el CreditProduct resuelto

El handler resuelve el `CreditProduct` y lo pasa en el contexto. `ProductEligibilityRule` lo lee desde ahí. Esto sigue el mismo patrón que `Customer` y `MemberShares` — el handler enriquece el contexto, las reglas lo consumen.

### D6: Seed de 5 productos típicos de cooperativa costarricense

La migración incluye los siguientes productos con valores realistas para SUGEF:

| Producto | Tasa | Plazo máx | Monto máx | Colateral |
|---|---|---|---|---|
| Préstamo Personal | 14% | 60 meses | 5,000,000 CRC | No |
| Préstamo de Consumo | 18% | 36 meses | 2,000,000 CRC | No |
| Préstamo Vehicular | 12% | 84 meses | 15,000,000 CRC | Sí (LTV 90%) |
| Préstamo Hipotecario | 9% | 300 meses | 100,000,000 CRC | Sí (LTV 80%) |
| Microcrédito | 20% | 24 meses | 500,000 CRC | No |

## Risks / Trade-offs

- **[Riesgo] Cambio de tasa en producto no afecta contratos activos** — Intencional por diseño. El contrato persiste su tasa aprobada en el evento `ContractCreated`. Si la cooperativa cambia la tasa del producto, los contratos existentes no se ven afectados. Mitigation: documentado como comportamiento esperado.

- **[Riesgo] ProductId inválido en CreateContractCommand** — El handler valida existencia y estado `Active` del producto antes de proceder. Si el producto no existe o está `Inactive`, retorna error inmediato sin ejecutar el engine.

- **[Trade-off] CRUD sin aprobación** — Los endpoints de administración del catálogo no tienen flujo de aprobación. Un oficial podría activar un producto con parámetros incorrectos. Mitigation: validación fuerte en el dominio (LTV entre 0-100%, tasa > 0, plazos coherentes). Flujo de aprobación queda para Fase 2.

## Migration Plan

1. Aplicar `20260808_AddCreditProductsTable.sql` (crea tabla + seed de 5 productos)
2. Deployar nueva versión — `ProductId` es requerido en `CreateContractCommand`
3. Todos los callers del API deben incluir `ProductId` en sus requests (breaking change coordinado)

## Open Questions

- ¿Las tasas de los productos se expresan en CRC o aplican también a USD? (por ahora: la tasa es independiente de moneda, aplica sobre el monto en la moneda del contrato)
- ¿Debe `ProductEligibilityRule` verificar también que la moneda del contrato sea compatible con el producto? (sugerencia: agregar campo `AllowedCurrencies` al producto en una iteración futura)
