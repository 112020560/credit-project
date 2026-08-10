## 1. Domain — Enums y Value Object

- [x] 1.1 Crear enum `GuaranteeType` (`Hipoteca`, `Prenda`, `FianzaSolidaria`, `DepositoAPlazo`, `CesionDeDerecho`) en `CreditSystem.Domain/Enums/`
- [x] 1.2 Crear enum `GuaranteeStatus` (`Vigente`, `Liberada`, `Ejecutada`) en `CreditSystem.Domain/Enums/`
- [x] 1.3 Crear value object `GuaranteeValuation` (`AppraisalValue: decimal`, `CoverageRate: decimal`) con validación en constructor y propiedad `EffectiveCoverage = AppraisalValue × CoverageRate` en `CreditSystem.Domain/ValueObjects/`

## 2. Domain — Entidad e Interfaz de Repositorio

- [x] 2.1 Crear entidad `LoanGuarantee` en `CreditSystem.Domain/Entities/` con campos: `Id`, `LoanContractId`, `Type`, `Description`, `Valuation`, `Status`, `ExpirationDate`
- [x] 2.2 Implementar constructor de `LoanGuarantee` con validación básica (descripción no vacía, valuation no nulo)
- [x] 2.3 Crear interfaz `ILoanGuaranteeRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con métodos `GetByIdAsync`, `GetByContractAsync`, `InsertAsync`, `UpdateStatusAsync`

## 3. Infrastructure — Repositorio y Migración

- [x] 3.1 Crear script de migración `20260808_AddLoanGuaranteesTable.sql` con tabla `loan_guarantees`, FK a `loan_contract_summaries.contract_id`, e índice en `loan_contract_id`
- [x] 3.2 Implementar `LoanGuaranteeRepository` en `CreditSystem.Infrastructure/Repositories/` usando Dapper para todos los métodos de `ILoanGuaranteeRepository`
- [x] 3.3 Registrar `ILoanGuaranteeRepository → LoanGuaranteeRepository` en `DependencyInjection.cs`

## 4. Application — Actualización del Command de Creación de Contrato

- [x] 4.1 Agregar record `GuaranteeInput` (`GuaranteeType Type`, `string Description`, `decimal AppraisalValue`, `decimal CoverageRate`) en el namespace del command
- [x] 4.2 Reemplazar `CollateralValue: decimal?` por `Guarantees: IReadOnlyList<GuaranteeInput>?` en `CreateContractCommand`
- [x] 4.3 Actualizar `CreateContractCommandValidator`: eliminar validación de `CollateralValue`, agregar validación de cada `GuaranteeInput` (AppraisalValue > 0, CoverageRate en (0, 1])
- [x] 4.4 Actualizar `CreateContractCommandHandler`: calcular `effectiveCollateral = Σ(g.AppraisalValue × g.CoverageRate)` desde `request.Guarantees` y asignarlo a `context.CollateralValue`
- [x] 4.5 En `CreateContractCommandHandler`: si el contrato es aprobado, persistir cada `GuaranteeInput` como `LoanGuarantee` con estado `Vigente` vinculado al `ContractId` usando `ILoanGuaranteeRepository`

## 5. API — Endpoints de Garantías

- [x] 5.1 Crear `GuaranteeEndpoints.cs` en `CreditSystem.Api/EndPoints/` con `POST /api/loans/{loanContractId}/guarantees`, `GET /api/loans/{loanContractId}/guarantees` y `PUT /api/loans/{loanContractId}/guarantees/{guaranteeId}/status`
- [x] 5.2 Crear DTOs: `CreateGuaranteeRequest`, `UpdateGuaranteeStatusRequest`, `GuaranteeResponse`
- [x] 5.3 En el handler del `POST`: verificar que el contrato existe en `loan_contract_summaries` antes de insertar; retornar 404 si no existe
- [x] 5.4 Registrar `app.MapGuaranteeEndpoints()` en `Program.cs`

## 6. Tests

- [x] 6.1 Agregar tests unitarios de `GuaranteeValuation`: `EffectiveCoverage` correcto, rechazo de `AppraisalValue <= 0`, rechazo de `CoverageRate` fuera de `(0, 1]`
- [x] 6.2 Agregar tests unitarios de `LoanGuarantee`: constructor rechaza descripción vacía, estado inicial `Vigente`
- [x] 6.3 Actualizar tests de `CreateContractCommandHandler`: contrato sin garantías usa `CollateralValue = null`, contrato con garantías calcula `CollateralValue` efectivo correctamente, garantías se persisten tras aprobación
