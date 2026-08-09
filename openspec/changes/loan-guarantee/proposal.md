## Why

El colateral hoy es un `decimal?` sin estructura ni trazabilidad: no distingue entre una hipoteca, una prenda vehicular o un fiador solidario, y no permite calcular la cobertura efectiva según las políticas de la cooperativa. Sin garantías como entidad del dominio, el sistema no puede cumplir con los requisitos de gestión de riesgo crediticio ni con SUGEF 1-05.

## What Changes

- Crear la entidad `LoanGuarantee` en el dominio con tipo, descripción, valor de avalúo y porcentaje de cobertura
- Crear enum `GuaranteeType` (Hipoteca, Prenda, FianzaSolidaria, DepósitoAPlazo, CesiónDeDerecho)
- Crear value object `GuaranteeValuation` (AppraisalValue + CoverageRate) con validación
- Crear enum `GuaranteeStatus` (Vigente, Liberada, Ejecutada)
- Exponer `ILoanGuaranteeRepository` con operaciones de alta, consulta y actualización de estado
- Actualizar `ContractEvaluationContext.CollateralValue` para derivarse de las garantías (suma de AppraisalValue × CoverageRate)
- **BREAKING**: `CollateralRule` y `ProductEligibilityRule` pasan a usar `CollateralValue` calculado desde garantías, no un decimal directo
- Endpoints REST para registrar, listar y actualizar estado de garantías por contrato
- Migración SQL `loan_guarantees` con FK a `loan_contract_summaries`

## Capabilities

### New Capabilities

- `loan-guarantee`: Entidad LoanGuarantee con tipo, valoración y estado; repositorio Dapper; endpoints CRUD; migración SQL

### Modified Capabilities

- `loan-contract`: El proceso de creación de contrato puede recibir garantías iniciales; `CollateralValue` en el contexto de evaluación se calcula desde las garantías
- `underwriting-policy`: `CollateralRule` y `ProductEligibilityRule` usan el valor de cobertura efectivo derivado de las garantías

## Impact

- **Domain**: `LoanGuarantee`, `GuaranteeType`, `GuaranteeStatus`, `GuaranteeValuation`, `ILoanGuaranteeRepository`, `ContractEvaluationContext`
- **Infrastructure**: `LoanGuaranteeRepository` (Dapper), migración SQL `loan_guarantees`
- **Application**: `CreateContractCommandHandler` (puede pasar garantías al contexto de evaluación)
- **API**: nuevos endpoints `GuaranteeEndpoints.cs`
- **Tests**: `GuaranteeValuationTests`, actualización de `CollateralRuleTests`, `ProductEligibilityRuleTests`
