# Flujo de Negocio — Originación Crediticia

Describe el ciclo de vida completo de un prospecto desde su registro hasta su conversión en cliente, los estados posibles de cada entidad y los eventos de dominio que se publican en cada transición.

---

## Flujo de principio a fin

```
[REGISTRO]          [ENRIQUECIMIENTO]      [SOLICITUD]         [DOCUMENTOS]
   │                      │                    │                    │
Crear Prospecto ──► Enriquecer Prospecto ──► Crear Solicitud ──► Adjuntar Docs
(Draft)              (Draft)                  (Draft)             (Draft)
                                                                    │
                                                           ┌── Validar Doc ──► Validated
                                                           └────────────────► Rejected
                                                                    │
                                              [SUBMIT]             ▼
                                         ──► POST /submit ◄────────┘
                                                  │
                                    ┌─────────────┼─────────────┐
                              score >= 95    score 21–94    score <= 20
                                  │               │               │
                            AutoApprove     ManualReview     AutoReject
                                  │               │               │
                            ┌─────┘         InReview        Rejected ◄──── Prospect: Draft
                            │               │
                            │         [WORKFLOW]
                            │         POST /approve (paso 1)
                            │               │
                            │         ¿más pasos?
                            │          SI ──┘ (sigue InReview)
                            │          NO ──┐
                            │               │
                            └───────────────┘
                                  │
                            [CONVERSIÓN]
                         CreditApplication: Approved
                         Prospect: Converted
                         Customer: Active  ◄──── NUEVO CLIENTE
```

---

## Estados de las entidades

### Prospect

| Estado | Significado | Transiciones posibles |
|--------|------------|----------------------|
| `Draft` | Recién creado o rechazado (puede volver a aplicar) | → Submitted, → Converted |
| `Submitted` | Solicitud enviada, en evaluación manual | → Converted, → Draft (si rechazado) |
| `Converted` | Convertido en cliente. Estado final | — |

```
Draft ──► Submitted ──► Converted
  ▲           │
  └───────────┘  (rechazo regresa a Draft)
```

### CreditApplication

| Estado | Significado | Transiciones posibles |
|--------|------------|----------------------|
| `Draft` | Borrador, se pueden agregar documentos | → Submitted* |
| `Submitted` | No usado actualmente (submit va directo a Approved/InReview/Rejected) | — |
| `InReview` | En revisión manual, workflow activo | → Approved, → Rejected |
| `Approved` | Aprobada. Estado final | — |
| `Rejected` | Rechazada. Estado final | — |

> \* El `POST /submit` evalúa el riesgo y transiciona directamente a `Approved`, `InReview` o `Rejected`. El estado `Submitted` existe en el modelo pero el motor salta sobre él.

```
Draft ──► (submit)
              ├──► Approved   (AutoApprove, score >= umbral)
              ├──► InReview   (ManualReview, score en zona media)
              │        ├──► Approved  (último paso del workflow aprobado)
              │        └──► Rejected  (cualquier paso rechazado)
              └──► Rejected   (AutoReject, score <= umbral)
```

---

## Eventos de dominio por acción

### 1. Crear Prospecto
`POST /api/v1/prospects`

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| *(ninguno)* | — | El registro inicial no emite eventos |

---

### 2. Enriquecer Prospecto
`PUT /api/v1/prospects/{id}/enrich`

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| *(ninguno)* | — | El enriquecimiento no emite eventos |

---

### 3. Crear Solicitud de Crédito
`POST /api/v1/credit-applications`

| Evento | Contrato | Payload |
|--------|----------|---------|
| `CreditApplicationCreated` | `CreditApplicationCreatedContract` | `ApplicationId`, `ProspectId`, `Status="Draft"` |

---

### 4. Adjuntar Documento a la Solicitud
`POST /api/v1/credit-applications/{id}/documents`

| Evento | Contrato | Payload |
|--------|----------|---------|
| `DocumentUploaded` | `DocumentUploadedContract` | `DocumentId`, `OwnerId`, `OwnerType`, `DocumentTypeCode`, `StorageUrl`, `UploadedAt` |

---

### 5. Validar Documento
`PUT /api/v1/documents/{id}/validate`

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| `DocumentValidated` | `DocumentValidatedContract` | Decision = `Validated` |
| `DocumentRejected` | `DocumentRejectedContract` | Decision = `Rejected` |

---

### 6. Enviar Solicitud (Submit)
`POST /api/v1/credit-applications/{id}/submit`

Siempre se emiten los primeros dos eventos. Los siguientes dependen del outcome:

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| `CreditApplicationSubmitted` | `CreditApplicationSubmittedContract` | Siempre |
| `RiskEvaluationCompleted` | `RiskEvaluationCompletedContract` | Siempre — incluye `TotalScore` y `Outcome` |
| `ApprovalRequested` | `ApprovalRequestedContract` | Solo si `ManualReview` — indica el primer paso del workflow |
| `CreditApplicationApproved` | `CreditApplicationApprovedContract` | Solo si `AutoApprove` |
| `ProspectConverted` | `ProspectConvertedContract` | Solo si `AutoApprove` |
| `CustomerCreated` | `CreateCustomerContract` (SharedKernel) | Solo si `AutoApprove` — broadcast + command a `credit-service-customer-events` |

#### Outcomes del RiskEngine

| Outcome | Condición | Resultado |
|---------|-----------|-----------|
| `AutoApprove` | `TotalScore >= autoApproveThreshold` | Solicitud `Approved`, Prospect `Converted`, Customer creado |
| `ManualReview` | `autoRejectThreshold < score < autoApproveThreshold` | Solicitud `InReview`, Prospect `Submitted` |
| `AutoReject` | `TotalScore <= autoRejectThreshold` | Solicitud `Rejected`, Prospect regresa a `Draft` |

---

### 7. Aprobar paso del Workflow
`POST /api/v1/credit-applications/{id}/approve`

Solo disponible cuando `CreditApplication.Status = InReview`.

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| `ApprovalRequested` | `ApprovalRequestedContract` | Hay más pasos pendientes en el workflow |
| `ApplicationApproved` | `ApplicationApprovedContract` | Último paso completado |
| `ProspectConverted` | `ProspectConvertedContract` | Último paso completado |
| `CustomerCreated` | `CreateCustomerContract` (SharedKernel) | Último paso completado — broadcast + command a `credit-service-customer-events` |

---

### 8. Rechazar en Workflow
`POST /api/v1/credit-applications/{id}/reject`

Solo disponible cuando `CreditApplication.Status = InReview`.

| Evento | Contrato | Cuándo |
|--------|----------|--------|
| `ApplicationRejected` | `ApplicationRejectedContract` | Siempre |

El Prospect regresa a `Draft` y puede iniciar una nueva solicitud.

---

## Catálogo de eventos

| Evento | Interface (SharedKernel) | Contrato concreto | Namespace |
|--------|--------------------------|-------------------|-----------|
| `CreditApplicationCreated` | `CreditApplicationCreated` | `CreditApplicationCreatedContract` | `Crm.Application.CreditApplications.Dtos` |
| `CreditApplicationSubmitted` | `CreditApplicationSubmitted` | `CreditApplicationSubmittedContract` | `Crm.Application.CreditApplications.Dtos` |
| `RiskEvaluationStarted` | — | `RiskEvaluationStartedContract` | `Crm.Application.RiskEngine.Dtos` |
| `RiskEvaluationCompleted` | `RiskEvaluationCompleted` | `RiskEvaluationCompletedContract` | `Crm.Application.RiskEngine.Dtos` |
| `CreditApplicationApproved` | `CreditApplicationApproved` | `CreditApplicationApprovedContract` | `Crm.Application.CreditApplications.Dtos` |
| `CreditApplicationRejected` | `CreditApplicationRejected` | `CreditApplicationRejectedContract` | `Crm.Application.CreditApplications.Dtos` |
| `ProspectConverted` | `ProspectConverted` | `ProspectConvertedContract` | `Crm.Application.Prospects.Dtos` |
| `ApprovalRequested` | — | `ApprovalRequestedContract` | `Crm.Application.ApprovalWorkflows.Dtos` |
| `ApplicationApproved` | — | `ApplicationApprovedContract` | `Crm.Application.ApprovalWorkflows.Dtos` |
| `ApplicationRejected` | — | `ApplicationRejectedContract` | `Crm.Application.ApprovalWorkflows.Dtos` |
| `DocumentUploaded` | — | `DocumentUploadedContract` | `Crm.Application.Documents.Dtos` |
| `DocumentValidated` | — | `DocumentValidatedContract` | `Crm.Application.Documents.Dtos` |
| `DocumentRejected` | — | `DocumentRejectedContract` | `Crm.Application.Documents.Dtos` |
| `CustomerCreated` | `CustomerCreated` (SharedKernel) | `CreateCustomerContract` | `Crm.Application.Customers.Dtos` |
| `CustomerUpdated` | `CustomerUpdated` (SharedKernel) | `CustomerUpdatedContract` | `Crm.Application.Customers.Dtos` |

> Los eventos con interface en SharedKernel son **cross-service** — otros microservicios pueden suscribirse a ellos.
> Los que solo tienen contrato local son **internos al CRM**.

---

## Reglas de negocio clave

- Un Prospect `Converted` no puede iniciar una nueva solicitud.
- El submit requiere al menos los documentos de tipo `NationalId` e `IncomeProof` en estado `Uploaded` o `Verified`.
- El RiskEngine requiere una matriz activa; si no existe, el submit falla con `NoActiveMatrix`.
- El workflow de aprobación es configurable (N pasos, roles por paso). Si no hay workflow activo al momento del submit, el ManualReview usa un flujo de agente único (un solo approve cierra la solicitud).
- El rechazo en cualquier paso del workflow devuelve el Prospect a `Draft` — puede volver a aplicar.
