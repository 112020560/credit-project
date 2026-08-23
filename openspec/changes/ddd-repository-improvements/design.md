## Context

`ICustomersRepository` tiene tres problemas identificados en el DDD analysis:

1. `GetCustomerByIdTrackingAsync` — el nombre `Tracking` es un concepto de EF Core expuesto en la interfaz del dominio. El dominio no debe saber que existe el tracking de entidades.
2. `GetAllCustomersAsync` — cargar todos los customers sin límite es un riesgo en producción. No tiene ningún uso legítimo que no pueda cubrirse con `SearchAsync` paginado.
3. `SearchAsync(string? query, int page, int pageSize, CancellationToken)` — mezcla criterio de búsqueda (negocio) con parámetros de paginación (infraestructura/UI) en la firma del método.

## Goals / Non-Goals

**Goals:**
- Renombrar `GetCustomerByIdTrackingAsync` a `GetForUpdateAsync`
- Eliminar `GetAllCustomersAsync` y migrar sus usos
- Crear `CustomerSearchCriteria` record con los parámetros de búsqueda

**Non-Goals:**
- No se implementa el patrón Specification completo en este cambio
- No se cambia el comportamiento de búsqueda, solo la firma
- No se agrega caching ni optimizaciones de query

## Decisions

### D1: `GetForUpdateAsync` como nombre semántico para el método con tracking

El nombre expresa la intención de negocio: "obtener el customer para realizar una operación de modificación". La implementación en Infrastructure puede usar `AsTracking()` internamente sin que el dominio lo sepa.

```csharp
// Antes:
Task<Customer?> GetCustomerByIdTrackingAsync(Guid id, CancellationToken ct);

// Después:
Task<Customer?> GetForUpdateAsync(Guid id, CancellationToken ct);
```

### D2: `CustomerSearchCriteria` como record simple

```csharp
public record CustomerSearchCriteria(string? Query, int Page, int PageSize);
```

Encapsula los parámetros en un objeto del dominio, facilitando agregar nuevos criterios (filtro por status, fecha de creación, etc.) sin cambiar la firma del repositorio.

### D3: `GetAllCustomersAsync` se elimina

Se buscan todos los usos en el codebase. Si alguno tiene una necesidad real, se reemplaza con `SearchAsync` con `PageSize` alto controlado. Si no hay usos, se elimina directamente.

## Risks / Trade-offs

**[Risk] Si algún código usa `GetAllCustomersAsync` para operaciones batch**
→ Mitigation: Buscar todos los usos antes de eliminar. Si existe un caso batch legítimo, se crea un método específico (`GetAllForBatchProcessingAsync`) con límite explícito en el nombre para que sea obvio que es peligroso.
