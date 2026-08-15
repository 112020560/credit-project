## Context

Stack: .NET 9, DDD + Event Sourcing, Dapper + Npgsql, MediatR. Este cambio depende de `ddd-structural-integrity` estar completado primero (el `ICustomerReferenceRepository` de escritura ya existe cuando este cambio se aplica).

Los 5 problemas que este cambio corrige son independientes entre sí en su implementación, pero todos contribuyen al mismo objetivo: que el código hable el lenguaje del negocio y que el modelo no pueda ser usado de forma incorrecta.

## Goals / Non-Goals

**Goals:**
- Renombrar `CustomerReference` → `CustomerCreditProfile` con migración PostgreSQL
- Eliminar el modelo anémico: `CustomerCreditProfile` con setters privados y factory method
- Renombrar `ICustomerService` → `ICustomerReadRepository`
- Hacer privado el constructor vacío de `RevolvingCreditAggregate`
- Mover `DomainEvent`/`IDomainEvent` a namespace compartido `Domain.Abstractions.Events`
- Usar `Money` en `ContractEvaluationContext` para campos financieros

**Non-Goals:**
- Cambiar el nombre de la tabla `customer_references` en PostgreSQL
- Agregar nuevos campos al perfil de cliente
- Introducir Typed IDs (Spec separado — baja prioridad)
- Cambiar el esquema de eventos en el event store

## Decisions

### Decision 1: Renombrar clase pero mantener nombre de tabla

La tabla `customer_references` mantiene su nombre en PostgreSQL — es un nombre perfectamente válido en snake_case. Solo cambia el nombre de la clase C# de `CustomerReference` a `CustomerCreditProfile`. El mapeo Dapper ya usa column mapping explícito o convenciones de snake_case, por lo que este cambio es transparente para la capa de datos.

**Migración SQL**: se agrega un script de migración que renombra la función/trigger si existe y actualiza comentarios de tabla. No hay ALTER TABLE de columnas.

### Decision 2: Factory method estático + constructor privado para CustomerCreditProfile

```csharp
public class CustomerCreditProfile
{
    private CustomerCreditProfile() { }

    public static CustomerCreditProfile Create(
        Guid externalId, string fullName, string documentType, string documentNumber) { ... }

    public Guid Id { get; private set; }
    public Guid ExternalId { get; private set; }
    public string FullName { get; private set; } = null!;
    // ... otros campos con private set
}
```

Para que `CustomerReferenceRepository` (Infrastructure) pueda hacer el mapeo Dapper desde SQL → objeto, se usa el patrón de **constructor privado + Dapper custom mapper** o bien `internal set` accesible desde Infrastructure via `InternalsVisibleTo`.

**Alternativa descartada**: usar `init` setters. Descartada porque `init` solo restringe la asignación fuera del inicializador, pero Dapper necesita poder asignar propiedades al mapear desde el DataReader — `init` no es compatible con Dapper por defecto.

**Decisión**: usar `internal set` con `[assembly: InternalsVisibleTo("CreditSystem.Infrastructure")]` en Domain, permitiendo que la implementación del repositorio haga el mapeo correctamente sin sacrificar la encapsulación hacia Application y Api.

### Decision 3: ICustomerService → ICustomerReadRepository (solo lectura)

La interfaz de lectura se renombra para diferenciarla explícitamente del repositorio de escritura (`ICustomerReferenceRepository` introducido en `ddd-structural-integrity`). El nombre `ICustomerReadRepository` comunica inmediatamente su rol en el patrón CQRS.

El archivo se mueve de `Domain/Abstractions/Services/ICustomerService.cs` a `Domain/Abstractions/Repositories/ICustomerReadRepository.cs` para unificar todos los repositorios en la misma carpeta.

### Decision 4: Constructor privado en RevolvingCreditAggregate

El constructor `public RevolvingCreditAggregate()` se hace `private`. El constructor de rehidratación `public RevolvingCreditAggregate(RevolvingCreditState? snapshot, IEnumerable<IDomainEvent> events)` permanece público ya que lo usa el repositorio de Infrastructure.

### Decision 5: Mover DomainEvent/IDomainEvent a Domain.Abstractions.Events

Ruta actual: `Src/Core/CreditSystem.Domain/Aggregates/LoanContract/Events/Base/`
Ruta destino: `Src/Core/CreditSystem.Domain/Abstractions/Events/`

Los archivos `DomainEvent.cs` e `IDomainEvent.cs` se mueven físicamente. Todos los archivos que tienen `using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;` se actualizan al nuevo namespace. Esto incluye todos los eventos de `LoanContract/Events/`, todos los eventos de `RevolvingCredit/Events/`, y los agregados.

### Decision 6: ContractEvaluationContext con Money

`RequestedAmount` pasa de `decimal` a `Money`. Los campos `CollateralValue`, `MonthlyIncome`, `MonthlyDebt` pasan de `decimal?` a `Money?`.

Las reglas afectadas (`MaxLoanAmountRule`, `CollateralRule`, `DebtToIncomeRule`) acceden a `.Amount` del value object en lugar del decimal directo. El handler `CreateContractCommandHandler` construye el contexto creando `new Money(command.Amount, command.Currency)` para `RequestedAmount`.

## Risks / Trade-offs

- **Impacto de renombrado amplio** → `CustomerReference` aparece en Domain, Application, Infrastructure y potencialmente en Tests. Mitigation: compilación `dotnet build` falla inmediatamente en cualquier referencia no actualizada — el compilador actúa como safety net.
- **Dapper + constructor privado** → Dapper usa reflection para mapear propiedades. Con `private set` necesita `InternalsVisibleTo` o un custom type handler. Mitigation: usar `internal set` + `InternalsVisibleTo` es el approach más simple y probado en .NET.
- **Cambio de tipos en ContractEvaluationContext** → las reglas de evaluación deben actualizarse para acceder a `.Amount`. Mitigation: el cambio de tipo es un error de compilación — no puede pasar desapercibido.
- **RevolvingCreditAggregate constructor privado** → si hay tests que instancian directamente con `new RevolvingCreditAggregate()` deben actualizarse a usar `Create(...)`. Mitigation: los tests existentes en `RevolvingCreditAggregateTests.cs` deben revisarse en la tarea de validación.

## Migration Plan

1. Mover `DomainEvent`/`IDomainEvent` a nuevo namespace y actualizar todos los `using` — **hacer primero** para que el resto del refactor compile
2. Renombrar `CustomerReference` → `CustomerCreditProfile` con factory method y `internal set`
3. Renombrar `ICustomerService` → `ICustomerReadRepository` y mover a `Repositories/`
4. Hacer privado constructor vacío de `RevolvingCreditAggregate`
5. Actualizar `ContractEvaluationContext` y las reglas afectadas
6. Agregar script SQL de migración (comentarios de tabla, no hay cambio de columnas)
7. `dotnet build` + `dotnet test`

## Open Questions

- ¿Se requiere `[assembly: InternalsVisibleTo("CreditSystem.Tests")]` adicionalmente para que los tests puedan usar `internal set`? Verificar si los tests existentes prueban `CustomerCreditProfile` directamente.
