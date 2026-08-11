## Context

El sistema actual tiene la prelación de pagos hardcodeada en `LoanContractAggregate.ApplyPayment()` con el orden: comisiones → interés moratorio → interés corriente → principal. No existe el concepto de capital social en ninguna capa. El evento `PaymentApplied` no tiene campo para él, `LoanContractState` no lo acumula, y `cooperative_members` no tiene saldo de capital social.

Las cooperativas costarricenses están obligadas por su normativa interna (estatutos) a mantener el capital social de sus socios. Cada pago de préstamo genera un aporte que incrementa la participación del socio como propietario. Este dato es necesario para reportes regulatorios, balances de socios y documentos legales.

## Goals / Non-Goals

**Goals:**
- Motor de prelación configurable por producto: orden de aplicación de fondos como dato del producto, no como código.
- Capital social por producto: tipo de cálculo y modo de cobro configurables.
- Acumulación de capital social en el perfil del socio (saldo total).
- Visibilidad en comprobante de pago PDF.
- Retrocompatibilidad de datos: préstamos existentes sin producto asignado usan el waterfall por defecto actual (comisiones → interés moratorio → interés corriente → principal, sin capital social).

**Non-Goals:**
- Capital social en líneas de crédito revolventes (mejora futura).
- Transferencia o retiro de capital social acumulado (operación separada).
- Contabilización de capital social en un libro mayor (fuera del scope de este sistema).
- Cambio de waterfall en préstamos ya desembolsados (el waterfall se toma del producto al momento de cada pago).

## Decisions

### Decisión 1: PaymentWaterfall como value object pasado al agregado, no cargado dentro de él

**Opción elegida**: `ApplyPayment()` recibe `PaymentWaterfall` como parámetro. El agregado no conoce repositorios ni el producto al que pertenece el préstamo.

**Alternativa descartada**: el agregado almacena el `ProductId` en su estado y el handler lo usa para hidratar el waterfall internamente.

**Razón**: mantiene el agregado puro (sin dependencias de infraestructura). El handler de Application es quien conoce el producto y construye el waterfall. Si el préstamo no tiene producto asignado, el handler usa el `PaymentWaterfall.Default` con la prelación legacy.

---

### Decisión 2: SocialCapitalConfig separado del waterfall

El waterfall define el *orden* de los componentes. `SocialCapitalConfig` define *cómo se calcula* el monto del componente SocialCapital. Son dos concerns distintos.

```csharp
// En el producto:
PaymentWaterfall Waterfall { get; }          // orden de prelación
SocialCapitalConfig? SocialCapitalConfig { get; }  // cálculo del monto
```

Si el producto no tiene `SocialCapitalConfig`, el componente `SocialCapital` del waterfall simplemente aplica ₡0 (ignorado).

---

### Decisión 3: Cálculo del capital social en Application, no en el agregado

El `ApplyPaymentCommandHandler` calcula el monto de capital social antes de llamar al agregado. El agregado solo recibe el `PaymentWaterfall` completo con los montos ya calculados.

**Razón**: la lógica de cálculo (porcentaje, monto fijo, etc.) requiere contexto del producto, no del agregado. Mantiene el agregado enfocado en aplicar la prelación, no en calcularla.

---

### Decisión 4: Modo SeparateCollection — efecto contable, no en el préstamo

Si `CollectionMode = SeparateCollection`:
- El capital social NO se deduce del monto del pago del préstamo.
- El monto del pago se aplica íntegro a los componentes del préstamo (interés, principal, etc.).
- El capital social se registra en el evento `PaymentApplied` como `SocialCapitalContributed` informativo.
- El handler actualiza el saldo del socio de todas formas.
- No se genera un segundo débito automático — eso es responsabilidad del sistema contable externo.

Si `CollectionMode = IncludedInPayment`:
- El capital social se descuenta del monto recibido **antes** de iniciar la prelación del préstamo.
- El waterfall opera sobre `amount - socialCapitalContribution`.

---

### Decisión 5: Acumulación de capital social en cooperative_members, proyectada desde PaymentApplied

En lugar de un agregado separado `CapitalSocialAggregate`, se mantiene como campo `social_capital_balance` en `cooperative_members` y se actualiza mediante un proyector que escucha el evento `PaymentApplied`.

**Razón**: la consulta de capital social es una lectura simple. El Event Store guarda la historia completa si se necesita recalcular. Un agregado separado sería sobreingeniería para este caso.

---

### Decisión 6: Waterfall serializado como JSONB en credit_products

El waterfall y la config de capital social se almacenan como `JSONB` en la tabla `credit_products`. No se normalizan en tablas separadas.

**Razón**: son configuraciones del producto que se leen juntas. JSONB permite esquema flexible sin migraciones adicionales cuando se agreguen nuevos tipos de cálculo.

---

### Decisión 7: PaymentWaterfall.Default como constante de dominio

```csharp
public static PaymentWaterfall Default => new([
    new(1, PaymentComponent.Fees),
    new(2, PaymentComponent.PenaltyInterest),
    new(3, PaymentComponent.RegularInterest),
    new(4, PaymentComponent.Principal),
]);
```

Garantiza retrocompatibilidad sin cambios en base de datos para préstamos sin producto.

## Risks / Trade-offs

**[Risk] Tests existentes que llaman `ApplyPayment()` directamente rompen** → Mitigación: pasar `PaymentWaterfall.Default` en todos los tests afectados. Es un cambio mecánico, no semántico.

**[Risk] Préstamos activos sin `ProductId` asignado** → Mitigación: el handler usa `PaymentWaterfall.Default` cuando el préstamo no tiene producto. El capital social es ₡0 en esos casos.

**[Risk] El cálculo del capital social puede exceder el monto disponible tras la prelación** → Mitigación: si `CollectionMode = IncludedInPayment`, el capital social se descuenta primero. Si el monto total del pago es insuficiente para cubrir capital social + deuda, se prioriza la deuda y el capital social se registra como ₡0 en ese pago.

**[Risk] Inconsistencia de saldo de capital social si se reprocesa el event store** → Mitigación: el proyector es idempotente (usa `ON CONFLICT DO UPDATE` con suma incremental basada en versión del evento). Reconstrucción completa del saldo siempre es posible desde el event store.

## Migration Plan

1. Ejecutar `20260810_AddSocialCapitalAndWaterfall.sql`:
   - `ALTER TABLE credit_products ADD COLUMN waterfall_config JSONB`
   - `ALTER TABLE credit_products ADD COLUMN social_capital_config JSONB`
   - `ALTER TABLE cooperative_members ADD COLUMN social_capital_balance DECIMAL(18,2) NOT NULL DEFAULT 0`
   - `ALTER TABLE rm_payment_history ADD COLUMN social_capital_contributed DECIMAL(18,2) NOT NULL DEFAULT 0`
2. Los productos existentes quedan con `waterfall_config = NULL` → el handler usa `PaymentWaterfall.Default`.
3. El saldo de capital social de socios existentes inicia en 0. No se recalcula retroactivamente (los pagos anteriores no tenían capital social).
4. Rollback: revertir la migración SQL y desplegar la versión anterior. Los campos JSONB NULL no afectan la lógica legacy.

## Open Questions

- ¿Los seguros (`Insurance`) como componente del waterfall requieren configuración propia (cálculo de prima) o se gestionan como una fee fija externa al sistema? → Por ahora se incluye `Insurance` en el enum pero sin configuración específica; actúa igual que `Fees` (cobra lo que esté en `TotalFees`). Se puede especializar en una iteración futura.
- ¿Se requiere un reporte histórico de capital social por socio (cuánto aportó en cada pago)? → El event store lo tiene disponible. Por ahora solo se expone el saldo total acumulado. Un reporte detallado queda como mejora futura.
