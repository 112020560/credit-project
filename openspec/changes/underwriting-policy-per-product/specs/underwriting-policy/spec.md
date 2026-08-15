## MODIFIED Requirements

### Requirement: UnderwritingPolicy encapsula las políticas de evaluación crediticia
El sistema SHALL disponer de un record `UnderwritingPolicy` en el domain layer que encapsula los parámetros configurables del motor de evaluación: tasa base de interés, umbral de días para auto-default, comportamiento cuando no hay credit score, DTI máximo, y parámetros de mora y comisiones.

Campos:
- `BaseInterestRate` (`decimal`): tasa anual base sobre la que se acumulan los ajustes de las reglas
- `AutoDefaultThresholdDays` (`int`): días de atraso para disparar auto-default
- `NoScoreBehavior` (`NoScoreBehavior` enum): `ApproveWithPenalty` | `Reject`
- `SharesMultiplierLimit` (`int`): multiplicador de aportaciones para límite informativo
- `RequireActiveMembership` (`bool`): si exige membresía cooperativa activa
- `GracePeriodDays` (`int`): días de gracia antes de registrar mora
- `PenaltyRate` (`decimal`): tasa de mora anual
- `OriginationFeeRate` (`decimal`): comisión de apertura
- `EnforceSharesCapacityLimit` (`bool`): si el límite de aportaciones es hard stop
- `MaxDtiRatio` (`decimal`): ratio DTI máximo para `DebtToIncomeRule` y `PaymentCapacityRule`

#### Scenario: Política cargada correctamente desde base de datos por ID
- **WHEN** se invoca `IUnderwritingPolicyRepository.GetByIdAsync("default")`
- **THEN** retorna un `UnderwritingPolicy` con los valores de la fila `id = 'default'` en `underwriting_policies`

#### Scenario: Política no encontrada por ID
- **WHEN** se invoca `GetByIdAsync` con un ID que no existe en la tabla
- **THEN** retorna `null`

#### Scenario: Política con valores de seed por defecto
- **WHEN** la tabla `underwriting_policies` tiene el registro seed inicial
- **THEN** `BaseInterestRate = 8.0`, `AutoDefaultThresholdDays = 90`, `NoScoreBehavior = ApproveWithPenalty`, `MaxDtiRatio = 0.50`

---

### Requirement: IUnderwritingPolicyRepository expone GetByIdAsync
El sistema SHALL extender `IUnderwritingPolicyRepository` con un método `GetByIdAsync(string id, CancellationToken ct)` que carga una política específica por su identificador primario.

El método `GetActiveAsync` existente permanece sin cambios para compatibilidad con `LoanQueryService`.

#### Scenario: Consulta por ID existente
- **WHEN** se llama `GetByIdAsync("default")`
- **THEN** retorna el `UnderwritingPolicy` correspondiente con todos sus campos mapeados

#### Scenario: Consulta por ID inexistente
- **WHEN** se llama `GetByIdAsync("hipotecario")` y no existe esa fila
- **THEN** retorna `null` (no lanza excepción)

---

### Requirement: UnderwritingPolicy eliminada del contenedor DI como singleton
El sistema SHALL eliminar el registro `AddSingleton<UnderwritingPolicy>` de `DependencyInjection`. La policy deja de ser una dependencia inyectada directamente — se carga por demanda desde el repositorio en el handler de creación de contratos.

#### Scenario: Arranque de la aplicación sin singleton de policy
- **WHEN** la aplicación inicia
- **THEN** no existe ningún registro de `UnderwritingPolicy` como singleton en el contenedor DI
- **THEN** la aplicación arranca sin bloqueo síncrono en startup

#### Scenario: IUnderwritingPolicyRepository disponible como scoped
- **WHEN** `CreateContractCommandHandler` es resuelto desde DI
- **THEN** recibe `IUnderwritingPolicyRepository` inyectado (no `UnderwritingPolicy` directamente)
