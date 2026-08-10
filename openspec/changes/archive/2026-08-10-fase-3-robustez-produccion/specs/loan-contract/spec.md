## MODIFIED Requirements

### Requirement: Verificación de existencia del cliente
El sistema SHALL verificar que el solicitante exista como `CustomerCreditProfile` **y** como `CooperativeMember` en el sistema local antes de ejecutar la evaluación crediticia.

Si `UnderwritingPolicy.RequireActiveMembership = true`, el sistema SHALL además verificar que el estado de la membresía sea `Active`. Si `RequireActiveMembership = false`, se permite continuar aunque no exista registro de membresía (modo de transición).

Adicionalmente, toda creación y desembolso de contrato SHALL registrar la operación en `audit_log` con el `user_id` extraído del header `X-User-Id`.

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

#### Scenario: Contrato creado registrado en audit log

- **WHEN** el contrato es aprobado y persistido exitosamente
- **THEN** se inserta en `audit_log`: `action = "contract.created"`, `entity_id = loanId`, `user_id` del header `X-User-Id` (null si no se proveyó)
