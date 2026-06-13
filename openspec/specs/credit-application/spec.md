# Credit Application

## Purpose

El módulo Credit Application permite registrar, evaluar y resolver solicitudes de crédito realizadas por clientes para productos crediticios definidos en el sistema.

---

## Requirement: Create Credit Application

El sistema debe permitir registrar una solicitud de crédito para un cliente.

### Acceptance Criteria

* Debe existir un cliente válido.
* Debe existir un producto crediticio válido.
* La solicitud debe registrar el monto solicitado.
* La solicitud debe registrar la fecha de creación.
* La solicitud debe iniciar en estado `Pending`.
* Debe generarse un identificador único para la solicitud.

---

## Requirement: Approve Credit Application

El sistema debe permitir aprobar una solicitud de crédito.

### Acceptance Criteria

* La solicitud debe existir.
* La solicitud debe encontrarse en estado `Pending`.
* Una solicitud aprobada no puede volver a aprobarse.
* Al aprobarse debe registrarse la fecha de aprobación.
* Al aprobarse debe iniciarse el proceso de creación de la línea de crédito correspondiente.

---

## Requirement: Reject Credit Application

El sistema debe permitir rechazar una solicitud de crédito.

### Acceptance Criteria

* La solicitud debe existir.
* La solicitud debe encontrarse en estado `Pending`.
* Debe registrarse el motivo de rechazo.
* Una solicitud rechazada no puede aprobarse posteriormente.
* Una solicitud rechazada no puede volver a rechazarse.

---

## Business Rules

### BR-001

Una solicitud de crédito solamente puede pertenecer a un cliente existente.

### BR-002

Una solicitud de crédito solamente puede asociarse a un producto crediticio existente.

### BR-003

Una solicitud únicamente puede resolverse una vez.

Estados finales:

* Approved
* Rejected

### BR-004

Una solicitud aprobada debe derivar en una línea de crédito activa dentro del sistema.
