## MODIFIED Requirements

### Requirement: Verificación de existencia del cliente
El sistema SHALL verificar que el solicitante exista como `CustomerCreditProfile` **y** como `CooperativeMember` en el sistema local antes de ejecutar la evaluación crediticia.

Si `UnderwritingPolicy.RequireActiveMembership = true`, el sistema SHALL además verificar que el estado de la membresía sea `Active`. Si `RequireActiveMembership = false`, se permite continuar aunque no exista registro de membresía (modo de transición).

#### Scenario: Cliente no encontrado
- **WHEN** `ExternalCustomerId` no corresponde a ningún `CustomerCreditProfile` local
- **THEN** el sistema retorna `Success = false` con mensaje "Customer not found"
- **THEN** no se ejecuta el motor de reglas ni se crea ningún contrato

#### Scenario: Cliente encontrado pero no es socio y política es restrictiva
- **WHEN** `ExternalCustomerId` existe como `CustomerCreditProfile` pero no tiene `CooperativeMember` registrado
- **AND** `policy.RequireActiveMembership = true`
- **THEN** el sistema retorna `Success = false` con mensaje "Applicant is not a registered cooperative member"
- **THEN** no se ejecuta el motor de reglas

#### Scenario: Cliente encontrado y es socio activo
- **WHEN** `ExternalCustomerId` existe como `CustomerCreditProfile` y como `CooperativeMember` con estado `Active`
- **THEN** el sistema popula `ContractEvaluationContext` con `MemberSharesAmount` e `IsActiveMember = true`
- **THEN** continúa con la evaluación crediticia

#### Scenario: Cliente encontrado sin membresía y política permisiva
- **WHEN** `ExternalCustomerId` existe como `CustomerCreditProfile` pero no tiene `CooperativeMember`
- **AND** `policy.RequireActiveMembership = false`
- **THEN** el sistema continúa con `IsActiveMember = null` en el contexto
- **THEN** `MemberSharesRule` se omite

---

## ADDED Requirements

### Requirement: ContractEvaluationContext incluye datos de membresía
`ContractEvaluationContext` SHALL incluir dos campos opcionales para transportar información de membresía al motor de reglas:

- `MemberSharesAmount` (`Money?`): total de aportaciones del socio en la moneda de sus aportaciones
- `IsActiveMember` (`bool?`): `true` si el `CooperativeMember` tiene estado `Active`, `false` si está `Suspended` o `Withdrawn`, `null` si no hay registro de membresía

Estos campos son opcionales para mantener compatibilidad con tests que no involucran el contexto de cooperativa.

#### Scenario: Contexto construido con datos de membresía
- **WHEN** el handler de `CreateContract` resuelve un `CooperativeMember` activo con aportaciones
- **THEN** `context.MemberSharesAmount` contiene el `TotalAmount` del `MemberShare` del socio
- **THEN** `context.IsActiveMember` es `true`

#### Scenario: Contexto construido sin datos de membresía
- **WHEN** no existe `CooperativeMember` para el `ExternalCustomerId` y la política es permisiva
- **THEN** `context.MemberSharesAmount` es `null`
- **THEN** `context.IsActiveMember` es `null`
