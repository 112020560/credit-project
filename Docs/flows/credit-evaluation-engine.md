# Motor de Evaluación Crediticia

Documentación del flujo de reglas que evalúa si un préstamo debe ser aprobado o rechazado, cómo se calcula la tasa final y cómo se determina el monto máximo financiable.

---

## Flujo de evaluación (ejemplo real)

```
Solicitud: amount=1,500,000 | income=850,000/mes | debt=150,000/mes | term=24m | rate_producto=14.5%
```

```
POST /loans
       │
       ├─ [Handler] Cliente existe en customer_credit_profiles? ──No──► 400 "Customer not found"
       ├─ [Handler] Producto existe y está Active?              ──No──► 400 "Product not found/inactive"
       └─ [Handler] RequireActiveMembership=true y no es socio? ──Si──► 400 "Not a member"
                                       │
                                       ▼ pasa al motor de reglas
       ┌───────────────────────────────────────────────────────────┐
       │              C O N T R A C T   E N G I N E               │
       └───────────────────────────────────────────────────────────┘
                                       │
              ┌────────────────────────▼────────────────────────┐
              │  [P0] MemberSharesRule                          │
              │                                                 │
              │  aportaciones=5,000  multiplier=5               │
              │  límite = 5,000 × 5 = 25,000                   │
              │                                                 │
              │  enforce_shares_capacity_limit = false          │
              │  → 1,500,000 > 25,000 pero NO bloquea          │
              │  → reporta "Shares limit exceeded (informative)"│
              └────────────────────────┬────────────────────────┘
                                       │ Pass (informativo)
              ┌────────────────────────▼────────────────────────┐
              │  [P1] ProductEligibilityRule          HARDSTOP  │
              │                                                 │
              │  product.minAmount=100,000                      │
              │  product.maxAmount=5,000,000                    │
              │  product.minTerm=6  product.maxTerm=60          │
              │                                                 │
              │  100,000 ≤ 1,500,000 ≤ 5,000,000 → OK          │
              │  6 ≤ 24 ≤ 60 → OK                              │
              └────────────────────────┬────────────────────────┘
                                       │ Pass
              ┌────────────────────────▼────────────────────────┐
              │  [P1] CreditScoreRule                 HARDSTOP  │
              │                                                 │
              │  score = 720  mínimo = 500                      │
              │  720 ≥ 500 → OK                                 │
              │  tramo 700–749 → ajuste tasa: +0.0%            │
              └────────────────────────┬────────────────────────┘
                                       │ Pass (+0%)
              ┌────────────────────────▼────────────────────────┐
              │  [P2] DebtToIncomeRule                HARDSTOP  │
              │                                                 │
              │  cuota_estimada(1,500,000 | 14.5% | 24m)        │
              │    = 73,156/mes                                 │
              │                                                 │
              │  DTI = (150,000 + 73,156) / 850,000            │
              │      = 223,156 / 850,000 = 26.3%               │
              │                                                 │
              │  max_dti_ratio = 0.50 (50%)                     │
              │  zona_warning  = 0.50 × 0.80 = 40%             │
              │                                                 │
              │  26.3% ≤ 40% → OK sin penalización             │
              └────────────────────────┬────────────────────────┘
                                       │ Pass (+0%)
              ┌────────────────────────▼────────────────────────┐
              │  [P2] PaymentCapacityRule             HARDSTOP  │
              │                                                 │
              │  cuota_max = ingreso × max_dti − deuda_exist    │
              │            = 850,000 × 0.50 − 150,000          │
              │            = 425,000 − 150,000                  │
              │            = 275,000/mes disponibles            │
              │                                                 │
              │  r = 14.5% / 12 / 100 = 0.012083/mes           │
              │  PV = 275,000 × [(1−(1+r)^−24) / r]            │
              │     = 275,000 × 21.915                          │
              │     ≈ 5,477,000                                 │
              │                                                 │
              │  1,500,000 ≤ 5,477,000 → OK                    │
              └────────────────────────┬────────────────────────┘
                                       │ Pass
              ┌────────────────────────▼────────────────────────┐
              │  [P3] CollateralRule              informativa    │
              │                                                 │
              │  garantía = FianzaSolidaria                     │
              │  appraisalValue=2,000,000 × coverageRate=0.80   │
              │  colateral_efectivo = 1,600,000                 │
              │                                                 │
              │  ratio = 1,600,000 / 1,500,000 = 1.067x        │
              │  1.0 ≤ ratio < 1.2 → descuento: −0.5%          │
              └────────────────────────┬────────────────────────┘
                                       │ Pass (−0.5%)
              ┌────────────────────────▼────────────────────────┐
              │  [P4] ActiveLoansRule             informativa    │
              │                                                 │
              │  sin préstamos activos → sin penalización       │
              └────────────────────────┬────────────────────────┘
                                       │ Pass (+0%)
                          ┌────────────▼────────────┐
                          │  RESULTADO FINAL         │
                          │                          │
                          │  tasa_base    = 14.50%   │
                          │  + CreditScore =  0.00%  │
                          │  + DTI         =  0.00%  │
                          │  + Colateral   = −0.50%  │
                          │  + ActiveLoans =  0.00%  │
                          │  ─────────────────────── │
                          │  tasa_aprobada = 14.00%  │
                          │                          │
                          │     ✓ APROBADO           │
                          └──────────────────────────┘
```

---

## Cuándo se rechaza (casos de fallo)

```
┌─────────────────────────────────────────────────────────────────┐
│ Regla              │ Condición de rechazo                       │
├─────────────────────────────────────────────────────────────────┤
│ MemberSharesRule   │ Solo si enforce_shares_capacity_limit=true  │
│                    │ Y monto > aportaciones × multiplier         │
├─────────────────────────────────────────────────────────────────┤
│ ProductEligibility │ monto < minAmount o monto > maxAmount       │
│                    │ plazo < minTerm o plazo > maxTerm           │
│                    │ sin garantía si product.requiresCollateral  │
│                    │ LTV > product.maxLtv                        │
├─────────────────────────────────────────────────────────────────┤
│ CreditScoreRule    │ score < 500                                 │
│                    │ sin score y policy.noScoreBehavior=reject   │
├─────────────────────────────────────────────────────────────────┤
│ DebtToIncomeRule   │ DTI > max_dti_ratio                        │
│                    │ (deuda_exist + cuota_nueva) / ingreso > 50% │
├─────────────────────────────────────────────────────────────────┤
│ PaymentCapacity    │ monto > PV(cuota_max, tasa_producto, plazo) │
│                    │ cuota_max = ingreso×dti_max − deuda_exist   │
│                    │ también falla si cuota_max ≤ 0             │
└─────────────────────────────────────────────────────────────────┘
```

---

## El rol de `max_dti_ratio`

El `max_dti_ratio` (configurado en `underwriting_policies`) es el **único parámetro que controla la capacidad de endeudamiento** del cliente. Aparece en dos reglas:

### En `DebtToIncomeRule` — filtro de flujo de caja

Verifica que la cuota del préstamo solicitado no lleve al cliente sobre el límite:

```
DTI = (deuda_mensual_existente + cuota_estimada_nueva) / ingreso_mensual
               ≤ max_dti_ratio  →  PASA
               > max_dti_ratio  →  RECHAZA (hard stop)
```

### En `PaymentCapacityRule` — techo de monto financiable

Calcula cuánto es lo máximo que se puede prestar dado el ingreso disponible:

```
cuota_max   = ingreso × max_dti_ratio − deuda_existente
monto_max   = PV(cuota_max | tasa_producto | plazo)    ← valor presente
```

Si el solicitante pide más de ese monto máximo → hard stop.

### Efecto del ratio sobre el monto máximo aprobable

Ejemplo: ingreso=₡850,000 | deuda=₡150,000 | plazo=24m | tasa=14.5%

| `max_dti_ratio` | Cuota disponible | **Monto máximo aprobable** |
|---|---|---|
| `0.30` | ₡105,000/mes | ₡2,091,000 |
| `0.40` | ₡190,000/mes | ₡3,784,000 |
| **`0.50`** | **₡275,000/mes** | **₡5,477,000** |
| `0.60` | ₡360,000/mes | ₡7,169,000 |

Para ajustar la política de crédito de la cooperativa:

```sql
-- Política conservadora (cooperativas pequeñas)
UPDATE underwriting_policies SET max_dti_ratio = 0.40 WHERE id = 'default';

-- Política estándar (default)
UPDATE underwriting_policies SET max_dti_ratio = 0.50 WHERE id = 'default';

-- Política agresiva (con garantías fuertes)
UPDATE underwriting_policies SET max_dti_ratio = 0.60 WHERE id = 'default';
```

---

## Ajustes de tasa por regla

Las reglas informativas no rechazan pero sí modifican la tasa aprobada:

| Regla | Condición | Ajuste |
|---|---|---|
| `CreditScoreRule` | score ≥ 750 | +0.0% |
| | score 700–749 | +1.5% |
| | score 650–699 | +3.0% |
| | score 600–649 | +5.0% |
| | score 550–599 | +8.0% |
| | score 500–549 | +12.0% |
| | sin score (approve_with_penalty) | +5.0% |
| `DebtToIncomeRule` | DTI > max_dti×0.80 (zona warning) | +2.0% |
| | DTI ≤ max_dti×0.80 | +0.0% |
| `CollateralRule` | sin colateral | +1.0% |
| | ratio colateral < 1.0x | +0.5% |
| | ratio colateral 1.0x–1.2x | −0.5% |
| | ratio colateral ≥ 1.2x | −1.5% |
| `ActiveLoansRule` | tiene préstamos activos | +1.0% |
| | sin préstamos activos | +0.0% |

```
tasa_aprobada = tasa_base_producto (o policy.base_interest_rate) + Σ ajustes
```

---

## Reglas de negocio configurables en BD

Todas las políticas se modifican en `underwriting_policies WHERE id = 'default'` — sin cambios de código:

| Campo | Afecta |
|---|---|
| `base_interest_rate` | Tasa base cuando el producto no tiene tasa propia |
| `max_dti_ratio` | Techo de DTI y cálculo de monto máximo (PaymentCapacity) |
| `shares_multiplier_limit` | Límite informativo de aportaciones |
| `enforce_shares_capacity_limit` | Si el límite de aportaciones es hard stop |
| `require_active_membership` | Si exige membresía cooperativa activa |
| `no_score_behavior` | `approve_with_penalty` o `reject` sin score |
| `auto_default_threshold_days` | Días de mora para auto-default |
| `grace_period_days` | Días de gracia antes de registrar mora |
| `penalty_rate` | Tasa de mora |
| `origination_fee_rate` | Comisión de apertura |
