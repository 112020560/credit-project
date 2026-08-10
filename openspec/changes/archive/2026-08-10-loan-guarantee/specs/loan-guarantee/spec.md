## ADDED Requirements

### Requirement: Entidad LoanGuarantee con tipo, valoración y estado
El sistema SHALL modelar las garantías de un préstamo como la entidad `LoanGuarantee` en el domain layer, con los siguientes campos: `Id` (Guid), `LoanContractId` (Guid), `Type` (GuaranteeType), `Description` (string), `Valuation` (GuaranteeValuation), `Status` (GuaranteeStatus), y `ExpirationDate` (DateOnly?).

`GuaranteeType` SHALL ser un enum con los valores: `Hipoteca`, `Prenda`, `FianzaSolidaria`, `DepositoAPlazo`, `CesionDeDerecho`.

`GuaranteeStatus` SHALL ser un enum con los valores: `Vigente`, `Liberada`, `Ejecutada`.

`GuaranteeValuation` SHALL ser un value object inmutable con `AppraisalValue` (decimal, > 0) y `CoverageRate` (decimal, > 0 y <= 1). El valor de cobertura efectivo se calcula como `AppraisalValue × CoverageRate`.

#### Scenario: GuaranteeValuation con valores válidos
- **WHEN** se crea `GuaranteeValuation` con `AppraisalValue = 10_000_000` y `CoverageRate = 0.80`
- **THEN** `EffectiveCoverage` retorna `8_000_000`

#### Scenario: GuaranteeValuation rechaza AppraisalValue no positivo
- **WHEN** se intenta crear `GuaranteeValuation` con `AppraisalValue <= 0`
- **THEN** el constructor lanza `ArgumentException`

#### Scenario: GuaranteeValuation rechaza CoverageRate fuera de rango
- **WHEN** se intenta crear `GuaranteeValuation` con `CoverageRate <= 0` o `CoverageRate > 1`
- **THEN** el constructor lanza `ArgumentException`

#### Scenario: LoanGuarantee rechaza descripción vacía
- **WHEN** se intenta crear `LoanGuarantee` con `Description` nulo o vacío
- **THEN** el constructor lanza `ArgumentException`

---

### Requirement: Repositorio ILoanGuaranteeRepository
El sistema SHALL exponer la interfaz `ILoanGuaranteeRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con los métodos:
- `GetByIdAsync(Guid id, CancellationToken ct)` → `LoanGuarantee?`
- `GetByContractAsync(Guid loanContractId, CancellationToken ct)` → `IEnumerable<LoanGuarantee>`
- `InsertAsync(LoanGuarantee guarantee, CancellationToken ct)` → `Task`
- `UpdateStatusAsync(Guid id, GuaranteeStatus status, CancellationToken ct)` → `Task`

#### Scenario: Consulta de garantías por contrato
- **WHEN** se llama `GetByContractAsync` con un `loanContractId` existente
- **THEN** retorna todas las garantías asociadas a ese contrato en cualquier estado

#### Scenario: Contrato sin garantías retorna lista vacía
- **WHEN** se llama `GetByContractAsync` con un `loanContractId` que no tiene garantías registradas
- **THEN** retorna una colección vacía (no null)

---

### Requirement: Migración SQL loan_guarantees
El sistema SHALL incluir el script `20260808_AddLoanGuaranteesTable.sql` que crea la tabla `loan_guarantees` con las columnas: `id` (UUID PK), `loan_contract_id` (UUID, FK a `loan_contract_summaries.contract_id`), `type` (VARCHAR), `description` (TEXT), `appraisal_value` (NUMERIC 18,2), `coverage_rate` (NUMERIC 6,4), `status` (VARCHAR, default `Vigente`), `expiration_date` (DATE nullable), `created_at` (TIMESTAMPTZ).

La tabla SHALL tener un índice en `loan_contract_id` para las consultas por contrato.

#### Scenario: Migración aplicada exitosamente
- **WHEN** se ejecuta el script de migración
- **THEN** existe la tabla `loan_guarantees` con todas las columnas definidas
- **THEN** existe el índice en `loan_contract_id`

#### Scenario: FK referencia el read model de contratos
- **WHEN** se inserta una garantía con `loan_contract_id` que no existe en `loan_contract_summaries`
- **THEN** la base de datos rechaza la inserción por violación de FK

---

### Requirement: Endpoints REST para gestión de garantías
El sistema SHALL exponer los siguientes endpoints en `GuaranteeEndpoints.cs`:
- `POST /api/loans/{loanContractId}/guarantees` — registrar una nueva garantía para un contrato existente
- `GET /api/loans/{loanContractId}/guarantees` — listar todas las garantías de un contrato
- `PUT /api/loans/{loanContractId}/guarantees/{guaranteeId}/status` — actualizar el estado de una garantía

#### Scenario: Registrar garantía en contrato existente
- **WHEN** se envía `POST /api/loans/{id}/guarantees` con tipo, descripción, valor de avalúo y cobertura válidos
- **THEN** el sistema persiste la garantía con estado `Vigente` y retorna 201 con el ID asignado

#### Scenario: Listar garantías de contrato
- **WHEN** se consulta `GET /api/loans/{id}/guarantees`
- **THEN** el sistema retorna la lista de garantías con sus datos completos y estado actual

#### Scenario: Actualizar estado de garantía
- **WHEN** se envía `PUT /api/loans/{id}/guarantees/{gId}/status` con `Status = "Liberada"`
- **THEN** el sistema actualiza el estado de la garantía y retorna 200

#### Scenario: Garantía de contrato inexistente rechazada
- **WHEN** se intenta registrar una garantía para un `loanContractId` que no existe en `loan_contract_summaries`
- **THEN** el sistema retorna 404
