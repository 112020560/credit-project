## MODIFIED Requirements

### Requirement: Validación de entrada al crear un contrato
El sistema SHALL rechazar solicitudes de creación de contrato que no cumplan con los parámetros mínimos de entrada antes de ejecutar cualquier evaluación crediticia.

Parámetros requeridos:
- `ExternalCustomerId`: GUID no vacío del cliente en el CRM
- `ProductId`: GUID no vacío del producto de crédito seleccionado
- `Amount`: decimal > 0
- `Currency`: uno de `USD`, `EUR`, `CRC`
- `TermMonths`: entero entre 1 y 360 (inclusive)
- `CollateralValue`: si se provee, MUST ser > 0
- `AmortizationMethod`: valor válido del enum (default: el del producto seleccionado, o `French` si el producto no lo especifica)

> Nota: el rango de `Amount` ya no se valida a nivel de input — la validación de rango se delega a `ProductEligibilityRule` según los límites del producto seleccionado.

#### Scenario: ProductId ausente rechazado en validación
- **WHEN** se envía una solicitud sin `ProductId` o con `ProductId = Guid.Empty`
- **THEN** el sistema retorna error de validación sin ejecutar el motor de reglas

#### Scenario: Monto fuera de rango rechazado en validación
- **WHEN** se envía `Amount <= 0`
- **THEN** el sistema retorna error de validación sin ejecutar el motor de reglas

#### Scenario: Moneda no soportada rechazada
- **WHEN** se envía `Currency = "MXN"` u otra no listada
- **THEN** el sistema retorna error de validación indicando las monedas permitidas

#### Scenario: Plazo fuera de rango rechazado
- **WHEN** `TermMonths < 1` o `TermMonths > 360`
- **THEN** el sistema retorna error de validación

#### Scenario: Colateral con valor negativo rechazado
- **WHEN** se provee `CollateralValue <= 0`
- **THEN** el sistema retorna error de validación

---

### Requirement: Resolución y validación del producto antes de la evaluación crediticia
El sistema SHALL resolver el `CreditProduct` referenciado por `ProductId` y validar su existencia y estado antes de continuar con la evaluación. Si el producto no existe o está `Inactive`, el sistema retorna error sin ejecutar el motor de reglas.

#### Scenario: Producto no encontrado
- **WHEN** `ProductId` no corresponde a ningún `CreditProduct` registrado
- **THEN** el sistema retorna `Success = false` con mensaje "Credit product not found"
- **THEN** no se ejecuta el motor de reglas ni se crea ningún contrato

#### Scenario: Producto inactivo rechazado
- **WHEN** el `CreditProduct` existe pero su `Status = Inactive`
- **THEN** el sistema retorna `Success = false` con mensaje "Credit product is not active"
- **THEN** no se ejecuta el motor de reglas

#### Scenario: Producto activo resuelto correctamente
- **WHEN** el `CreditProduct` existe y está `Active`
- **THEN** el handler lo incluye en `ContractEvaluationContext.Product`
- **THEN** la evaluación continúa normalmente

---

### Requirement: Tasa base efectiva del contrato proviene del producto
El sistema SHALL usar la `BaseInterestRate` del `CreditProduct` como tasa base del contrato cuando el producto la define. Si el producto no define tasa base propia (`null`), se usa `UnderwritingPolicy.BaseInterestRate`.

La tasa base efectiva SHALL pasarse como parámetro al `ContractEngine.EvaluateAsync` en lugar de que el engine la lea directamente de la política.

#### Scenario: Contrato usa tasa base del producto
- **WHEN** `CreditProduct.Rates.BaseInterestRate` es 14% y `UnderwritingPolicy.BaseInterestRate` es 8%
- **THEN** el contrato se evalúa con tasa base 14%
- **THEN** `ApprovedRate = 14% + Σ(ajustes de todas las reglas)`

#### Scenario: Producto sin tasa propia usa tasa de la política
- **WHEN** `CreditProduct.Rates.BaseInterestRate` es null
- **THEN** el contrato se evalúa con `UnderwritingPolicy.BaseInterestRate` como tasa base

#### Scenario: Tasa aprobada persistida en el evento ContractCreated
- **WHEN** el contrato es aprobado
- **THEN** `ContractCreated` persiste la `ApprovedRate` calculada con la tasa base efectiva del producto
- **THEN** cambios futuros a la tasa del producto NO afectan la tasa de contratos ya creados
