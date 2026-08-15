## MODIFIED Requirements

### Requirement: Validación de entrada al crear un contrato
El sistema SHALL rechazar solicitudes de creación de contrato que no cumplan con los parámetros mínimos de entrada antes de ejecutar cualquier evaluación crediticia.

Parámetros requeridos:
- `ExternalCustomerId`: GUID no vacío del cliente en el CRM
- `ProductId`: GUID no vacío del producto de crédito solicitado
- `Amount`: decimal > 0
- `Currency`: uno de `USD`, `EUR`, `CRC`
- `TermMonths`: entero entre 1 y 360 (inclusive)
- `AmortizationMethod`: valor válido del enum (default: `French`)
- `Guarantees`: lista opcional de `GuaranteeInput { GuaranteeType, Description, AppraisalValue, CoverageRate }`. Si se provee, cada elemento MUST tener `AppraisalValue > 0` y `0 < CoverageRate <= 1`.

> **Nota**: `CollateralValue` como campo directo ha sido eliminado del command. El valor efectivo de colateral se calcula automáticamente desde la lista `Guarantees`.

#### Scenario: Monto inválido rechazado en validación
- **WHEN** se envía `Amount = 0`
- **THEN** el sistema retorna error de validación sin ejecutar el motor de reglas

#### Scenario: Moneda no soportada rechazada
- **WHEN** se envía `Currency = "MXN"` u otra no listada
- **THEN** el sistema retorna error de validación indicando las monedas permitidas

#### Scenario: Plazo fuera de rango rechazado
- **WHEN** `TermMonths < 1` o `TermMonths > 360`
- **THEN** el sistema retorna error de validación

#### Scenario: GuaranteeInput con AppraisalValue negativo rechazado
- **WHEN** se provee un `GuaranteeInput` con `AppraisalValue <= 0` o `CoverageRate` fuera de `(0, 1]`
- **THEN** el sistema retorna error de validación

#### Scenario: Command sin garantías es válido
- **WHEN** se envía el command con `Guarantees` nulo o lista vacía
- **THEN** la validación pasa y el colateral efectivo es null (préstamo sin garantía)

---

## ADDED Requirements

### Requirement: CollateralValue derivado de garantías al crear contrato
El sistema SHALL calcular el `CollateralValue` efectivo del `ContractEvaluationContext` como la suma de `AppraisalValue × CoverageRate` de todos los `GuaranteeInput` provistos en el command. Si la lista es vacía o nula, `CollateralValue` es null.

Si el contrato es **aprobado**, el handler SHALL persistir cada `GuaranteeInput` como una entidad `LoanGuarantee` con estado `Vigente` vinculada al ID del contrato creado.

Si el contrato es **rechazado**, no se persiste ninguna garantía.

#### Scenario: Colateral efectivo calculado desde dos garantías
- **WHEN** el command incluye `[{AppraisalValue=10_000_000, CoverageRate=0.80}, {AppraisalValue=5_000_000, CoverageRate=1.0}]`
- **THEN** `context.CollateralValue = 10_000_000 × 0.80 + 5_000_000 × 1.0 = 13_000_000`

#### Scenario: Garantías persistidas tras aprobación
- **WHEN** el contrato es aprobado y el command tenía 2 garantías
- **THEN** se persisten 2 registros en `loan_guarantees` con estado `Vigente` vinculados al `ContractId`

#### Scenario: Sin garantías — CollateralValue es null
- **WHEN** el command no incluye garantías
- **THEN** `context.CollateralValue = null` y `CollateralRule` aplica el ajuste por préstamo no garantizado
