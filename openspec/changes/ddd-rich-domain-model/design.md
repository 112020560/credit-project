## Context

El backend CRM fue construido con Clean Architecture y separación de capas correcta, pero el modelo de dominio es anémico: todas las entidades son POCOs sin comportamiento, las reglas de negocio residen en Application handlers, y no existen Domain Events. El resultado es que:

- Las invariantes de los aggregates (ej: "solo se puede aprobar si está en InReview") no tienen punto único de verdad
- La conversión Prospect→Customer está triplicada en tres handlers diferentes
- Los nombres de eventos de integración son inconsistentes (`ApplicationApproved` vs `CreditApplicationApproved`)
- Los cambios de estado son mutar-propiedades-directamente, sin verificación de precondiciones

Stack: .NET 9, EF Core 9 (PostgreSQL / Npgsql), MediatR, SmartCore.Outbox.

## Goals / Non-Goals

**Goals:**
- Introducir `AggregateRoot` con soporte de Domain Events en `Crm.Domain`
- Que todos los aggregates principales hereden de `AggregateRoot` y expongan métodos de comportamiento
- Convertir sub-entidades sin identidad de negocio en Value Objects inmutables con `OwnsMany`/`OwnsOne` en EF Core
- Centralizar la conversión Prospect→Customer en `Customer.FromProspect()` (factory method)
- Actualizar los Command Handlers para usar los métodos de comportamiento y publicar Domain Events al outbox
- Unificar nombres de eventos de integración derivándolos de los Domain Events

**Non-Goals:**
- No se implementa Event Sourcing — los domain events son efímeros (no se persisten individualmente)
- No se mueve la lógica de outbox publishing a un componente genérico/infraestructura
- No se agrega CQRS read-side / projections
- No se implementan tests automatizados en este cambio (se abordan en un cambio dedicado posterior)

## Decisions

### D1: AggregateRoot con lista interna de Domain Events — dispatch in-process via MediatR

**Decisión:** `AggregateRoot` acumula `List<IDomainEvent>` internamente (sin dependencia de MediatR en el dominio). Los handlers de Application leen esa lista después de `SaveChangesAsync` y la despachan via `IMediator.Publish()`. Esto activa los `INotificationHandler<T>` que manejan la coordinación entre aggregates, cada uno con su propia unidad de trabajo.

**Alternativa considerada:** Handlers que coordinan todos los aggregates directamente (el patrón actual). Se descarta porque concentra demasiada responsabilidad en un solo handler y hace imposible testear cada aggregate de forma independiente.

**Alternativa considerada:** Outbox-driven (eventos al outbox → background worker → actualiza otros aggregates). Se descarta para este cambio porque requiere infraestructura adicional y la consistencia eventual no está justificada dentro de los límites del mismo servicio.

**Alternativa considerada:** Retornar los events desde los métodos de comportamiento. Se descarta porque requiere que el handler capture el valor de retorno; la lista interna es más ergonómica y es el patrón estándar de Vaughn Vernon.

```csharp
// Crm.Domain/Abstractions/IDomainEvent.cs
public interface IDomainEvent { }

// Crm.Domain/Abstractions/AggregateRoot.cs
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### D2: Los métodos de comportamiento retornan Result<T> para señalar errores de dominio

**Decisión:** Métodos como `Submit()`, `Approve()`, `Reject(reason)` retornan `Result` (del SharedKernel) para comunicar precondiciones no cumplidas sin lanzar excepciones.

**Alternativa considerada:** Lanzar excepciones de dominio. Se descarta porque el proyecto ya usa el patrón Result de forma consistente; las excepciones de dominio romperían esa coherencia.

```csharp
public Result Submit()
{
    if (Status != CreditApplicationStatus.Draft)
        return Result.Failure(CreditApplicationError.InvalidTransition(Status, CreditApplicationStatus.Submitted));
    Status = CreditApplicationStatus.Submitted;
    RaiseDomainEvent(new CreditApplicationSubmittedEvent(Id, ProspectId));
    return Result.Success();
}
```

### D3: Value Objects compartidos en `Crm.Domain/ValueObjects/` — usados por Customer y Prospect

**Decisión:** Los tipos `Address`, `EmailContact`, `PhoneContact`, `WorkInfo` y `FiscalInfo` se definen una sola vez en `Crm.Domain/ValueObjects/` y son usados por ambos aggregates (`Customer` y `Prospect`). No viola ningún principio DDD porque los Value Objects son inmutables y no tienen identidad — pueden ser compartidos libremente dentro del mismo bounded context.

**Alternativa considerada:** Tipos separados por aggregate (`CustomerAddress`, `ProspectAddress`). Se descarta porque son conceptualmente idénticos y duplicar el código no aporta valor.

**Implementación EF Core:** `OwnsMany` con tabla separada para cada aggregate. La misma clase `Address` se mapea a `customer_addresses` para `Customer` y a `prospect_addresses` para `Prospect`. El Id interno de EF se mantiene como shadow property — sin exponer en el modelo de dominio.

```csharp
// Crm.Domain/ValueObjects/Address.cs
public record Address(string Type, string? Street, string? City, string? State,
                      string? Country, string? PostalCode, bool? IsPrimary);

// En CrmDbContext:
entity.OwnsMany(c => c.Addresses, a => {
    a.ToTable("customer_addresses");
    // Sin exposición de Id en el VO; EF usa shadow property
});
prospectEntity.OwnsMany(p => p.Addresses, a => {
    a.ToTable("prospect_addresses");
});
```

Los VOs son `record` types — inmutables por defecto.

### D4: `Customer.FromProspect()` como static factory method en la entidad

**Decisión:** Factory method `static Customer FromProspect(Prospect prospect)` en `Customer.cs`. Los handlers que actualmente llaman `ApproveCreditApplicationCommandHandler.MapProspectToCustomer()` pasarán a usar este método.

**Alternativa considerada:** Crear un `CustomerFactory` service en el dominio. Se descarta por ser innecesariamente complejo para un caso de construcción que depende solo de datos del `Prospect`.

### D5: Los handlers son responsables de traducir Domain Events a OutboxEvents

**Decisión:** Después de llamar un método de comportamiento, el handler itera `aggregate.DomainEvents` y por cada uno crea el `OutboxEvent` correspondiente, luego llama `aggregate.ClearDomainEvents()`.

**Alternativa considerada:** Un pipeline behavior de MediatR que haga el dispatch automático. Se descarta porque la traducción Domain Event → OutboxEvent es específica del contrato de integración y no es genérica.

### D6: Un aggregate por transacción — coordinación via MediatR INotification (REC-05)

**Decisión:** Cada Command Handler toca UN SOLO aggregate en su `SaveChangesAsync`. La coordinación entre aggregates ocurre vía `IMediator.Publish(domainEvent)` después del save. Cada `INotificationHandler<T>` carga y modifica su propio aggregate con su propia llamada a `SaveChangesAsync`.

**Flujo resultante para Submit (AutoApprove):**
```
SubmitCreditApplicationCommand
  ├── application.Submit() → evaluate risk → application.Approve()
  ├── SaveChangesAsync  ← solo CreditApplication
  ├── IMediator.Publish(CreditApplicationApprovedEvent)
  │     └── CreditApplicationApprovedHandler:
  │           ├── prospect.Convert()
  │           ├── Customer.FromProspect(prospect)
  │           ├── SaveChangesAsync  ← Prospect + Customer (mismo aggregate boundary)
  │           └── outbox: ProspectConverted, CustomerCreated
  └── outbox: CreditApplicationApproved, RiskEvaluationCompleted
```

**Flujo para ManualReview:**
```
SubmitCreditApplicationCommand
  ├── application.Submit() → evaluate risk → application.SendToReview(workflowId)
  ├── SaveChangesAsync  ← solo CreditApplication
  ├── IMediator.Publish(CreditApplicationSentToReviewEvent)
  │     └── CreditApplicationSentToReviewHandler:
  │           ├── prospect.Submit()
  │           ├── SaveChangesAsync  ← solo Prospect
  │           └── outbox: ApprovalRequested
  └── outbox: CreditApplicationSubmitted, RiskEvaluationCompleted
```

**Beneficio clave:** El `SubmitCreditApplicationCommand` pasa de 200 líneas a ~40. Cada handler tiene responsabilidad única y puede ser testeado en aislamiento.

## Risks / Trade-offs

**[Risk] Breaking change en schema de base de datos para Value Objects**
→ Mitigation: Crear migración EF Core explícita. Los datos existentes en tablas hijas (`customer_addresses`, `prospect_emails`, etc.) se preservan si se mantiene el mismo nombre de tabla. Revisar el schema resultante de `OwnsMany` para confirmar compatibilidad antes de aplicar.

**[Risk] Los aggregates que heredan de `AggregateRoot` tienen un campo `_domainEvents` que EF Core podría intentar mapear**
→ Mitigation: Marcar `DomainEvents` como `[NotMapped]` o ignorarlo en la configuración de EF Core via `entity.Ignore(e => e.DomainEvents)`.

**[Risk] Los handlers actuales que mutan propiedades directamente quedan inconsistentes con el nuevo patrón**
→ Mitigation: Actualizar TODOS los handlers en el mismo PR para evitar que ambos patrones coexistan. El compilador ayuda: si las propiedades de los aggregates pasan a `private set`, el código que las muta externamente fallará en compilación.

**[Risk] Value Objects inmutables: ¿cómo hace EF Core para materializar objetos sin setter público?**
→ Mitigation: EF Core 9 soporta `init` setters y constructores con parámetros en `OwnsMany`. Usar record types o clases con constructor matching.

## Migration Plan

1. Crear `IDomainEvent` y `AggregateRoot` en `Crm.Domain/Abstractions/`
2. Crear tipos `Address`, `EmailContact`, `PhoneContact`, `WorkInfo`, `FiscalInfo` en `Crm.Domain/ValueObjects/`
3. Agregar herencia `AggregateRoot` a los 5 aggregates principales + reemplazar sub-entidades por VOs compartidos
4. Definir Domain Events por aggregate (`Crm.Domain/<Aggregate>/Events/`)
5. Agregar métodos de comportamiento por aggregate
6. Crear `Customer.FromProspect()` factory method
7. Actualizar `CrmDbContext`: `OwnsMany` para todos los VOs de Customer y Prospect
8. Crear migración EF Core
9. Crear `INotificationHandler<T>` para coordinación entre aggregates (REC-05)
10. Simplificar Command Handlers: cada uno toca su propio aggregate + publica via `IMediator.Publish`
11. Verificar build completo: `dotnet build CrmProject.sln`

**Rollback:** `dotnet ef database update <PreviousMigrationName>` revierte el schema. Los cambios de código son reversibles vía git.

## Open Questions

- ¿El campo `Metadata` en `CustomerAddress` (string JSON) se mantiene o se modela como Dictionary? Decisión: mantener como string en este cambio — modelar en cambio posterior.
