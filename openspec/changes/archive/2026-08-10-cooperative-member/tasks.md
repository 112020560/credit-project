## 1. Domain — Value Objects y Enums

- [x] 1.1 Crear enum `MemberStatus` (`Active`, `Suspended`, `Withdrawn`) en `CreditSystem.Domain/Enums/`
- [x] 1.2 Crear value object `MemberShare` (`TotalAmount: Money`, `NumberOfContributions: int`, `LastContributionDate: DateTime?`) en `CreditSystem.Domain/ValueObjects/`
- [x] 1.3 Implementar método `MemberShare.WithUpdatedContribution(additionalAmount, date)` que retorna nueva instancia

## 2. Domain — Eventos de dominio del aggregate

- [x] 2.1 Crear `MemberRegistered` en `CreditSystem.Domain/Aggregates/CooperativeMember/Events/`
- [x] 2.2 Crear `MemberSuspended` con `Reason` y `SuspendedAt`
- [x] 2.3 Crear `MemberReinstated` con `Reason` y `ReinstatedAt`
- [x] 2.4 Crear `MemberWithdrawn` con `Reason` y `WithdrawnAt`
- [x] 2.5 Crear `MemberSharesUpdated` con `NewShares: MemberShare` y `UpdatedAt`

## 3. Domain — Aggregate CooperativeMember

- [x] 3.1 Crear `MemberState` (record inmutable) en `CreditSystem.Domain/Aggregates/CooperativeMember/`
- [x] 3.2 Crear `CooperativeMemberAggregate` con constructor privado, constructor de rehidratación `(MemberState? snapshot, IEnumerable<IDomainEvent> events)` y método `Apply` con switch por tipo de evento
- [x] 3.3 Implementar factory method `CooperativeMemberAggregate.Register(externalId, memberNumber, joinedAt, initialShares)`
- [x] 3.4 Implementar `Suspend(reason)` con validación de estado y emisión de `MemberSuspended`
- [x] 3.5 Implementar `Reinstate(reason)` con validación de estado y emisión de `MemberReinstated`
- [x] 3.6 Implementar `Withdraw(reason)` con validación de estado y emisión de `MemberWithdrawn`
- [x] 3.7 Implementar `UpdateShares(newShares)` con emisión de `MemberSharesUpdated`

## 4. Domain — Repositorio e interfaz

- [x] 4.1 Crear `ICooperativeMemberRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con métodos `GetByExternalIdAsync`, `GetByMemberNumberAsync` y `UpsertAsync`

## 5. Domain — Regla de underwriting MemberSharesRule

- [x] 5.1 Agregar campos `MemberSharesAmount: Money?` e `IsActiveMember: bool?` al record `ContractEvaluationContext`
- [x] 5.2 Agregar campos `SharesMultiplierLimit: int` y `RequireActiveMembership: bool` al record `UnderwritingPolicy`
- [x] 5.3 Actualizar el constructor de `UnderwritingPolicy` para incluir los nuevos campos
- [x] 5.4 Crear `MemberSharesRule` en `CreditSystem.Domain/Rules/Implementations/` implementando `IContractRule` e `IHardStopRule` con `Priority = 0`
- [x] 5.5 Implementar lógica de `MemberSharesRule`: validar `IsActiveMember` vs `RequireActiveMembership`, validar `RequestedAmount` vs `TotalSharesAmount × SharesMultiplierLimit`, omitir si `MemberSharesAmount` es null o cero

## 6. Infrastructure — Repositorio y persistencia

- [x] 6.1 Crear script de migración `20260808_AddCooperativeMembersTable.sql` con tabla `cooperative_members` e índices únicos en `external_id` y `member_number`
- [x] 6.2 Crear script de migración `20260808_AddMembershipPolicyColumns.sql` que agrega `shares_multiplier_limit` y `require_active_membership` a `underwriting_policies` con valores por defecto
- [x] 6.3 Implementar `CooperativeMemberRepository` en `CreditSystem.Infrastructure/Repositories/` usando Dapper para `GetByExternalIdAsync`, `GetByMemberNumberAsync` y `UpsertAsync`
- [x] 6.4 Actualizar `UnderwritingPolicyRepository` para leer los nuevos campos `shares_multiplier_limit` y `require_active_membership`
- [x] 6.5 Registrar `ICooperativeMemberRepository → CooperativeMemberRepository` en `DependencyInjection.cs`
- [x] 6.6 Registrar `MemberSharesRule` en el contenedor DI como `IContractRule`

## 7. SharedKernel — Mensaje de sincronización

- [x] 7.1 Crear record `MemberSynced` en `SharedKernel` con campos: `ExternalId`, `MemberNumber`, `Status`, `JoinedAt`, `TotalSharesAmount`, `SharesCurrency`, `NumberOfContributions`, `LastContributionDate`

## 8. Infrastructure — Consumer RabbitMQ

- [x] 8.1 Crear `MemberSyncedConsumer` en `CreditSystem.Infrastructure/Messaging/RabbitMq/Consumers/` que recibe `MemberSynced` y llama `ICooperativeMemberRepository.UpsertAsync`
- [x] 8.2 Registrar `MemberSyncedConsumer` en MassTransit en `DependencyInjection.cs` en el endpoint `credit-service-customer-events`

## 9. Application — Handler de sincronización y actualización de CreateContract

- [x] 9.1 Crear comando `SyncMemberFromCrm` y `SyncMemberFromCrmCommandHandler` en `CreditSystem.Application/Commands/SyncMemberFromCrm/` que delega a `ICooperativeMemberRepository.UpsertAsync`
- [x] 9.2 Actualizar `CreateContractCommandHandler`: resolver `CooperativeMemberAggregate` por `ExternalCustomerId`, validar membresía según `RequireActiveMembership`, poblar `MemberSharesAmount` e `IsActiveMember` en el contexto de evaluación

## 10. API — Endpoints del socio

- [x] 10.1 Crear `MemberEndpoints.cs` en `CreditSystem.Api/EndPoints/` con `GET /api/members/{externalId}` y `GET /api/members/{externalId}/shares`
- [x] 10.2 Crear DTOs de respuesta `MemberProfileResponse` y `MemberSharesResponse`
- [x] 10.3 Registrar `app.MapMemberEndpoints()` en `Program.cs`

## 11. Tests

- [x] 11.1 Agregar tests unitarios de `CooperativeMemberAggregate`: creación, suspensión, reinstatement, retiro, actualización de aportaciones
- [x] 11.2 Agregar tests unitarios de `MemberSharesRule`: límite superado, dentro del límite, sin aportaciones, membresía inactiva con política restrictiva/permisiva
- [x] 11.3 Actualizar tests de `CreateContractCommandHandler` para cubrir el caso de socio activo y no-socio
