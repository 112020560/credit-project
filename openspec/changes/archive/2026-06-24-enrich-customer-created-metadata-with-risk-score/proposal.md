## Why

Al convertir un prospecto a cliente vía el flujo de originación de crédito, el evento `CustomerCreated` se publica con `Metadata = null`, perdiendo el score de riesgo calculado durante la evaluación. El `TotalScore` de la `RiskEvaluation` ya está persistido en DB en el momento de la aprobación y debe incluirse en `Metadata["CreditScore"]` para que los servicios consumidores (crédito, retail) reciban el perfil financiero del cliente desde el primer evento.

## What Changes

- En `SubmitCreditApplicationCommand` (path AutoApprove): al construir el `CreateCustomerContract`, leer la `RiskEvaluation` del `CreditApplication` y poblar `Metadata["CreditScore"]` con `evaluation.TotalScore`.
- En `ApprovalWorkflowService` (path ManualReview aprobado): al construir el `CreateCustomerContract`, leer la `RiskEvaluation` más reciente del `CreditApplication` y poblar `Metadata["CreditScore"]` con `evaluation.TotalScore`.
- El path directo (`POST /api/v1/customers`) no se modifica — ya funciona correctamente.

## Capabilities

### New Capabilities

_(ninguna)_

### Modified Capabilities

- `customer-management`: El evento `CustomerCreated` emitido desde el path de conversión ahora incluye `Metadata["CreditScore"]` con el `TotalScore` de la evaluación de riesgo.
- `prospect-to-customer-conversion`: La conversión de prospecto a cliente ahora enriquece el evento `CustomerCreated` con datos del score de riesgo calculado.

## Impact

- **Handlers afectados**: `SubmitCreditApplicationCommand`, `ApprovalWorkflowService`
- **Repositorio requerido**: `IRiskEvaluationsRepository` — necesita un método para obtener la evaluación más reciente por `CreditApplicationId`
- **Sin cambios en contratos HTTP**: ningún endpoint cambia su request/response
- **Sin cambios de schema en DB**: no se agrega ninguna columna ni migración
- **Sin cambios en el path directo** (`CreateCustomerCommand`)
