## Context

El sistema CRM tiene un Core Domain claro: el flujo de originación de crédito. Sin embargo, este núcleo no está documentado, y cualquier desarrollador nuevo debe inferirlo leyendo el código. Adicionalmente, `CustomersRef` en `Crm.Domain` es una entidad para referenciar clientes que existen en sistemas externos (clientes que nunca fueron originados por este CRM). Tiene `ExternalId`, `RiskScore`, `Version` y se escribe al schema del dominio desde un endpoint dedicado. El problema no es que exista, sino que su nombre es técnico/ambiguo y no hay una interfaz de dominio que defina cómo debe interactuar con el resto del modelo.

## Goals / Non-Goals

**Goals:**
- Documentar explícitamente el mapa estratégico del dominio en un ADR
- Crear un Anti-Corruption Layer para la integración que produce `CustomersRef`
- Establecer la nomenclatura que el equipo usará para hablar de subdomains

**Non-Goals:**
- No se separa el sistema en microservicios
- No se cambia la arquitectura del monolito
- No se implementa un ACL completo para todas las integraciones existentes (foco en `CustomersRef`)

## Decisions

### D1: Clasificación de subdomains

**Core Domain** (máximo esfuerzo de modelado, los mejores desarrolladores):
- Originación de crédito: `Prospect` → `CreditApplication` → `RiskEvaluation` → `Customer`
- Motor de riesgo: `RiskMatrix`, `RiskRule`, evaluación y scoring

**Supporting Subdomain** (necesario pero no diferenciador):
- Gestión de documentos (`Document`, `DocumentValidation`)
- Workflows de aprobación (`WorkflowDefinition`, `WorkflowStep`, `ApprovalDecision`)

**Generic Subdomain** (usar librería o servicio externo):
- Autenticación / autorización (JWT)
- Mensajería (RabbitMQ / SmartCore.Outbox)
- Logging (Serilog / Seq)
- Telemetría (OpenTelemetry)

### D2: ExternalCustomerRef como entidad explícita de referencia — con endpoint y repositorio propios

`CustomersRef` se renombra a `ExternalCustomerRef` para expresar su propósito: es una referencia a un cliente que vive en un sistema externo. Se expone un endpoint `POST /api/v1/external-customers` que permite registrar o actualizar estas referencias. La interfaz de dominio `IExternalCustomerRefRepository` se define en `Crm.Domain/Abstractions/Persistence/` y su implementación vive en Infrastructure.

```
External System data → POST /api/v1/external-customers → RegisterExternalCustomerCommand
  → IExternalCustomerRefRepository.UpsertAsync(ExternalCustomerRef)
```

**No se crea un ACL complejo** porque el CRM es el destino de los datos (no los consume hacia afuera). El endpoint actúa como el punto de entrada controlado — eso es suficiente para proteger el modelo de dominio.

### D3: ADR en docs/adr/ con formato estándar

Usar formato MADR (Markdown Architectural Decision Records):
- `docs/adr/0001-strategic-design-domain-classification.md`

## Risks / Trade-offs

**[Risk] Renombrar `CustomersRef` puede afectar el sistema externo que escribe en esa tabla directamente**
→ Mitigation: Si el sistema externo escribe directo a DB (sin pasar por el API del CRM), agregar un VIEW de compatibilidad con el nombre antiguo mientras se migra ese sistema al nuevo endpoint.

**[Risk] El endpoint `POST /api/v1/external-customers` puede recibir datos con formatos distintos de múltiples sistemas externos**
→ Mitigation: El contrato del endpoint define el schema canónico del CRM. Cada sistema externo es responsable de adaptar su payload — el CRM no debe adaptar su modelo por cada fuente.
