## MODIFIED Requirements

### Requirement: Rehidratación del agregado desde eventos
El sistema SHALL reconstruir el estado de `LoanContractAggregate` desde un historial de eventos o desde un snapshot + delta de eventos usando el constructor canónico `LoanContractAggregate(LoanContractState? snapshot, IEnumerable<IDomainEvent> events)`. El constructor alternativo `LoanContractAggregate(IEnumerable<IDomainEvent>)` queda eliminado por producir doble aplicación de eventos y corrupción de estado.

#### Scenario: Rehidratación desde historial completo sin snapshot
- **WHEN** el repositorio reconstruye un contrato pasando `snapshot: null` y la lista completa de eventos
- **THEN** el aggregate aplica cada evento exactamente una vez en orden
- **THEN** el estado final refleja fielmente el historial completo

#### Scenario: Rehidratación desde snapshot + delta
- **WHEN** el repositorio reconstruye un contrato pasando un `LoanContractState` snapshot y los eventos posteriores al snapshot
- **THEN** el aggregate parte del estado del snapshot y aplica solo los eventos delta
- **THEN** el estado final es equivalente a haber aplicado todos los eventos desde el inicio

#### Scenario: Constructor eliminado no disponible
- **WHEN** cualquier código intenta usar `new LoanContractAggregate(IEnumerable<IDomainEvent>)`
- **THEN** el compilador rechaza la llamada (error de compilación)
- **THEN** el caller debe migrar a `new LoanContractAggregate(null, events)`
