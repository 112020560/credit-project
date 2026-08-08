## Context

Stack: .NET 9, DDD + Event Sourcing, Dapper + Npgsql, MediatR. Este cambio depende de `ddd-structural-integrity` y `ddd-ubiquitous-language` completados. El `ContractEngine` ya no tiene `ILogger` (resuelto en spec anterior) y las referencias a tipos financieros ya usan `Money`.

## Goals / Non-Goals

**Goals:**
- Externalizar `BaseInterestRate` y `AutoDefaultThresholdDays` a tabla PostgreSQL via `UnderwritingPolicy`
- Agregar `ContractApproved` al ciclo de vida del contrato
- Hacer configurable el comportamiento de `CreditScoreRule` sin score disponible
- Estandarizar comentarios a inglés en Domain y Application

**Non-Goals:**
- Agregar UI o endpoint de administración para editar políticas (backoffice out of scope)
- Typed IDs (separado, considerado de muy baja prioridad)
- Agregar `DocumentNumber` como Value Object (separado)

## Decisions

### Decision 1: UnderwritingPolicy como record de dominio cargado en startup

`UnderwritingPolicy` es un record inmutable en Domain que se carga una vez al inicio y se registra en DI como singleton. No se recarga en runtime (requeriría restart de la aplicación para cambiar políticas).

```csharp
public record UnderwritingPolicy
{
    public decimal BaseInterestRate { get; init; }
    public int AutoDefaultThresholdDays { get; init; }
    public NoScoreBehavior NoScoreBehavior { get; init; }
}
```

**Alternativa descartada**: recargar la política en cada evaluación (query en cada request). Descartada porque la política cambia raramente y el overhead de un query adicional por cada solicitud de crédito no está justificado.

**Alternativa descartada**: usar `IOptions<UnderwritingPolicy>` desde `appsettings.json`. Descartada porque el usuario especificó base de datos como fuente de configuración (permite cambios sin redeployment, con trazabilidad de quién cambió qué).

### Decision 2: IUnderwritingPolicyRepository en Domain, singleton en DI

```csharp
// Domain/Abstractions/Repositories/IUnderwritingPolicyRepository.cs
public interface IUnderwritingPolicyRepository
{
    Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default);
}
```

En `Program.cs` / DI registration: el `UnderwritingPolicy` se carga en startup y se registra como singleton para que `ContractEngine` y `CreditScoreRule` (ambos registrados como scoped/transient) lo reciban por inyección.

### Decision 3: ContractApproved es un segundo evento emitido por Create()

El factory method `LoanContractAggregate.Create(...)` emite dos eventos en secuencia:
1. `ContractCreated` — persiste los datos del contrato
2. `ContractApproved` — captura la decisión de aprobación con la tasa final

El estado del aggregate no cambia entre ambos eventos (permanece en `Approved`). `ContractApproved` es un evento de auditoría/notificación que permite a proyectores y suscriptores reaccionar específicamente a la aprobación sin necesidad de inferirla de `ContractCreated`.

**Alternativa descartada**: fusionar `ContractApproved` en `ContractCreated` agregando los campos de aprobación. Descartada porque mezcla dos hechos de negocio distintos en un solo evento — viola el principio de eventos como hechos atómicos del dominio.

### Decision 4: CreditScoreRule recibe UnderwritingPolicy por constructor

`CreditScoreRule` se registra con DI y recibe `UnderwritingPolicy` (singleton) por constructor. El comportamiento sin score lo determina `policy.NoScoreBehavior`.

La penalización por `ApproveWithPenalty` se fija en `+5.0%` como valor de negocio razonable. Si en el futuro se quiere configurar también, se agrega `NoScorePenaltyRate` a `UnderwritingPolicy`.

### Decision 5: RecordMissedPayment recibe umbral como parámetro

En lugar de leer la política desde dentro del aggregate (lo que requeriría inyectar una dependencia en el aggregate), el umbral se pasa como parámetro al método:

```csharp
public void RecordMissedPayment(int paymentNumber, DateTime dueDate, Money lateFee, int autoDefaultThresholdDays)
```

El caller (el worker `PaymentMissedWorker` / el job `PaymentMissedJob`) obtiene la política y pasa el umbral. Esto mantiene el aggregate libre de dependencias.

## Risks / Trade-offs

- **UnderwritingPolicy singleton**: si la política cambia en DB, requiere restart. Mitigación aceptada: políticas de underwriting son cambios de negocio formales que deben gestionarse con process — un restart controlado es razonable.
- **ContractApproved añade un evento extra al stream**: ligero aumento de volumen en el event store. Trade-off aceptado: el valor de auditoría y la claridad del modelo justifican el overhead.
- **Cambio de firma de RecordMissedPayment**: es un BREAKING change en la API interna del aggregate. Mitigación: el único caller es `PaymentMissedJob` — es un cambio localizado.

## Migration Plan

1. Crear tabla `underwriting_policies` con script SQL y seed
2. Crear `UnderwritingPolicy` record, `IUnderwritingPolicyRepository`, `UnderwritingPolicyRepository`
3. Registrar política como singleton en DI startup
4. Actualizar `ContractEngine` para usar `policy.BaseInterestRate`
5. Actualizar `CreditScoreRule` para usar `policy.NoScoreBehavior`
6. Actualizar `LoanContractAggregate.RecordMissedPayment` para recibir umbral como parámetro
7. Crear `ContractApproved` evento y agregarlo al factory method `Create()`
8. Actualizar `ApplyEvent` switch para manejar `ContractApproved`
9. Limpiar comentarios en español de Domain y Application
10. `dotnet build` + `dotnet test`
