# DDD Analysis — CRM Backend

Fecha: 2026-06-24
Score global: **4.5 / 10**

## Diagnóstico por principio

| Principio DDD                   | Score | Estado |
|---------------------------------|-------|--------|
| Ubiquitous Language             | 6/10  | Bien, con naming smells |
| Bounded Contexts                | 5/10  | Un solo contexto, sin ACL |
| Entities / Value Objects / Aggregates | 2/10 | Todo son Entities, sin VOs |
| Domain Events                   | 1/10  | Inexistentes en el dominio |
| Comportamiento en aggregates    | 2/10  | Modelo anémico generalizado |
| Repositorios / Factories        | 5/10  | Interfaces OK, con leaks de persistencia |
| Strategic Design                | 5/10  | Core Domain implícito, no distillado |

---

## Recomendaciones

### REC-01 — Eliminar el Anemic Domain Model (CRITICO)
**Spec:** `ddd-rich-domain-model`

Las entidades son bolsas de datos sin comportamiento. Las transiciones de estado y reglas de negocio viven en los Command Handlers de Application.

**Aggregates afectados y métodos que deben existir:**

| Aggregate         | Métodos a agregar |
|-------------------|-------------------|
| `CreditApplication` | `Submit()`, `Approve()`, `Reject(reason)`, `SendToReview(workflowId)` |
| `Prospect`        | `Submit()`, `Enrich(data)`, `Convert()`, `Reject()` |
| `Customer`        | `Activate()`, `Deactivate()`, `UpdateProfile(data)`, `UpdateFinancials(data)` |
| `WorkflowDefinition` | `Activate()`, `Deactivate()` |
| `RiskMatrix`      | `Activate()` |

**Consecuencia actual:** La regla "una CreditApplication solo puede pasar a Approved si está en InReview" está dispersa entre tres handlers y puede violarse si se agrega código nuevo.

---

### REC-02 — Introducir Domain Events (CRITICO)
**Spec:** `ddd-rich-domain-model`

No existen domain events. Los handlers publican integration events directamente al outbox desde Application, mezclando dos responsabilidades.

**Patrón objetivo:**
```
aggregate.Approve()  →  acumula DomainEvent internamente
handler recoge events  →  traduce a OutboxEvent (integration events)
```

**Inconsistencias adicionales por ausencia de domain events:**
- `ApplicationApproved` (ApprovalWorkflowService) ≠ `CreditApplicationApproved` (SubmitCreditApplicationCommand) — mismo concepto, dos nombres
- `ApplicationRejected` ≠ `CreditApplicationRejected` — igual

---

### REC-03 — Convertir sub-entidades a Value Objects (CRITICO)
**Spec:** `ddd-rich-domain-model`

`CustomerAddress`, `CustomerEmail`, `CustomerPhone`, `CustomerFiscalInfo` y sus equivalentes en `Prospect` tienen `Id` propio pero no tienen identidad de negocio real.

| Clase              | Estado actual        | Debería ser     |
|--------------------|----------------------|-----------------|
| `CustomerAddress`  | Entidad con Id       | Value Object    |
| `CustomerEmail`    | Entidad con Id       | Value Object    |
| `CustomerPhone`    | Entidad con Id       | Value Object    |
| `CustomerFiscalInfo` | Entidad con Id     | Value Object    |
| `ProspectAddress`  | Entidad con Id       | Value Object    |
| `ProspectEmail`    | Entidad con Id       | Value Object    |
| `ProspectPhone`    | Entidad con Id       | Value Object    |

En EF Core se mapean con `OwnsMany` / `OwnsOne`.

---

### REC-04 — Agregar Factory Method `Customer.FromProspect(prospect)` (CRITICO)
**Spec:** `ddd-rich-domain-model`

La conversión Prospect → Customer está triplicada:
1. `SubmitCreditApplicationCommand` (AutoApprove): llama `ApproveCreditApplicationCommandHandler.MapProspectToCustomer`
2. `ApprovalWorkflowService.RecordDecisionAsync` (ManualReview approve): misma llamada
3. `ApproveCreditApplicationCommand`: dueño del método estático, pero no debería serlo

La lógica de construcción de un aggregate complejo pertenece al dominio.

---

### REC-05 — Un aggregate por transacción (CRITICO)
**Spec:** `ddd-rich-domain-model`

`SubmitCreditApplicationCommand` y `ApprovalWorkflowService` mutan `CreditApplication`, `Prospect` y crean `Customer` en un solo `SaveChangesAsync`. Viola la regla DDD de consistencia fuerte solo dentro de un aggregate.

**Solución:** Cada transición dispara un domain event → el siguiente aggregate reacciona de forma asíncrona vía event handler.

---

### REC-06 — Eliminar `CustomerModel.cs` (IMPORTANTE)
**Spec:** `ddd-language-cleanup`

`CustomerModel` en `Crm.Domain` duplica propiedades de `Customer` sin semántica de negocio. Si es una proyección de lectura, pertenece a Application como DTO.

---

### REC-07 — Renombrar o clarificar `CustomersRef` (IMPORTANTE)
**Spec:** `ddd-language-cleanup`

`CustomersRef` es un nombre técnico/ambiguo. No queda claro qué contexto lo produce ni qué representa. Renombrar a algo que exprese su propósito real (ej: `ExternalCustomerSnapshot`).

---

### REC-08 — Agregar `CustomerStatus` con constantes (IMPORTANTE)
**Spec:** `ddd-language-cleanup`

`Customer.Status = "Active"` hardcoded en `CreateCustomerCommand.cs:100` es un magic string. Crear `CustomerStatus` como `CreditApplicationStatus` ya hace.

---

### REC-09 — Ocultar `AsTracking` detrás de semántica de negocio (IMPORTANTE)
**Spec:** `ddd-repository-improvements`

`GetCustomerByIdTrackingAsync` expone un concepto de EF Core (`AsTracking`) en la interfaz del dominio. Renombrar a `GetForUpdateAsync` — expresa intención, no mecanismo.

---

### REC-10 — Unificar nombres de eventos de integración (IMPORTANTE)
**Spec:** `ddd-language-cleanup`

Inconsistencias en nombres de eventos:
- `ApplicationApproved` vs `CreditApplicationApproved`
- `ApplicationRejected` vs `CreditApplicationRejected`

Definir un glosario de eventos y uniformar en todo el codebase.

---

### REC-11 — Eliminar `Class1.cs` (IMPORTANTE)
**Spec:** `ddd-language-cleanup`

Archivo scaffolding sobrante en `Crm.Domain`. No tiene propósito.

---

### REC-12 — Definir explícitamente el Core Domain (LARGO PLAZO)
**Spec:** `ddd-strategic-design`

Documentar en un ADR qué es el Core Domain (flujo de originación de crédito), cuáles son los bounded contexts y qué es Supporting vs Generic Subdomain.

**Clasificación sugerida:**
- **Core Domain:** Prospect → CreditApplication → RiskEvaluation → Customer (originación)
- **Supporting:** Gestión de documentos, Workflow definitions
- **Generic:** Autenticación, Mensajería (RabbitMQ/Outbox), Logging

---

### REC-13 — Agregar Anti-Corruption Layer para integraciones externas (LARGO PLAZO)
**Spec:** `ddd-strategic-design`

`CustomersRef` parece ser un snapshot de un sistema externo pero no hay ACL explícito. Los datos externos no deben llegar directamente al schema del dominio sin traducción.

---

### REC-14 — Remover o limitar `GetAllCustomersAsync` (REPO)
**Spec:** `ddd-repository-improvements`

Cargar TODOS los customers sin paginación ni filtro es un riesgo de producción. Reemplazar con queries específicas o con `SearchAsync` paginado.

---

### REC-15 — Revisar parámetros de paginación en repositorios (REPO)
**Spec:** `ddd-repository-improvements`

`SearchAsync(string? query, int page, int pageSize, ...)` mezcla criterio de búsqueda (dominio) con parámetros de paginación (UI/infraestructura). Considerar encapsular en un objeto `CustomerSearchCriteria`.

---

## Specs planificados

| Spec ID                        | Recomendaciones | Criticidad |
|-------------------------------|-----------------|------------|
| `ddd-rich-domain-model`       | REC-01, 02, 03, 04, 05 | CRITICO |
| `ddd-language-cleanup`        | REC-06, 07, 08, 10, 11 | IMPORTANTE |
| `ddd-repository-improvements` | REC-09, 14, 15  | IMPORTANTE |
| `ddd-strategic-design`        | REC-12, 13      | LARGO PLAZO |

## Camino crítico hacia 8/10

```
REC-01 (comportamiento)
  → REC-02 (domain events)
    → REC-03 (value objects)
      → REC-04 (factory methods)
        → REC-05 (1 aggregate/transacción)
```

Una vez aplicados estos 5, el modelo puede expresar y proteger sus propias invariantes de negocio.
