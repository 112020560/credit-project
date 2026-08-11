## 1. Infraestructura — Generador de número de socio

- [x] 1.1 Crear interfaz `IMemberNumberGenerator` en `CreditSystem.Domain/Abstractions/` con método `Task<string> GenerateAsync(CancellationToken ct = default)`
- [x] 1.2 Crear migration SQL `20260811_AddMemberNumberSequences.sql` con tabla `member_number_sequences (year INT PRIMARY KEY, last_value INT NOT NULL DEFAULT 0)`
- [x] 1.3 Crear clase de configuración `MemberNumberFormatOptions` en `CreditSystem.Infrastructure/` con propiedades `Prefix` (default "CM") y `SequentialDigits` (default 5)
- [x] 1.4 Implementar `DefaultMemberNumberGenerator` en `CreditSystem.Infrastructure/` que lee `MemberNumberFormatOptions`, ejecuta el SQL atómico `INSERT ... ON CONFLICT DO UPDATE SET last_value = last_value + 1 RETURNING last_value` y retorna el número formateado
- [x] 1.5 Agregar sección `MemberNumberFormat` a `appsettings.json` con valores por defecto `{ "Prefix": "CM", "SequentialDigits": 5 }`
- [x] 1.6 Registrar `IMemberNumberGenerator → DefaultMemberNumberGenerator` y `MemberNumberFormatOptions` en `DependencyInjection.cs`

## 2. Aplicación — Comando EnrollMember

- [x] 2.1 Crear `EnrollMemberCommand` en `CreditSystem.Application/Commands/EnrollMember/` con propiedades `ExternalCustomerId`, `JoinedAt`, `InitialSharesAmount`, `SharesCurrency`
- [x] 2.2 Crear `EnrollMemberResponse` con propiedades `MemberId`, `MemberNumber`, `ExternalCustomerId`, `JoinedAt`
- [x] 2.3 Crear `EnrollMemberCommandHandler` que: (1) verifica existencia en `customer_credit_profiles` via `ICustomerReadRepository`, (2) verifica que no existe ya en `cooperative_members` via `ICooperativeMemberRepository.GetByExternalIdAsync`, (3) llama `IMemberNumberGenerator.GenerateAsync()`, (4) llama `CooperativeMemberAggregate.Register()`, (5) persiste via `ICooperativeMemberRepository.UpsertAsync()`, (6) retorna `EnrollMemberResponse`

## 3. API — Endpoint de enrolamiento

- [x] 3.1 Agregar `POST /enroll` en `MemberEndpoints.MapMemberEndpoints()` que mapea el body a `EnrollMemberCommand`, despacha via MediatR y retorna `201 Created` con `EnrollMemberResponse` o `400 Bad Request` con el mensaje de error

## 4. Limpieza — Eliminar flujo MemberSynced

- [x] 4.1 Eliminar archivo `MemberSyncedConsumer.cs`
- [x] 4.2 Eliminar directorio `CreditSystem.Application/Commands/SyncMemberFromCrm/` (command + handler)
- [x] 4.3 En `DependencyInjection.cs`: eliminar `x.AddConsumer<MemberSyncedConsumer>()` y `e.ConfigureConsumer<MemberSyncedConsumer>(context)` del endpoint RabbitMQ
