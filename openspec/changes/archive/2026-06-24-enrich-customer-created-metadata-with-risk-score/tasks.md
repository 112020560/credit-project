## 1. SubmitCreditApplicationCommand — path AutoApprove

- [x] 1.1 En el bloque `AutoApprove`, construir `Metadata` a partir de `evaluation.TotalScore` antes de crear el `CreateCustomerContract`: `Metadata = new Dictionary<string, object> { ["CreditScore"] = evaluation.TotalScore }`
- [x] 1.2 Verificar que el `OutboxEvent` de `CustomerCreated` (EventType `"CustomerCreated"`) incluye `Metadata["CreditScore"]` en el payload serializado

## 2. ApprovalWorkflowService — path ManualReview aprobado

- [x] 2.1 Al construir el `CreateCustomerContract` en el bloque `allStepsComplete`, cargar evaluaciones con `unitOfWork.RiskEvaluationsRepository.GetByCreditApplicationIdAsync(application.Id, cancellationToken)`
- [x] 2.2 Tomar la evaluación con mayor `EvaluatedAt`: `var latestEval = evaluations.OrderByDescending(e => e.EvaluatedAt).FirstOrDefault()`
- [x] 2.3 Asignar `Metadata = latestEval is not null ? new Dictionary<string, object> { ["CreditScore"] = latestEval.TotalScore } : null` al `CreateCustomerContract`
- [x] 2.4 Verificar que el `OutboxEvent` de `CustomerCreated` incluye `Metadata["CreditScore"]` cuando existe evaluación, y `Metadata` es `null` cuando no existe

## 3. Validación

- [x] 3.1 Build completo sin errores (`dotnet build CrmProject.sln`)
- [ ] 3.2 Crear prospecto → credit application → submit → AutoApprove → verificar en `outbox_db.Events` que el payload de `CustomerCreated` contiene `Metadata.CreditScore` con el score de la evaluación
- [ ] 3.3 Crear prospecto → credit application → submit → ManualReview → aprobar → verificar en `outbox_db.Events` que el payload de `CustomerCreated` contiene `Metadata.CreditScore`
- [ ] 3.4 Verificar que `POST /api/v1/customers` directo sigue funcionando sin regresión en `Metadata`
