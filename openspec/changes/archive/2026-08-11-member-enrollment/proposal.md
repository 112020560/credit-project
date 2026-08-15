## Why

El sistema de crédito necesita ser adaptable tanto a cooperativas como a financieras. Actualmente, el enrolamiento de socios cooperativos depende de un evento `MemberSynced` que proviene del CRM con el número de socio ya calculado — esto acopla incorrectamente el CRM a conceptos del dominio cooperativo y rompe la separación de responsabilidades. El CreditSystem debe ser el dueño del agregado `CooperativeMember`, incluyendo la generación del número de socio.

## What Changes

- **BREAKING** Eliminar `MemberSyncedConsumer`, `SyncMemberFromCrmCommand` y su handler — el enrolamiento ya no es automático vía evento del CRM
- Introducir `IMemberNumberGenerator` con implementación `DefaultMemberNumberGenerator` que genera números de socio en formato configurable (`CM-{año}-{seq}`) usando una secuencia atómica en base de datos
- Nuevo comando `EnrollMemberCommand` con su handler, que valida, genera el número de socio y registra el agregado
- Nuevo endpoint `POST /api/v1/members/enroll` que expone el enrolamiento como acción deliberada de un operador
- Migración SQL para la tabla `member_number_sequences`
- Configuración `MemberNumberFormat` en `appsettings.json`

## Capabilities

### New Capabilities

- `member-enrollment`: Enrolamiento explícito de un cliente como socio cooperativo. El CreditSystem genera el número de socio de forma atómica y configurable. El cliente debe existir previamente en `customer_credit_profiles`.
- `member-number-generation`: Servicio de generación de números de socio con formato configurable (prefijo + año + secuencial con padding), respaldado por una tabla de secuencias en la base de datos para garantizar unicidad.

### Modified Capabilities

- `customer-credit-profile`: No cambia el contrato de requisitos; el flujo `CustomerCreated → customer_credit_profiles` permanece sin cambios.

## Impact

- **Eliminados**: `MemberSyncedConsumer`, `SyncMemberFromCrmCommand`, `SyncMemberFromCrmCommandHandler`, registro en DI y en la cola RabbitMQ `credit-service-customer-events`
- **Nuevos archivos**: `IMemberNumberGenerator`, `DefaultMemberNumberGenerator`, `EnrollMemberCommand`, `EnrollMemberCommandHandler`, migration `member_number_sequences`
- **Modificados**: `MemberEndpoints` (nuevo endpoint enroll), `DependencyInjection.cs` (registro del generador), `appsettings.json`
- **Sin impacto**: Flujo `CustomerCreated`, agregados de préstamos, motor de reglas, proyectores
