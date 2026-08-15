# Spec: Loan Contract (delta)

Delta spec — modifica únicamente los requisitos afectados por la proyección asíncrona.

---

## MODIFIED Requirements

### Requirement: Respuesta al crear un contrato aprobado

El sistema SHALL retornar `HTTP 201` cuando un contrato es aprobado y persistido en el event store, independientemente del estado de los read models.

El `201` indica que el comando fue aceptado y los eventos fueron persistidos. No implica que los read models (`rm_loan_summaries`, etc.) estén actualizados en ese momento — los read models se actualizan de forma eventualmente consistente a través del `ProjectionDispatcherWorker`.

La respuesta DEBE incluir el campo `contractId` para que el cliente pueda consultar el estado del contrato una vez que el read model esté disponible.

#### Scenario: Contrato creado exitosamente
- **WHEN** el motor de reglas aprueba el contrato y los eventos se persisten en `stored_events`
- **THEN** el sistema retorna `HTTP 201` con `contractId`, `approvedRate` y `evaluationResults`
- **THEN** el read model `rm_loan_summaries` se actualiza de forma asíncrona (eventual)

#### Scenario: Fallo de proyección no afecta la respuesta
- **WHEN** el contrato se aprueba y se persiste, pero el `ProjectionDispatcherWorker` falla al proyectar
- **THEN** el sistema retorna `HTTP 201` igualmente
- **THEN** el fallo es visible en `GET /api/admin/projection-failures`
- **THEN** el contrato es recuperable vía rebuild de read models
