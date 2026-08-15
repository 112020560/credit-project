## Context

Estado actual: `UnderwritingPolicy` se registra como `AddSingleton` en `Infrastructure/DependencyInjection.cs`, ejecutando `GetActiveAsync().GetAwaiter().GetResult()` en startup — un bloqueo síncrono que lee siempre `WHERE id = 'default'`. Esta instancia única es inyectada directamente en `CreateContractCommandHandler`, `LoanQueryService` y todas las reglas del motor.

El problema: un banco o cooperativa no puede tener políticas diferenciadas por línea de crédito. Un préstamo hipotecario (DTI máx 40%, multiplier 15x) y un préstamo de consumo (DTI máx 50%, multiplier 5x) deben poder coexistir con políticas distintas.

Componentes afectados: `CreditProduct`, `CreditProductRepository`, `CreateContractCommandHandler`, `IUnderwritingPolicyRepository`, `UnderwritingPolicyRepository`, `DependencyInjection`, `ProductEndpoints`, nuevo `UnderwritingPolicyEndpoints`.

## Goals / Non-Goals

**Goals:**
- `CreditProduct` porta su `UnderwritingPolicyId` — FK a la policy que aplica
- El handler carga la policy del producto en tiempo de ejecución, no desde DI
- Los productos existentes sin policy explícita apuntan a `'default'` automáticamente
- Endpoint para listar policies disponibles — el operador puede ver qué IDs existen
- Backward compatible con datos existentes (DEFAULT 'default' en la migración)

**Non-Goals:**
- CRUD para crear/editar policies desde API — las policies se gestionan directamente en BD por el administrador técnico (es configuración de negocio sensible)
- Múltiples policies por producto (una regla diferente en cada evaluación)
- Cache de policies — se carga en cada evaluación (scoped)
- Migrar `LoanQueryService` — sigue usando la policy de su contexto (late fees no dependen del producto del contrato original en la lectura actual)

## Decisions

### Decisión 1: Eliminar el singleton de UnderwritingPolicy del DI

El singleton se elimina completamente. `CreateContractCommandHandler` pasa a recibir `IUnderwritingPolicyRepository` y carga la policy del producto en su método `Handle`. Las reglas del motor (`DebtToIncomeRule`, `PaymentCapacityRule`, etc.) siguen recibiendo `UnderwritingPolicy` en el constructor — se la pasa el handler al construir el contexto, o el `ContractEngine` la recibe como parámetro de `EvaluateAsync`.

**Alternativa descartada A**: mantener el singleton + agregar una sobrecarga que acepte policy explícita. Doble código, confuso.

**Alternativa descartada B**: registrar `UnderwritingPolicy` como `Scoped` y resolver desde un middleware que inyecte el productId. Demasiado acoplamiento entre la capa HTTP y el dominio.

### Decisión 2: Pasar UnderwritingPolicy como parámetro a ContractEngine.EvaluateAsync

`ContractEngine.EvaluateAsync(context, baseRate, ct)` añade `policy` como parámetro: `EvaluateAsync(context, policy, baseRate, ct)`. El engine la distribuye a las reglas que la necesitan al construirlas (o las reglas la reciben vía el contexto de evaluación).

**Alternativa descartada**: inyectar `UnderwritingPolicy` en el `ContractEngine` como scoped. El engine ya es scoped, pero las reglas son resueltas desde DI con la policy del startup — no del producto. Rompe el aislamiento.

### Decisión 3: Reglas reciben UnderwritingPolicy en constructor — se re-construyen por evaluación

Las reglas actuales reciben `UnderwritingPolicy` en el constructor via DI. Con el singleton eliminado, el DI ya no puede inyectarlas directamente. La solución más limpia: `ContractEngine` resuelve las reglas desde `IServiceProvider` pasando la policy, o las reglas que necesitan policy se construyen explícitamente en el engine con la policy correcta.

Implementación concreta: `ContractEngine` recibe `IEnumerable<IContractRule>` (las reglas sin policy ya resueltas desde DI — `CollateralRule`, `ActiveLoansRule`, `ProductEligibilityRule`) más las reglas que necesitan policy (`DebtToIncomeRule`, `PaymentCapacityRule`, `CreditScoreRule`, `MemberSharesRule`) que el engine construye internamente cuando recibe la policy. O mejor: `EvaluateAsync` recibe la policy y se la pasa al contexto de evaluación — las reglas la leen desde `ContractEvaluationContext.Policy`.

**Decisión final**: agregar `UnderwritingPolicy Policy` al `ContractEvaluationContext`. Las reglas que necesitan policy leen `context.Policy` en lugar del campo inyectado. Esto elimina la necesidad de reconstruir reglas y es el cambio mínimo.

### Decisión 4: LoanQueryService mantiene referencia a IUnderwritingPolicyRepository

`LoanQueryService` usa la policy para calcular late fees. Pasa a recibir `IUnderwritingPolicyRepository` y carga la policy `'default'` en sus métodos. El `AutoDefaultThresholdDays` no es por producto — es una política institucional global. Separación razonable.

### Decisión 5: GET /underwriting-policies es read-only, sin paginación

Solo lista — no crea ni edita. La gestión de policies es operación de DBA/admin técnico. Sin paginación porque el número de policies en un sistema real es pequeño (< 20).

## Risks / Trade-offs

- [Riesgo] Si la policy referenciada por un producto se elimina de la BD, el contrato fallará con error de "policy not found" → Mitigación: FK en BD previene el DELETE si hay productos referenciando esa policy
- [Trade-off] Cada evaluación de contrato hace una query extra a BD para cargar la policy → el impacto es mínimo (una query simple por PK)
- [Riesgo] `LoanQueryService` sigue cargando `'default'` para late fees — si la policy del producto tiene `GracePeriodDays` diferente, el worker de mora no lo considera → documentar como limitación conocida y alcance futuro
- [Trade-off] Agregar `Policy` al `ContractEvaluationContext` cambia la firma del record — requiere actualizar tests

## Migration Plan

1. Aplicar `20260812_AddUnderwritingPolicyIdToProducts.sql`
2. La columna tiene `DEFAULT 'default'` + FK — todos los productos existentes apuntan a `'default'`
3. Reiniciar la aplicación — el singleton ya no existe; el handler carga la policy por demanda
4. Rollback: `ALTER TABLE credit_products DROP COLUMN underwriting_policy_id` + revertir código
