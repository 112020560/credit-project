## Context

El sistema ya tiene `CustomerCreditProfile` como entidad local sincronizada desde el CRM. El dominio de cooperativa introduce una capa adicional: el **socio** (`CooperativeMember`), que es la persona que tiene una relación contractual con la cooperativa — ha firmado sus estatutos, tiene un número de socio, aporta capital social y mantiene una membresía con estado regulado.

La distinción es importante:
- `CustomerCreditProfile` = quién es la persona (identidad, score, ingresos)
- `CooperativeMember` = qué relación tiene con la cooperativa (número de socio, aportaciones, estado de membresía)

Una persona puede existir como `CustomerCreditProfile` (prospecto) sin ser aún socio. Solo los socios con membresía `Active` pueden solicitar crédito.

El sistema actual no tiene este concepto. El `CreateContractCommandHandler` solo verifica que exista un `CustomerCreditProfile`; no valida membresía ni aportaciones.

## Goals / Non-Goals

**Goals:**
- Modelar `CooperativeMember` como aggregate con ciclo de vida propio y eventos de dominio
- Sincronizar la membresía desde el CRM vía RabbitMQ con el mismo patrón que `CustomerCreditProfile`
- Integrar las aportaciones (`MemberShare`) en el contexto de evaluación de crédito
- Agregar `MemberSharesRule` al motor de underwriting como regla configurable
- Bloquear solicitudes de crédito de no-socios o socios suspendidos/retirados
- Exponer endpoints de consulta del perfil del socio

**Non-Goals:**
- Gestión de aportaciones como transacciones contables (eso pertenece al módulo de ahorros)
- Flujo de admisión de nuevos socios (se gestiona en el CRM externo)
- Cálculo de dividendos sobre aportaciones
- Autenticación/autorización (Fase 3 separada)

## Decisions

### D1: CooperativeMember como aggregate independiente (no entidad de CustomerCreditProfile)

`CooperativeMember` tiene su propio ciclo de vida, eventos de dominio y reglas de negocio. Extender `CustomerCreditProfile` crearía un aggregate demasiado ancho y mezclaría contextos (identidad crediticia vs membresía).

La relación se establece por `ExternalId` — el mismo identificador que usa el CRM para ambos conceptos.

**Alternativa descartada**: Agregar campos de membresía directo a `CustomerCreditProfile`. Descartada porque viola SRP y rompe la separación de bounded contexts.

### D2: MemberShare como value object dentro del aggregate

Las aportaciones son un dato financiero del socio (`TotalSharesAmount`, `LastContributionDate`, `NumberOfContributions`). No tienen identidad propia dentro del sistema de crédito — son un valor que se actualiza cuando el CRM sincroniza el perfil del socio.

Si en el futuro se requiere historial de aportaciones como transacciones individuales, ese modelo pertenece al módulo de ahorros, no al core de crédito.

**Alternativa descartada**: Tabla separada de `member_share_transactions`. Descartada por complejidad prematura — el core de crédito solo necesita el acumulado.

### D3: Sincronización vía mensaje MemberSynced en RabbitMQ

El CRM ya emite eventos de cliente (`CustomerCreated`, `CustomerUpdated`). Se agrega un nuevo mensaje `MemberSynced` en `SharedKernel` que contiene tanto los datos de membresía como las aportaciones actualizadas.

El consumer `MemberSyncedConsumer` hace upsert en la tabla `cooperative_members` usando `external_id` como llave de conflicto.

**Alternativa descartada**: Extender `CustomerCreated`/`CustomerUpdated` con campos de membresía. Descartada porque no todo cliente es socio, y mezclar los mensajes acopla los bounded contexts.

### D4: MemberSharesRule como hard stop configurable

Si el monto solicitado supera `TotalSharesAmount × SharesMultiplierLimit`, la solicitud se rechaza. Esta es una **hard stop rule** porque es una restricción estatutaria de la cooperativa, no solo una penalización de tasa.

`SharesMultiplierLimit` vive en `UnderwritingPolicy` para que sea configurable sin redeployment. Si el socio no tiene aportaciones registradas (monto = 0), la regla se omite con advertencia en los metadatos — no bloquea, para no afectar cooperativas en migración de datos.

### D5: ContractEvaluationContext se extiende con datos de membresía

Se agregan dos campos opcionales al record existente:
- `MemberSharesAmount`: `Money?` — total de aportaciones del socio
- `IsActiveMember`: `bool?` — si la membresía está activa

El handler de `CreateContract` resuelve el `CooperativeMember` por `ExternalId` y popula estos campos. Son opcionales para mantener retrocompatibilidad con tests existentes.

## Risks / Trade-offs

- **[Riesgo] Datos de aportaciones desactualizados** → El CRM controla el timing de sincronización. Si hay retraso, el socio puede pedir más de lo que le corresponde. Mitigación: el consumer hace upsert en tiempo real; la regla usa el valor más reciente en DB al momento de la evaluación.

- **[Riesgo] Socio activo sin CustomerCreditProfile** → Un socio puede existir sin score crediticio. El handler debe validar la existencia de ambos registros. Mitigación: validación explícita en el handler antes de construir el contexto de evaluación.

- **[Trade-off] Datos de membresía duplicados** → `cooperative_members` y `customer_credit_profiles` comparten `external_id` y algunos datos básicos. La duplicación es intencional para mantener los bounded contexts independientes y evitar JOINs en el hot path de evaluación.

- **[Riesgo] Regla omitida cuando aportaciones = 0** → Un socio nuevo sin aportaciones puede pedir cualquier monto si la regla se omite. Mitigación: `UnderwritingPolicy.RequireActiveMembership = true` bloquea solicitudes de no-socios; la regla de aportaciones es una capa adicional.

## Migration Plan

1. Aplicar migración SQL `20260808_AddCooperativeMembersTable.sql`
2. Hacer seed de datos de socios existentes desde el CRM (script de migración one-time)
3. Deployar nueva versión con consumer y endpoints
4. El flag `RequireActiveMembership` en `UnderwritingPolicy` inicia en `false` hasta que los datos estén migrados; una vez validados, se activa en BD sin redeployment

## Open Questions

- ¿Cuál es el `SharesMultiplierLimit` estándar de la cooperativa? (sugerencia: 5x como en muchas cooperativas SUGEF)
- ¿El CRM ya emite un evento `MemberSynced` o hay que coordinarlo con el equipo de integración?
- ¿Las aportaciones se expresan en CRC, USD o ambos? (impacta el tipo del value object `MemberShare`)
