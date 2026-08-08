## MODIFIED Requirements

### Requirement: UnderwritingPolicy encapsula las políticas de evaluación crediticia
El sistema SHALL disponer de un record `UnderwritingPolicy` en el domain layer que encapsula los parámetros configurables del motor de evaluación: tasa base de interés, umbral de días para auto-default, comportamiento cuando no hay credit score disponible, límite multiplicador de aportaciones y requerimiento de membresía activa.

Campos:
- `BaseInterestRate` (`decimal`): tasa anual base. Valor por defecto: 8.0
- `AutoDefaultThresholdDays` (`int`): días de atraso para disparar auto-default. Valor por defecto: 90
- `NoScoreBehavior` (`NoScoreBehavior` enum): `ApproveWithPenalty` | `Reject`. Valor por defecto: `ApproveWithPenalty`
- `SharesMultiplierLimit` (`int`): múltiplo máximo de aportaciones que puede solicitar un socio (ej: 5 = hasta 5× sus aportaciones). Valor por defecto: 5
- `RequireActiveMembership` (`bool`): si `true`, bloquea solicitudes de no-socios o socios suspendidos/retirados. Valor por defecto: `false` (modo transición)

#### Scenario: Política cargada correctamente desde base de datos
- **WHEN** la aplicación inicia y resuelve `IUnderwritingPolicyRepository`
- **THEN** retorna un `UnderwritingPolicy` con los valores de la tabla `underwriting_policies`

#### Scenario: Política con valores de seed por defecto
- **WHEN** la tabla `underwriting_policies` tiene el registro seed inicial
- **THEN** `BaseInterestRate = 8.0`, `AutoDefaultThresholdDays = 90`, `NoScoreBehavior = ApproveWithPenalty`, `SharesMultiplierLimit = 5`, `RequireActiveMembership = false`

---

## ADDED Requirements

### Requirement: Tabla underwriting_policies incluye columnas de membresía
La tabla `underwriting_policies` SHALL incluir las columnas `shares_multiplier_limit` (INT NOT NULL DEFAULT 5) y `require_active_membership` (BOOLEAN NOT NULL DEFAULT FALSE). La migración es aditiva (no rompe registros existentes).

#### Scenario: Migración aditiva exitosa
- **WHEN** se ejecuta la migración `20260808_AddMembershipPolicyColumns.sql`
- **THEN** la tabla `underwriting_policies` tiene las nuevas columnas con valores por defecto
- **THEN** el registro existente `id = 'default'` conserva sus valores anteriores y recibe los nuevos con el default
