## ADDED Requirements

### Requirement: CooperativeMember como aggregate del dominio
El sistema SHALL disponer de un aggregate `CooperativeMember` en `CreditSystem.Domain/Aggregates/CooperativeMember/` que encapsula la membresía de un socio en la cooperativa. Sus propiedades son:

- `Id` (`Guid`): identificador interno del registro de membresía
- `ExternalId` (`Guid`): identificador del socio en el CRM (mismo que `CustomerCreditProfile.ExternalId`)
- `MemberNumber` (`string`): número de socio asignado por la cooperativa
- `Status` (`MemberStatus` enum): `Active` | `Suspended` | `Withdrawn`
- `JoinedAt` (`DateTime`): fecha de ingreso como socio
- `Shares` (`MemberShare` value object): aportaciones de capital acumuladas

El aggregate SHALL ser creado únicamente via el factory method estático `CooperativeMember.Register(...)`. Su constructor vacío SHALL ser `private`.

#### Scenario: Creación via factory method
- **WHEN** se llama a `CooperativeMember.Register(externalId, memberNumber, joinedAt, initialShares)`
- **THEN** se retorna una instancia en estado `Active` con el evento `MemberRegistered` en `UncommittedEvents`
- **THEN** el constructor directo `new CooperativeMember()` es rechazado por el compilador

#### Scenario: Rehidratación desde eventos
- **WHEN** el repositorio rehidrata un `CooperativeMember` desde `(MemberState? snapshot, IEnumerable<IDomainEvent> events)`
- **THEN** el aggregate aplica cada evento en orden y el estado final refleja el historial

---

### Requirement: MemberShare como value object de aportaciones
El sistema SHALL disponer de un value object `MemberShare` en `CreditSystem.Domain/ValueObjects/` que encapsula las aportaciones de capital del socio:

- `TotalAmount` (`Money`): monto total acumulado de aportaciones
- `NumberOfContributions` (`int`): cantidad de aportaciones registradas
- `LastContributionDate` (`DateTime?`): fecha de la última aportación

`MemberShare` SHALL ser inmutable. Operaciones de actualización retornan una nueva instancia.

#### Scenario: Construcción de MemberShare
- **WHEN** se crea `MemberShare` con `totalAmount`, `numberOfContributions` y `lastContributionDate`
- **THEN** la instancia es inmutable y sus propiedades son de solo lectura

#### Scenario: Actualización de aportaciones
- **WHEN** se llama a `MemberShare.WithUpdatedContribution(additionalAmount, date)`
- **THEN** retorna una nueva instancia con `TotalAmount` incrementado y `LastContributionDate` actualizado

---

### Requirement: Ciclo de vida de la membresía
El aggregate `CooperativeMember` SHALL soportar las siguientes transiciones de estado con sus respectivos eventos de dominio:

- `Active` → `Suspended`: via `Suspend(reason)` → emite `MemberSuspended`
- `Suspended` → `Active`: via `Reinstate(reason)` → emite `MemberReinstated`
- `Active | Suspended` → `Withdrawn`: via `Withdraw(reason)` → emite `MemberWithdrawn`
- Cualquier estado: `UpdateShares(newShares)` → emite `MemberSharesUpdated`

#### Scenario: Suspensión de membresía
- **WHEN** se llama a `Suspend(reason)` en un `CooperativeMember` con estado `Active`
- **THEN** se emite `MemberSuspended` con motivo y timestamp
- **THEN** el estado transita a `Suspended`

#### Scenario: Suspensión rechazada si ya está suspendido
- **WHEN** se llama a `Suspend(reason)` en un socio ya `Suspended`
- **THEN** el aggregate lanza `DomainException`

#### Scenario: Retiro de membresía
- **WHEN** se llama a `Withdraw(reason)` en un socio `Active` o `Suspended`
- **THEN** se emite `MemberWithdrawn`
- **THEN** el estado transita a `Withdrawn` y no puede volver a `Active`

#### Scenario: Actualización de aportaciones
- **WHEN** se llama a `UpdateShares(newShares)` con un `MemberShare` de monto mayor
- **THEN** se emite `MemberSharesUpdated` con el nuevo monto y la fecha de actualización

---

### Requirement: ICooperativeMemberRepository como puerto de acceso
El sistema SHALL exponer `ICooperativeMemberRepository` en `CreditSystem.Domain/Abstractions/Repositories/` con las operaciones:

- `GetByExternalIdAsync(Guid externalId)`: retorna `CooperativeMember?`
- `GetByMemberNumberAsync(string memberNumber)`: retorna `CooperativeMember?`
- `UpsertAsync(Guid externalId, string memberNumber, MemberStatus status, DateTime joinedAt, MemberShare shares)`: crea o actualiza el registro del socio

La implementación `CooperativeMemberRepository` SHALL vivir en `CreditSystem.Infrastructure/Repositories/`.

#### Scenario: Resolución del repositorio desde DI
- **WHEN** se resuelve `ICooperativeMemberRepository` desde el contenedor DI
- **THEN** se obtiene `CooperativeMemberRepository` conectado a la tabla `cooperative_members`

#### Scenario: Búsqueda por ExternalId
- **WHEN** se llama a `GetByExternalIdAsync(externalId)` con un ID existente
- **THEN** retorna el `CooperativeMember` correspondiente con su estado y aportaciones actuales

#### Scenario: Búsqueda de socio inexistente
- **WHEN** se llama a `GetByExternalIdAsync(externalId)` con un ID que no existe
- **THEN** retorna `null`

---

### Requirement: Sincronización del socio desde CRM vía RabbitMQ
El sistema SHALL sincronizar los datos de membresía recibidos desde el CRM a través de un mensaje `MemberSynced` publicado en RabbitMQ. El consumer `MemberSyncedConsumer` hace upsert en la tabla `cooperative_members` usando `external_id` como llave de conflicto.

El mensaje `MemberSynced` SHALL contener: `ExternalId`, `MemberNumber`, `Status`, `JoinedAt`, `TotalSharesAmount`, `SharesCurrency`, `NumberOfContributions`, `LastContributionDate`.

#### Scenario: Sincronización de socio nuevo
- **WHEN** el CRM publica un mensaje `MemberSynced` para un `ExternalId` sin registro local
- **THEN** `MemberSyncedConsumer` crea un nuevo registro en `cooperative_members`
- **THEN** el estado y aportaciones reflejan los valores del mensaje

#### Scenario: Sincronización de socio existente
- **WHEN** el CRM publica `MemberSynced` para un `ExternalId` ya registrado
- **THEN** `MemberSyncedConsumer` actualiza el registro con los nuevos valores (upsert)

---

### Requirement: Tabla cooperative_members en PostgreSQL
El sistema SHALL tener una tabla `cooperative_members` con la siguiente estructura:

Columnas: `id` (UUID PK), `external_id` (UUID UNIQUE), `member_number` (VARCHAR UNIQUE), `status` (VARCHAR), `joined_at` (TIMESTAMPTZ), `total_shares_amount` (DECIMAL), `shares_currency` (VARCHAR), `number_of_contributions` (INT), `last_contribution_date` (TIMESTAMPTZ), `created_at` (TIMESTAMPTZ), `updated_at` (TIMESTAMPTZ).

#### Scenario: Migración aplicada exitosamente
- **WHEN** se ejecuta el script `20260808_AddCooperativeMembersTable.sql`
- **THEN** existe la tabla `cooperative_members` con todas las columnas descritas
- **THEN** existe índice único en `external_id` y en `member_number`

---

### Requirement: Endpoints de consulta del perfil del socio
El sistema SHALL exponer los siguientes endpoints en el grupo `/members`:

- `GET /api/members/{externalId}`: retorna el perfil completo del socio incluyendo estado y aportaciones
- `GET /api/members/{externalId}/shares`: retorna únicamente los datos de aportaciones

#### Scenario: Consulta de socio existente
- **WHEN** se consulta `GET /api/members/{externalId}` con un ID registrado
- **THEN** el sistema retorna `MemberProfileResponse` con `MemberNumber`, `Status`, `JoinedAt`, `TotalSharesAmount`, `SharesCurrency`

#### Scenario: Consulta de socio inexistente
- **WHEN** se consulta `GET /api/members/{externalId}` con un ID no registrado
- **THEN** el sistema retorna 404
