## Context

El sistema de crédito maneja dos entidades relacionadas pero distintas:
- `customer_credit_profiles`: copia local del perfil crediticio de cualquier cliente, poblada automáticamente vía `CustomerCreated` desde el CRM.
- `cooperative_members`: agregado del dominio cooperativo, que representa al socio como propietario de la cooperativa.

Actualmente `cooperative_members` se llena vía `MemberSynced` — un evento que el CRM publica con el número de socio ya incluido. Esto viola la arquitectura correcta: el CRM es genérico (sirve para cooperativas y financieras) y no debería conocer el concepto de "número de socio". `CooperativeMemberAggregate` ya existe en el dominio con `Register()` correcto; solo falta el flujo que lo active.

## Goals / Non-Goals

**Goals:**
- Hacer que `CreditSystem` sea el único dueño del número de socio
- Proveer un endpoint explícito de enrolamiento que un operador o backoffice pueda llamar
- Generar números de socio de forma atómica y configurable
- Eliminar el acoplamiento CRM → número de socio
- El módulo cooperativo debe ser opcional (una financiera no lo usa)

**Non-Goals:**
- No se modifica el flujo `CustomerCreated → customer_credit_profiles`
- No se implementa gestión de aportaciones (shares) más allá del monto inicial al enrolar
- No se implementa un workflow de aprobación de membresía (la aprobación es responsabilidad del proceso de negocio externo)
- No se migran datos históricos de `cooperative_members` existentes

## Decisions

### D1: Número de socio generado en CreditSystem, no recibido del CRM

**Decisión:** `IMemberNumberGenerator` vive en `Domain.Abstractions`; su implementación `DefaultMemberNumberGenerator` en Infrastructure usa una tabla `member_number_sequences (year INT PK, last_value INT)` con un `INSERT ... ON CONFLICT DO UPDATE ... RETURNING last_value` atómico.

**Alternativas consideradas:**
- Secuencia PostgreSQL nativa (`CREATE SEQUENCE`): requiere una secuencia por año, implica DDL dinámico al cambiar de año. Más complejo de mantener.
- UUID como número de socio: no es legible por humanos ni compatible con el formato cooperativo estándar.
- Generación en memoria (contador en singleton): no sobrevive reinicios ni funciona en múltiples instancias.

**Rationale:** Una tabla con `ON CONFLICT DO UPDATE RETURNING` es atómica, simple, portable y no requiere DDL dinámico.

### D2: Enrolamiento vía endpoint explícito, no vía evento del CRM

**Decisión:** `POST /api/v1/members/enroll` es la única puerta de entrada al enrolamiento. No existe ningún consumer de mensajería que cree socios automáticamente.

**Alternativas consideradas:**
- Flag `IsMember: true` en `CustomerCreated` metadata: mezcla lógica cooperativa en el consumer genérico.
- Evento separado `CooperativeMemberEnrolled` desde el CRM: el CRM seguiría necesitando conocer el concepto de socio cooperativo.

**Rationale:** El enrolamiento es un acto de negocio deliberado (firma de contrato, pago de aportación inicial). Modelarlo como un endpoint explícito refleja esa intención y mantiene el CRM agnóstico.

### D3: Eliminación de MemberSynced y SyncMemberFromCrm

**Decisión:** Eliminar `MemberSyncedConsumer`, `SyncMemberFromCrmCommand`, `SyncMemberFromCrmCommandHandler`, y desregistrarlos de DI y RabbitMQ.

**Rationale:** Con el nuevo flujo, estos componentes son redundantes y representan la arquitectura incorrecta. Mantenerlos crea ambigüedad sobre cuál es el flujo autoritativo.

### D4: Validación previa — el cliente debe existir

**Decisión:** El `EnrollMemberCommandHandler` consulta `ICustomerReadRepository` para verificar que el `ExternalCustomerId` existe en `customer_credit_profiles` antes de enrolar.

**Rationale:** El socio cooperativo es un rol que juega un cliente. Sin perfil crediticio, el sistema de crédito no puede operar el préstamo cuando lo solicite. Esto garantiza el orden correcto: primero cliente, luego socio.

## Risks / Trade-offs

- **[Risk] Secuencial con gaps:** Si una transacción de enrolamiento falla después de incrementar la secuencia, el número queda "quemado" y habrá un gap en la numeración (ej. CM-2026-00003 → CM-2026-00005). → **Mitigation:** Aceptable; es el comportamiento estándar de secuencias en bases de datos. Los números de socio no necesitan ser contiguos.

- **[Risk] Datos existentes en cooperative_members:** Si hay socios ya registrados vía `MemberSynced`, sus números de socio pueden no seguir el formato nuevo. → **Mitigation:** No se migran datos históricos. Los socios existentes conservan sus números; los nuevos usarán el formato configurable.

- **[Risk] Eliminación de MemberSynced sin notificar al CRM:** Si el CRM sigue publicando `MemberSynced`, nadie lo consumirá. → **Mitigation:** El mensaje simplemente no tendrá consumer — MassTransit lo descartará. No es un error, pero debe coordinarse con el equipo del CRM para evitar publicación innecesaria.

## Migration Plan

1. Aplicar migración `member_number_sequences` en la base de datos.
2. Desplegar el nuevo código (el consumer eliminado deja de procesar mensajes).
3. El CRM puede seguir publicando `MemberSynced` sin impacto (nadie lo consume).
4. Los operadores usan `POST /api/v1/members/enroll` para nuevos enrolamientos.

**Rollback:** Revertir el código restaura `MemberSyncedConsumer`. La tabla `member_number_sequences` puede quedar sin efecto (no tiene FK a ninguna tabla crítica).

## Open Questions

- ¿El número de socio debe ser visible / editable post-creación? Si la cooperativa necesita corregirlo, falta un endpoint de actualización.
- ¿Se requiere un evento de dominio publicado externamente al enrolar (`MemberEnrolled` hacia el CRM u otros sistemas)? Actualmente no se publica ningún evento de integración.
