# Credit Application

## Objetivo

Permitir que un cliente solicite un producto crediticio para posterior evaluación y aprobación.

## Requisito 1 - Crear solicitud

El sistema debe permitir crear una solicitud de crédito.

### Criterios

* Debe existir un cliente válido.
* Debe existir un producto crediticio válido.
* La solicitud inicia en estado Pending.

## Requisito 2 - Aprobar solicitud

El sistema debe permitir aprobar una solicitud.

### Criterios

* La solicitud debe existir.
* La solicitud no debe estar rechazada.
* Al aprobarse debe generarse una CreditLine.

## Requisito 3 - Rechazar solicitud

El sistema debe permitir rechazar una solicitud.

### Criterios

* La solicitud debe existir.
* Debe almacenarse el motivo de rechazo.
