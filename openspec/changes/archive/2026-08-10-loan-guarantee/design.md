## Context

Actualmente el colateral en el sistema es un único valor `decimal? CollateralValue` en `ContractEvaluationContext`, pasado directamente desde `CreateContractCommand`. No hay distinción entre tipos de garantía, no se almacena en base de datos, y no hay trazabilidad post-aprobación. Las reglas `CollateralRule` y `ProductEligibilityRule` operan sobre ese decimal sin saber si es una hipoteca, una prenda o un fiador.

Una cooperativa costarricense gestiona garantías como entidades auditables: cada garantía tiene tipo legal, bien o garante, valor de avalúo y porcentaje de cobertura aceptado por la institución. El valor de cobertura efectivo para un contrato es la suma de `AppraisalValue × CoverageRate` de todas las garantías vigentes.

## Goals / Non-Goals

**Goals:**
- Modelar `LoanGuarantee` como entidad de dominio con tipo, valoración y estado
- Calcular `CollateralValue` automáticamente desde las garantías provistas al crear el contrato
- Persistir garantías en base de datos vinculadas al contrato aprobado
- Exponer endpoints para registrar, listar y actualizar garantías post-aprobación
- Mantener `CollateralRule` y `ProductEligibilityRule` sin cambio de lógica (solo cambia la fuente del valor)

**Non-Goals:**
- Event sourcing para garantías (no son agregados con ciclo de vida complejo)
- Valoración dinámica de activos (ej: precio de mercado actualizado automáticamente)
- Integración con registros externos (Registro Público, BCCR)
- Multi-garantía con subrogación o prelación entre acreedores

## Decisions

### 1. LoanGuarantee es entidad, no agregado

Las garantías no tienen ciclo de vida con múltiples comandos concurrentes ni necesitan audit trail por evento. Son datos relacionales simples que cambian de estado (Vigente → Liberada/Ejecutada) con operaciones administrativas. Se usa CRUD directo vía `ILoanGuaranteeRepository` (Dapper).

**Alternativa descartada**: modelarlas como parte del `LoanContractAggregate` (event sourcing). Descartado porque inflaría el agregado con datos que no afectan el ciclo de vida del préstamo y complicaría la rehidratación.

### 2. Garantías se proveen al crear el contrato, se persisten si es aprobado

`CreateContractCommand` recibe una lista de `GuaranteeInput { GuaranteeType, Description, AppraisalValue, CoverageRate }`. El handler calcula el `CollateralValue` efectivo para el motor de reglas. Si el contrato es aprobado, las garantías se persisten como `LoanGuarantee` en la base de datos. Si es rechazado, no se persiste nada.

Esto permite que `CollateralRule` y `ProductEligibilityRule` sigan usando `context.CollateralValue` sin cambios — solo cambia cómo se calcula ese valor antes de invocar el motor.

**Alternativa descartada**: Registrar garantías solo post-aprobación (endpoint separado). Descartado porque deja la evaluación sin el valor de cobertura real, volviendo al mismo problema actual.

### 3. FK a loan_contract_summaries (read model) en tabla loan_guarantees

La tabla `loan_guarantees` referencia `loan_contract_id` que apunta a la proyección `loan_contract_summaries`. Esto es un acoplamiento controlado aceptable: las garantías son datos operativos, no de auditoría, y la proyección es la fuente de consulta del estado del contrato.

**Riesgo**: si la proyección está desactualizada al registrar una garantía post-aprobación, el FK podría no existir. Mitigación: el handler verifica la existencia del contrato en el read model antes de registrar garantías adicionales.

### 4. CoverageRate como fracción decimal

`CoverageRate` se almacena como fracción decimal (`0.80 = 80%`), consistente con `MaxLtv` en `ProductRates`. El valor efectivo de cobertura: `EffectiveCoverage = AppraisalValue × CoverageRate`.

### 5. GuaranteeInput en el command (no la entidad completa)

El command usa un DTO simple `GuaranteeInput` para no exponer la entidad de dominio en la capa Application. El handler construye `LoanGuarantee` con el ID del contrato aprobado.

## Risks / Trade-offs

- **Colateral sin garantías formales**: Si el comando llega sin `Guarantees`, `CollateralValue = null` y `CollateralRule` aplica el ajuste +1% (préstamo no garantizado). Esto es el comportamiento actual — sin regresión.
- **Eventual consistency de proyecciones**: Al registrar garantías adicionales post-aprobación, el endpoint verifica el contrato en `loan_contract_summaries`. Si la proyección aún no procesó el evento, el registro falla. Mitigación: retry en cliente o pequeño delay antes de llamar al endpoint.
- **Sin validación de valor de avalúo en tiempo real**: CoverageRate y AppraisalValue los provee el oficial de crédito. El sistema no valida si el valor es razonable respecto al mercado — eso es responsabilidad del proceso operativo (perito avaluador).

## Migration Plan

1. Ejecutar `20260808_AddLoanGuaranteesTable.sql` en la base de datos
2. Desplegar el nuevo servicio (los campos de garantías en el command son opcionales, sin breaking change en API)
3. Registrar garantías de contratos existentes mediante el nuevo endpoint si se requiere historial retroactivo
