## Context

Problemas de naming detectados en el DDD analysis (score 6/10 en Ubiquitous Language):

1. `CustomerModel` en `Crm.Domain` duplica las propiedades de `Customer` sin semántica de negocio. Solo se usa en `Customer.ConvertToModel()` que ningún handler actual consume.
2. `CustomersRef` tiene nombre técnico/ambiguo — no queda claro qué representa. Mirando sus propiedades (`ExternalId`, `DisplayName`, `LegalName`, `RiskScore`, `Metadata`, `Version`) parece ser un snapshot de datos de cliente provenientes de un sistema externo.
3. `Class1.cs` es scaffolding sobrante.
4. `Status = "Active"` en `CreateCustomerCommand.cs:100` es un magic string. El resto del dominio usa clases estáticas con constantes (ej: `CreditApplicationStatus.Draft`).

## Goals / Non-Goals

**Goals:**
- Eliminar artefactos sin propósito del Domain layer
- Crear `CustomerStatus` con constantes string (mismo patrón que `CreditApplicationStatus`)
- Documentar explícitamente el propósito de `CustomersRef` o renombrarlo

**Non-Goals:**
- No se refactoriza la arquitectura de bounded contexts en este cambio
- No se cambia el schema de base de datos
- No se agrega lógica de negocio nueva

## Decisions

### D1: CustomerModel se elimina completamente
`Customer.ConvertToModel()` no tiene consumidores activos en el codebase actual. Se elimina `CustomerModel` y el método `ConvertToModel()`. Si en el futuro se necesita una proyección de lectura, se crea como DTO en la capa Application.

### D2: CustomerStatus sigue el patrón existente de CreditApplicationStatus
```csharp
public static class CustomerStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}
```
Simple, consistente con el resto del dominio.

### D3: CustomersRef se documenta con comentario XML hasta tener más contexto
Hasta confirmar con el equipo qué sistema externo la produce, se agrega un comentario XML `<summary>` que describe su propósito conocido (snapshot de referencia de cliente externo) y se deja como tarea pendiente el renombramiento definitivo.

## Risks / Trade-offs

**[Risk] Algún código que usa `CustomerModel` o `Customer.ConvertToModel()` podría haberse omitido en el análisis**
→ Mitigation: El compilador detectará todas las referencias al eliminar la clase. Compilar y resolver todos los errores antes de mergear.

**[Risk] `CustomersRef` puede ser usada por algún servicio externo que lee de la DB directamente**
→ Mitigation: La clase no se elimina — solo se documenta. El renombramiento definitivo queda como tarea pendiente hasta confirmar con el equipo.
