## MODIFIED Requirements

### Requirement: CreditProduct porta su UnderwritingPolicyId
El sistema SHALL agregar la propiedad `UnderwritingPolicyId` (string, no nulo) al modelo `CreditProduct`. Esta propiedad referencia el `id` de la política de underwriting que aplica para los contratos de ese producto.

El valor por defecto es `"default"`. Productos existentes en BD son migrados automáticamente con `DEFAULT 'default'` en la columna.

#### Scenario: Producto creado con policy explícita
- **WHEN** `POST /products` recibe `underwritingPolicyId: "hipotecario"`
- **THEN** el producto queda registrado con `underwriting_policy_id = 'hipotecario'`

#### Scenario: Producto creado sin policy — usa default
- **WHEN** `POST /products` no incluye `underwritingPolicyId` en el body
- **THEN** el producto queda registrado con `underwriting_policy_id = 'default'`

#### Scenario: GET /products retorna underwritingPolicyId
- **WHEN** `GET /products` o `GET /products/{id}` es invocado
- **THEN** la respuesta incluye el campo `underwritingPolicyId` para cada producto

---

### Requirement: CreateContractCommandHandler carga la policy del producto
El sistema SHALL modificar `CreateContractCommandHandler` para que resuelva la `UnderwritingPolicy` desde `IUnderwritingPolicyRepository.GetByIdAsync(product.UnderwritingPolicyId)` en tiempo de ejecución, en lugar de recibir la policy como dependencia inyectada desde DI.

Si la policy referenciada por el producto no existe en BD, el handler SHALL retornar error con mensaje claro.

#### Scenario: Contrato creado con policy del producto
- **WHEN** `POST /loans` es procesado para un producto con `underwriting_policy_id = 'conservador'`
- **THEN** el handler carga la política `'conservador'` y la usa en la evaluación
- **THEN** las reglas (`DebtToIncomeRule`, `PaymentCapacityRule`, `CreditScoreRule`, `MemberSharesRule`) usan los parámetros de esa política

#### Scenario: Policy referenciada no existe
- **WHEN** el producto referencia `underwriting_policy_id = 'inexistente'` y esa fila no existe en `underwriting_policies`
- **THEN** el handler retorna error indicando que la política del producto no fue encontrada
- **THEN** no se ejecuta el motor de reglas

#### Scenario: Producto sin policy explícita usa 'default'
- **WHEN** el producto tiene `underwriting_policy_id = 'default'`
- **THEN** el handler carga la fila `id = 'default'` de `underwriting_policies`
- **THEN** el comportamiento es idéntico al comportamiento anterior del singleton

---

### Requirement: UnderwritingPolicy propagada al motor via ContractEvaluationContext
El sistema SHALL agregar el campo `UnderwritingPolicy Policy` al record `ContractEvaluationContext`. El handler asigna la policy cargada al contexto antes de invocar `ContractEngine.EvaluateAsync`. Las reglas que necesitan policy (`DebtToIncomeRule`, `PaymentCapacityRule`, `CreditScoreRule`, `MemberSharesRule`) leen `context.Policy` en lugar de recibirla por constructor.

#### Scenario: Reglas acceden a la policy desde el contexto
- **WHEN** `ContractEngine.EvaluateAsync` es invocado con un contexto que tiene `Policy` asignada
- **THEN** `DebtToIncomeRule` usa `context.Policy.MaxDtiRatio`
- **THEN** `PaymentCapacityRule` usa `context.Policy.MaxDtiRatio`
- **THEN** `CreditScoreRule` usa `context.Policy.NoScoreBehavior`
- **THEN** `MemberSharesRule` usa `context.Policy.EnforceSharesCapacityLimit`

#### Scenario: Migración SQL aplicada
- **WHEN** se ejecuta `20260812_AddUnderwritingPolicyIdToProducts.sql`
- **THEN** la tabla `credit_products` tiene columna `underwriting_policy_id VARCHAR NOT NULL DEFAULT 'default'`
- **THEN** existe FK de `credit_products.underwriting_policy_id` a `underwriting_policies.id`
- **THEN** todos los productos existentes tienen `underwriting_policy_id = 'default'`
