# Spec: Credit Application

> Describe reglas de negocio y criterios de aceptación.
> No describe implementación.

---

## Concepto

Una **Solicitud de Crédito** (`CreditApplication`) representa la intención de un cliente de obtener financiamiento bajo un producto crediticio específico. Pasa por un ciclo de revisión que termina en aprobación o rechazo. Si es aprobada, origina una Línea de Crédito activa.

---

## Estados

```
Pending ──→ Approved ──→ [genera CreditLine]
        └──→ Rejected
```

| Estado | Significado |
|--------|-------------|
| `Pending` | La solicitud fue recibida y está en revisión |
| `Approved` | Fue aceptada; se debe generar una CreditLine |
| `Rejected` | Fue rechazada; no genera CreditLine |

Una solicitud en estado `Approved` o `Rejected` es terminal: no admite más transiciones.

---

## Feature: Crear solicitud de crédito

**Como** operador del sistema
**Quiero** registrar una nueva solicitud de crédito para un cliente
**Para** iniciar el proceso de evaluación y aprobación

### Reglas de negocio

- RN-1: El cliente referenciado (`CustomerId`) debe existir en el sistema.
- RN-2: El producto crediticio referenciado (`ProductId`) debe existir en el sistema.
- RN-3: El `Amount` solicitado debe ser mayor a cero.
- RN-4: El `Amount` solicitado debe estar dentro del rango `[MinAmount, MaxAmount]` definido por el producto. Si el producto no tiene límites configurados, cualquier monto positivo es válido.
- RN-5: El `TermMonths` debe ser mayor a cero y no puede superar el plazo máximo del producto.
- RN-6: La solicitud se crea con estado inicial `Pending`.
- RN-7: `CreatedAt` y `UpdatedAt` se asignan automáticamente al momento de la creación.

### Criterios de aceptación

```gherkin
Scenario: Solicitud válida
  Given un Customer existente
  And un CreditProduct existente con MinAmount=1000, MaxAmount=50000, TermMonths=36
  When se envía CreateCreditApplicationCommand con Amount=10000 y TermMonths=12
  Then se crea una CreditApplication con Status="Pending"
  And se retorna el Id de la nueva solicitud

Scenario: Cliente inexistente
  Given un CustomerId que no existe en el sistema
  When se envía CreateCreditApplicationCommand
  Then la operación falla con error de tipo NotFound

Scenario: Producto inexistente
  Given un ProductId que no existe en el sistema
  When se envía CreateCreditApplicationCommand
  Then la operación falla con error de tipo NotFound

Scenario: Monto fuera del rango del producto
  Given un CreditProduct con MinAmount=1000 y MaxAmount=50000
  When se envía CreateCreditApplicationCommand con Amount=500
  Then la operación falla con error de tipo Validation

Scenario: Plazo excede el máximo del producto
  Given un CreditProduct con TermMonths=24
  When se envía CreateCreditApplicationCommand con TermMonths=36
  Then la operación falla con error de tipo Validation

Scenario: Monto cero o negativo
  When se envía CreateCreditApplicationCommand con Amount=0
  Then la operación falla con error de tipo Validation
```

---

## Feature: Aprobar solicitud de crédito

**Como** analista de crédito
**Quiero** aprobar una solicitud de crédito
**Para** activar la línea de crédito correspondiente para el cliente

### Reglas de negocio

- RN-8: Solo se puede aprobar una solicitud en estado `Pending`.
- RN-9: La solicitud referenciada debe existir.
- RN-10: La aprobación puede incluir condiciones distintas a las solicitadas: monto aprobado (`approved_amount`), plazo aprobado (`approved_term_months`) y tasa aprobada (`approved_rate`). Si no se proveen, se usan los valores originales de la solicitud y del producto.
- RN-11: El `approved_amount`, si se provee, debe ser mayor a cero.
- RN-12: El `approved_term_months`, si se provee, debe ser mayor a cero.
- RN-13: La aprobación cambia el estado de la solicitud a `Approved`.
- RN-14: Como efecto secundario de la aprobación, se genera una `CreditLine` en estado `Active` asociada a esta solicitud, con su schedule de amortización calculado.
- RN-15: `UpdatedAt` de la solicitud se actualiza al momento de la aprobación.

### Criterios de aceptación

```gherkin
Scenario: Aprobación sin ajustes
  Given una CreditApplication en estado Pending
  When se envía ApproveCreditApplicationCommand sin body
  Then la solicitud cambia a Status="Approved"
  And se crea una CreditLine con los términos originales de la solicitud
  And se retorna el Id de la CreditLine generada

Scenario: Aprobación con condiciones distintas a las solicitadas
  Given una CreditApplication en estado Pending con Amount=20000 y TermMonths=24
  When se envía ApproveCreditApplicationCommand con approved_amount=15000, approved_term_months=18, approved_rate=0.12
  Then la solicitud cambia a Status="Approved"
  And la CreditLine generada refleja Amount=15000, TermMonths=18 y Rate=0.12

Scenario: Solicitud inexistente
  Given un CreditApplicationId que no existe
  When se envía ApproveCreditApplicationCommand
  Then la operación falla con error de tipo NotFound

Scenario: Solicitud ya aprobada
  Given una CreditApplication en estado Approved
  When se envía ApproveCreditApplicationCommand
  Then la operación falla indicando que la solicitud no está en estado Pending

Scenario: Solicitud ya rechazada
  Given una CreditApplication en estado Rejected
  When se envía ApproveCreditApplicationCommand
  Then la operación falla indicando que la solicitud no está en estado Pending
```

---

## Feature: Rechazar solicitud de crédito

**Como** analista de crédito
**Quiero** rechazar una solicitud de crédito
**Para** notificar al cliente que su solicitud no fue aceptada

### Reglas de negocio

- RN-16: Solo se puede rechazar una solicitud en estado `Pending`.
- RN-17: La solicitud referenciada debe existir.
- RN-18: El motivo de rechazo (`rejection_reason`) es opcional, pero debe registrarse en `DecisionNotes` cuando se provee.
- RN-19: El rechazo cambia el estado de la solicitud a `Rejected`.
- RN-20: El rechazo no genera ninguna `CreditLine`.
- RN-21: `UpdatedAt` de la solicitud se actualiza al momento del rechazo.

### Criterios de aceptación

```gherkin
Scenario: Rechazo con motivo
  Given una CreditApplication en estado Pending
  When se envía RejectCreditApplicationCommand con rejection_reason="Score crediticio insuficiente"
  Then la solicitud cambia a Status="Rejected"
  And DecisionNotes queda como "Score crediticio insuficiente"
  And no se genera ninguna CreditLine

Scenario: Rechazo sin motivo
  Given una CreditApplication en estado Pending
  When se envía RejectCreditApplicationCommand sin body
  Then la solicitud cambia a Status="Rejected"
  And DecisionNotes permanece vacío

Scenario: Solicitud inexistente
  Given un CreditApplicationId que no existe
  When se envía RejectCreditApplicationCommand
  Then la operación falla con error de tipo NotFound

Scenario: Solicitud ya aprobada
  Given una CreditApplication en estado Approved
  When se envía RejectCreditApplicationCommand
  Then la operación falla indicando que la solicitud no está en estado Pending

Scenario: Solicitud ya rechazada
  Given una CreditApplication en estado Rejected
  When se envía RejectCreditApplicationCommand
  Then la operación falla indicando que la solicitud no está en estado Pending
```

---

## Invariantes globales

| # | Invariante |
|---|-----------|
| I-1 | Una solicitud solo puede tener un estado activo a la vez |
| I-2 | Los estados `Approved` y `Rejected` son terminales; no hay transición desde ellos |
| I-3 | Una `CreditLine` solo puede originarse desde una solicitud en estado `Approved` |
| I-4 | El `CustomerId` y `ProductId` de una solicitud no cambian después de su creación |
