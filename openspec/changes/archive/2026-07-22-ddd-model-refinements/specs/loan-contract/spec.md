## ADDED Requirements

### Requirement: ContractApproved como evento explícito del ciclo de vida
El sistema SHALL emitir el evento `ContractApproved` cuando un contrato pasa la evaluación crediticia y queda en estado `Approved`, separando semánticamente la creación del contrato (persistencia de datos) de la aprobación (decisión de negocio).

El evento `ContractApproved` SHALL contener:
- `AggregateId`: ID del contrato
- `CustomerId`: ID del cliente
- `ApprovedRate`: tasa de interés aprobada (resultado del engine)
- `ApprovedPrincipal`: monto aprobado
- `EvaluationMetadata`: metadata del resultado de las reglas

#### Scenario: Contrato aprobado emite ContractApproved
- **WHEN** `ContractEngine` aprueba la solicitud
- **THEN** el factory method `LoanContractAggregate.Create(...)` emite `ContractCreated` seguido de `ContractApproved`
- **THEN** ambos eventos quedan en `UncommittedEvents` y se persisten juntos en el event store

#### Scenario: Contrato rechazado no emite ContractApproved
- **WHEN** `ContractEngine` rechaza la solicitud
- **THEN** no se crea ningún `LoanContractAggregate`
- **THEN** no se emite `ContractApproved`

#### Scenario: Rehidratación con ContractApproved en el historial
- **WHEN** el repositorio rehidrata un contrato desde eventos que incluyen `ContractApproved`
- **THEN** el `ApplyEvent` switch maneja `ContractApproved` correctamente
- **THEN** el estado del aggregate no cambia (ya está `Approved` por `ContractCreated`) — el evento es informativo/auditable
