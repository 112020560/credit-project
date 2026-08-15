## Why

El sistema actual modela clientes genéricos (`CustomerCreditProfile`) pero una cooperativa de crédito presta exclusivamente a sus **socios** — personas que han depositado capital social (aportaciones) y mantienen una membresía activa. Sin este concepto, el motor de evaluación no puede aplicar las reglas de negocio propias de una cooperativa ni cumplir con los requisitos regulatorios de SUGEF para entidades cooperativas.

## What Changes

- Se introduce el aggregate `CooperativeMember` que representa la membresía de un socio en la cooperativa, relacionado con `CustomerCreditProfile` por `ExternalId`.
- Se agrega el ciclo de vida de membresía: `Active`, `Suspended`, `Withdrawn`, con eventos de dominio correspondientes.
- Se modela el concepto de **aportaciones de capital** (`MemberShare`) como value object dentro del aggregate: monto acumulado, fecha de última aportación.
- Se expone `ICooperativeMemberRepository` como puerto de lectura/escritura en el Domain layer.
- Se implementa `CooperativeMemberRepository` en Infrastructure con persistencia en PostgreSQL.
- Se agrega sincronización del socio desde el CRM vía RabbitMQ (mismo patrón que `CustomerCreditProfile`).
- Se agrega la regla de underwriting `MemberSharesRule` que evalúa si el monto solicitado supera el límite permitido por las aportaciones del socio (configurable via `UnderwritingPolicy`).
- Se exponen endpoints de consulta del perfil del socio y sus aportaciones.
- Se actualiza `CreateContractCommandHandler` para resolver el `CooperativeMember` del solicitante y pasarlo al contexto de evaluación.

## Capabilities

### New Capabilities

- `cooperative-member`: Aggregate `CooperativeMember` con ciclo de vida de membresía, aportaciones de capital, sincronización desde CRM y repositorio de acceso.
- `member-shares-rule`: Regla de underwriting que evalúa el monto solicitado contra el límite derivado de las aportaciones del socio, configurable en `UnderwritingPolicy`.

### Modified Capabilities

- `loan-contract`: El contexto de evaluación `ContractEvaluationContext` incorpora datos de membresía (`MemberSharesAmount`, `SharesMultiplierLimit`) para que `MemberSharesRule` pueda evaluar. El handler de creación de contrato ahora requiere que el solicitante sea un socio activo.
- `underwriting-policy`: Se agregan dos nuevos parámetros a `UnderwritingPolicy`: `SharesMultiplierLimit` (entero, ej: 5) y `RequireActiveMembership` (bool).

## Impact

- **Domain**: nuevo aggregate `CooperativeMember`, value object `MemberShare`, nueva regla `MemberSharesRule`, interfaz `ICooperativeMemberRepository`, actualización de `ContractEvaluationContext` y `UnderwritingPolicy`.
- **Application**: nuevo comando `SyncMemberFromCrm`, handler de `CreateContract` actualizado, nuevo query `GetMemberProfile`.
- **Infrastructure**: `CooperativeMemberRepository` (Dapper/PostgreSQL), nuevo consumer RabbitMQ `MemberSyncedConsumer`, migración SQL para tabla `cooperative_members`.
- **API**: nuevo grupo de endpoints `/members`.
- **Breaking**: `CreateContractCommand` ahora falla si el cliente no tiene membresía activa en la cooperativa.
