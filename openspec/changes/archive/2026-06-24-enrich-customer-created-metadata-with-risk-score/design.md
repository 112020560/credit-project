## Context

Cuando un prospecto se convierte a cliente vía el flujo de originación, el `CreateCustomerContract` se construye con `Metadata = null`. Sin embargo, la `RiskEvaluation` del `CreditApplication` ya está persistida en DB antes de que ocurra la conversión — fue calculada por `RiskEvaluationService.EvaluateAsync()` durante `SubmitCreditApplicationCommand`.

Existen dos paths de conversión donde aplica el cambio:
1. **AutoApprove** (`SubmitCreditApplicationCommand`): la `evaluation` ya está en memoria como resultado de `EvaluateAsync()`.
2. **ManualReview aprobado** (`ApprovalWorkflowService`): no hay `evaluation` en scope — debe cargarse desde DB vía `IRiskEvaluationsRepository`.

`IUnitOfWork.RiskEvaluationsRepository.GetByCreditApplicationIdAsync(id)` ya existe y retorna la lista de evaluaciones para un `CreditApplication`.

## Goals / Non-Goals

**Goals:**
- Incluir `Metadata["CreditScore"] = evaluation.TotalScore` en el `CreateCustomerContract` emitido durante la conversión.
- Mínimo cambio posible: reutilizar infraestructura existente, sin nuevas migraciones ni contratos.

**Non-Goals:**
- No modificar el path directo (`CreateCustomerCommand`).
- No incluir otros campos de la evaluación (`SuggestedInterestRate`, `SuggestedMaxAmount`) en este cambio.
- No agregar `CreditScore` como columna en la entidad `Customer` — solo va en el payload del evento.

## Decisions

### D1 — En `SubmitCreditApplicationCommand` usar el objeto `evaluation` ya en memoria

**Decisión**: En el path AutoApprove, `evaluation.TotalScore` ya está disponible en scope. Se usa directamente sin hacer ninguna query adicional a DB.

**Rationale**: Es la solución más simple y sin overhead. La evaluación ya está calculada y disponible.

### D2 — En `ApprovalWorkflowService` cargar la evaluación más reciente desde DB

**Decisión**: Llamar `unitOfWork.RiskEvaluationsRepository.GetByCreditApplicationIdAsync(application.Id)` y tomar la evaluación con mayor `EvaluatedAt`. Si no existe evaluación (edge case: aprobación sin evaluación previa), `Metadata` queda `null`.

**Rationale**: `ApprovalWorkflowService` no tiene acceso al resultado de `EvaluateAsync()` porque la evaluación ocurrió en un request previo. `GetByCreditApplicationIdAsync` ya existe, no requiere nuevo método.

**Alternativa descartada**: Pasar el `TotalScore` como parámetro al método `RecordDecisionAsync`. Aumentaría el acoplamiento del servicio con el caller y cambiaría la firma pública de la clase.

### D3 — `Metadata` es `null` si no existe evaluación

**Decisión**: Si `GetByCreditApplicationIdAsync` retorna lista vacía, el `CreateCustomerContract` se construye con `Metadata = null`, igual que antes.

**Rationale**: Es un edge case que no debería ocurrir en producción (toda aprobación pasa por evaluación de riesgo), pero es más seguro que lanzar excepción.

## Risks / Trade-offs

**[Riesgo] Múltiples evaluaciones por CreditApplication**
→ Mitigación: tomar la de mayor `EvaluatedAt`. `TriggerRiskEvaluationCommand` puede ejecutarse más de una vez (re-evaluación manual), pero la última es la relevante para la decisión de aprobación.

**[Riesgo] Aprobación sin evaluación previa (flujo de emergencia)**
→ Mitigación: `Metadata = null` — comportamiento silencioso igual al estado actual. No es regresión.
