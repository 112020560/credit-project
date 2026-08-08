# Spec: Loan Contract

Gestión del ciclo de vida completo de préstamos a plazo fijo mediante Event Sourcing. Incluye evaluación crediticia automática, desembolso, gestión de pagos, acumulación de interés, detección de mora, default, reestructuración y cancelación anticipada.

---

## Requirements

### Requirement: Validación de entrada al crear un contrato
El sistema SHALL rechazar solicitudes de creación de contrato que no cumplan con los parámetros mínimos de entrada antes de ejecutar cualquier evaluación crediticia.

Parámetros requeridos:
- `ExternalCustomerId`: GUID no vacío del cliente en el CRM
- `Amount`: decimal > 0 y <= 1,000,000
- `Currency`: uno de `USD`, `EUR`, `CRC`
- `TermMonths`: entero entre 1 y 360 (inclusive)
- `CollateralValue`: si se provee, MUST ser > 0
- `AmortizationMethod`: valor válido del enum (default: `French`)

#### Scenario: Monto fuera de rango rechazado en validación
- **WHEN** se envía `Amount = 0` o `Amount > 1,000,000`
- **THEN** el sistema retorna error de validación sin ejecutar el motor de reglas

#### Scenario: Moneda no soportada rechazada
- **WHEN** se envía `Currency = "MXN"` u otra no listada
- **THEN** el sistema retorna error de validación indicando las monedas permitidas

#### Scenario: Plazo fuera de rango rechazado
- **WHEN** `TermMonths < 1` o `TermMonths > 360`
- **THEN** el sistema retorna error de validación

#### Scenario: Colateral con valor negativo rechazado
- **WHEN** se provee `CollateralValue <= 0`
- **THEN** el sistema retorna error de validación

---

### Requirement: Verificación de existencia del cliente
El sistema SHALL verificar que el cliente exista en el sistema local (sincronizado desde CRM) mediante su `ExternalCustomerId` antes de ejecutar la evaluación crediticia.

#### Scenario: Cliente no encontrado
- **WHEN** `ExternalCustomerId` no corresponde a ningún `CustomerReference` local
- **THEN** el sistema retorna `Success = false` con mensaje "Customer not found"
- **THEN** no se ejecuta el motor de reglas ni se crea ningún contrato

#### Scenario: Cliente encontrado
- **WHEN** `ExternalCustomerId` existe localmente
- **THEN** el sistema continúa con la evaluación crediticia usando el `Id` interno del cliente

---

### Requirement: Evaluación crediticia — Límite máximo de préstamo
El sistema SHALL evaluar el monto solicitado contra límites absolutos y relativos al ingreso del cliente. Esta regla es un **hard stop**: si falla, se detiene toda la evaluación.

Límites:
- Monto absoluto máximo: **500,000** (en la moneda solicitada)
- Si se conoce el ingreso mensual del cliente: monto máximo = `ingreso_mensual × 12 × 5`

#### Scenario: Monto supera el límite absoluto
- **WHEN** `Amount > 500,000`
- **THEN** la regla falla con hard stop
- **THEN** el contrato es rechazado indicando que supera el máximo absoluto

#### Scenario: Monto supera 5x el ingreso anual
- **WHEN** el cliente tiene `MonthlyIncome` registrado y `Amount > MonthlyIncome × 12 × 5`
- **THEN** la regla falla con hard stop
- **THEN** el contrato es rechazado indicando que supera el límite basado en ingresos

#### Scenario: Monto dentro de límites
- **WHEN** `Amount <= 500,000` y (sin ingresos registrados O `Amount <= MonthlyIncome × 12 × 5`)
- **THEN** la regla pasa y la evaluación continúa

---

### Requirement: Evaluación crediticia — Score crediticio
El sistema SHALL evaluar el score crediticio del cliente y ajustar la tasa de interés según el nivel de riesgo. Esta regla es un **hard stop** si el score existe y es menor a 500.

Score mínimo aceptable: **500**. Sin score registrado: la regla se omite (no bloquea).

Ajustes de tasa por score:
- Score >= 750 → **+0%**
- Score >= 700 → **+1.5%**
- Score >= 650 → **+3%**
- Score >= 600 → **+5%**
- Score >= 550 → **+8%**
- Score >= 500 → **+12%**

#### Scenario: Score por debajo del mínimo
- **WHEN** el cliente tiene `CreditScore < 500`
- **THEN** la regla falla con hard stop
- **THEN** el contrato es rechazado indicando el score y el mínimo requerido

#### Scenario: Score excelente sin ajuste de tasa
- **WHEN** `CreditScore >= 750`
- **THEN** la regla pasa con ajuste de tasa **+0%**

#### Scenario: Score en rango medio con ajuste
- **WHEN** `500 <= CreditScore < 750`
- **THEN** la regla pasa con el ajuste de tasa correspondiente al tramo

#### Scenario: Cliente sin score registrado
- **WHEN** `CreditScore` es null
- **THEN** la regla se omite (pasa sin ajuste de tasa ni hard stop)

---

### Requirement: Evaluación crediticia — Relación Deuda/Ingreso (DTI)
El sistema SHALL calcular el DTI del cliente incluyendo la cuota estimada del nuevo préstamo y rechazar si supera el 50%. Sin datos de ingreso, la regla se omite.

Fórmula: `DTI = (deuda_mensual_existente + cuota_estimada_nueva) / ingreso_mensual`

Cuota estimada: calculada con tasa referencial 12% anual sobre el monto y plazo solicitados.

Umbrales:
- DTI > 50% → **rechazado**
- DTI > 40% y <= 50% → **aprobado con ajuste +2% a la tasa**
- DTI <= 40% → **aprobado sin ajuste**

#### Scenario: DTI supera el máximo permitido
- **WHEN** DTI calculado > 50%
- **THEN** la regla falla (no es hard stop, pero contribuye al rechazo general)
- **THEN** el resultado indica DTI, ingreso mensual y deuda total

#### Scenario: DTI en zona de advertencia
- **WHEN** 40% < DTI <= 50%
- **THEN** la regla pasa con ajuste de tasa **+2%**

#### Scenario: DTI aceptable
- **WHEN** DTI <= 40%
- **THEN** la regla pasa sin ajuste de tasa

#### Scenario: Sin datos de ingreso
- **WHEN** `MonthlyIncome` es null o cero
- **THEN** la regla se omite sin bloquear ni ajustar la tasa

---

### Requirement: Evaluación crediticia — Colateral
El sistema SHALL evaluar el colateral provisto y ajustar la tasa de interés según la cobertura. El colateral es opcional; su ausencia no rechaza el préstamo.

Ajustes según ratio de colateral (`colateral / monto_solicitado`):
- Sin colateral → préstamo no garantizado, ajuste **+1%**
- Ratio < 1.0 (colateral < monto) → garantía parcial, ajuste **+0.5%**
- Ratio >= 1.0 y < 1.2 → garantizado, descuento **-0.5%**
- Ratio >= 1.2 → bien garantizado, descuento **-1.5%**

#### Scenario: Sin colateral provisto
- **WHEN** `CollateralValue` es null o cero
- **THEN** la regla pasa con ajuste de tasa **+1%** (préstamo no garantizado)

#### Scenario: Colateral parcial
- **WHEN** `CollateralValue > 0` y `CollateralValue / Amount < 1.0`
- **THEN** la regla pasa con ajuste de tasa **+0.5%**

#### Scenario: Colateral suficiente
- **WHEN** `CollateralValue / Amount >= 1.0` y `< 1.2`
- **THEN** la regla pasa con descuento de tasa **-0.5%**

#### Scenario: Colateral excelente
- **WHEN** `CollateralValue / Amount >= 1.2`
- **THEN** la regla pasa con descuento de tasa **-1.5%**

---

### Requirement: Evaluación crediticia — Préstamos activos existentes
El sistema SHALL aplicar un ajuste de tasa adicional si el cliente ya tiene préstamos activos en el sistema.

#### Scenario: Cliente con préstamos activos
- **WHEN** el cliente tiene al menos un `LoanContract` en estado `Active` o `Delinquent`
- **THEN** la regla pasa con ajuste de tasa **+1%**

#### Scenario: Cliente sin préstamos activos
- **WHEN** el cliente no tiene préstamos activos
- **THEN** la regla pasa sin ajuste de tasa

#### Scenario: Estado de préstamos no disponible
- **WHEN** `HasActiveLoans` es null
- **THEN** la regla se omite sin ajuste

---

### Requirement: Cálculo de tasa final y decisión de aprobación
El sistema SHALL calcular la tasa de interés final sumando los ajustes de todas las reglas a la tasa base, y aprobar o rechazar el contrato según el resultado del motor.

- Tasa base: **8%**
- Tasa final = `8% + Σ(ajustes de todas las reglas evaluadas)`
- Si alguna regla falla (independientemente de si es hard stop o no): contrato **rechazado**
- Si todas las reglas pasan: contrato **aprobado** con la tasa final calculada

#### Scenario: Todas las reglas pasan — contrato aprobado
- **WHEN** ninguna regla falla en la evaluación
- **THEN** el sistema retorna aprobación con `ContractId`, `ApprovedRate` y los resultados de cada regla

#### Scenario: Al menos una regla falla — contrato rechazado
- **WHEN** una o más reglas devuelven resultado fallido
- **THEN** el sistema retorna rechazo con los mensajes de las reglas que fallaron
- **THEN** no se crea ningún evento ni se persiste ningún contrato

#### Scenario: Hard stop detiene la evaluación
- **WHEN** una regla con `IHardStopRule` falla
- **THEN** la evaluación se detiene inmediatamente sin ejecutar las reglas restantes

---

### Requirement: Creación del contrato y generación del schedule
El sistema SHALL crear el `LoanContractAggregate` con el schedule de amortización calculado según el método seleccionado, emitir el evento `ContractCreated` y persistirlo en el event store.

Métodos de amortización disponibles: `French`, `German`, `Flat`, `American`, `InterestOnly`.

El contrato se crea en estado **Approved**. El `NextPaymentDue` se inicializa con la primera cuota del schedule.

#### Scenario: Creación exitosa con método francés (default)
- **WHEN** se aprueba un contrato sin especificar `AmortizationMethod`
- **THEN** se usa `French` como método por defecto
- **THEN** se emite `ContractCreated` con el schedule calculado
- **THEN** el contrato queda en estado `Approved`

#### Scenario: Creación con método alemán
- **WHEN** se aprueba un contrato con `AmortizationMethod = German`
- **THEN** el schedule refleja capital constante con interés decreciente por cuota

#### Scenario: Schedule persistido como parte del evento
- **WHEN** se crea el contrato
- **THEN** el `PaymentSchedule` completo con todas las `AmortizationEntry` queda registrado en el evento `ContractCreated`

---

### Requirement: Desembolso del préstamo
El sistema SHALL permitir desembolsar un préstamo que esté en estado `Approved`, transitando al estado `Active`.

#### Scenario: Desembolso exitoso
- **WHEN** se desembolsa un contrato en estado `Approved` con método y cuenta de destino
- **THEN** se emite `LoanDisbursed` con monto, método, cuenta y timestamp
- **THEN** el contrato transita a estado `Active`

#### Scenario: Desembolso rechazado si no está en Approved
- **WHEN** se intenta desembolsar un contrato en estado distinto a `Approved`
- **THEN** el sistema lanza `DomainException` indicando el estado actual
- **THEN** no se emite ningún evento

---

### Requirement: Aplicación de pagos
El sistema SHALL aplicar pagos a préstamos en estado `Active` o `Delinquent`, distribuyendo el monto en orden: **fees pendientes → interés acumulado → principal**.

Si el pago cubre completamente el saldo restante, el sistema emite automáticamente `ContractPaidOff`.

#### Scenario: Pago parcial en orden correcto
- **WHEN** se aplica un pago insuficiente para cubrir todas las deudas
- **THEN** se emite `PaymentApplied` con breakdown detallado: `FeePaid`, `InterestPaid`, `PrincipalPaid`, `NewBalance`
- **THEN** el saldo se reduce en el monto de capital efectivamente pagado

#### Scenario: Pago cubre saldo completo — payoff automático
- **WHEN** el pago cubre `CurrentBalance + AccruedInterest + TotalFees` completo
- **THEN** se emite `PaymentApplied` seguido automáticamente de `ContractPaidOff`
- **THEN** el contrato transita a estado `PaidOff` con `CurrentBalance = 0`

#### Scenario: Moneda incorrecta rechazada
- **WHEN** el pago se realiza en moneda distinta a la del contrato
- **THEN** el sistema lanza `DomainException` indicando la moneda esperada

#### Scenario: Pago en contrato no pagable rechazado
- **WHEN** el contrato está en estado `Default` o `PaidOff`
- **THEN** el sistema lanza `DomainException` indicando que el contrato no está en estado pagable

---

### Requirement: Acumulación de interés diario
El sistema SHALL acumular interés sobre el saldo principal de préstamos activos de forma periódica mediante un worker en background.

Fórmula: `interés_periodo = tasa_diaria(balance) × días_del_periodo`

El interés acumulado se suma a `AccruedInterest`; no reduce el `CurrentBalance`.

#### Scenario: Acumulación exitosa en contrato activo
- **WHEN** se ejecuta `AccrueInterest(periodStart, periodEnd)` sobre un contrato `Active`
- **THEN** se emite `InterestAccrued` con monto, período y saldo al momento del cálculo
- **THEN** `AccruedInterest` se incrementa en el monto calculado

#### Scenario: Acumulación rechazada en estado incorrecto
- **WHEN** se intenta acumular interés en un contrato que no está en estado `Active`
- **THEN** el sistema lanza `DomainException`

---

### Requirement: Registro de pago perdido y transición a Delinquent
El sistema SHALL registrar pagos perdidos en préstamos `Active` o `Delinquent` cuando una cuota no se paga en su fecha de vencimiento, aplicar un cargo por mora (`lateFee`) y transitar el contrato a `Delinquent`.

#### Scenario: Primera cuota perdida — transición a Delinquent
- **WHEN** se registra un pago perdido en un contrato `Active`
- **THEN** se emite `PaymentMissed` con número de cuota, fecha de vencimiento, monto adeudado, días de mora y cargo por mora
- **THEN** `TotalFees` aumenta en el `lateFee` aplicado
- **THEN** el contrato transita a estado `Delinquent`

#### Scenario: Cuota no registrada dos veces
- **WHEN** se intenta registrar como perdido un pago con número ya procesado
- **THEN** el sistema lanza `DomainException`

#### Scenario: Cuota no existente en schedule
- **WHEN** se registra como perdido un número de cuota que no existe en el schedule
- **THEN** el sistema lanza `DomainException`

---

### Requirement: Auto-default por mora extendida
El sistema SHALL marcar automáticamente un contrato como `Default` si un pago registrado como perdido lleva **90 o más días** de mora.

#### Scenario: Auto-default al registrar pago perdido con 90+ días
- **WHEN** `daysOverdue >= 90` al momento de registrar un `PaymentMissed`
- **THEN** se emite `PaymentMissed` y, acto seguido, `ContractDefaulted`
- **THEN** el contrato transita a estado `Default`

#### Scenario: Default manual explícito
- **WHEN** se ejecuta `MarkAsDefault(reason)` con cualquier motivo
- **THEN** se emite `ContractDefaulted` si el contrato no está ya en `Default`
- **THEN** el contrato transita a estado `Default`

#### Scenario: Default idempotente
- **WHEN** se llama `MarkAsDefault` en un contrato ya en `Default`
- **THEN** no se emite ningún evento adicional

---

### Requirement: Reestructuración de préstamo
El sistema SHALL permitir reestructurar un préstamo en estado `Delinquent` o `Default`, aplicando nuevas condiciones (tasa, plazo, condonación parcial) y volviendo el contrato a estado `Active`.

Condiciones:
- La nueva tasa, nuevo plazo y monto a condonar son parámetros explícitos
- El nuevo saldo = `CurrentBalance - ForgiveAmount`
- El nuevo saldo MUST ser > 0
- Los pagos perdidos se resetean a 0
- Se recalcula el schedule con las nuevas condiciones

#### Scenario: Reestructuración exitosa desde Default
- **WHEN** se reestructura un contrato en estado `Default` con parámetros válidos
- **THEN** se emite `ContractRestructured` con nueva tasa, nuevo plazo, nuevo schedule y monto condonado
- **THEN** el contrato transita a `Active` con `PaymentsMissed = 0`

#### Scenario: Reestructuración rechazada desde estado inválido
- **WHEN** se intenta reestructurar un contrato `Active` o `PaidOff`
- **THEN** el sistema lanza `DomainException` indicando que solo se puede reestructurar desde `Delinquent` o `Default`

#### Scenario: Condonación que deja saldo cero o negativo rechazada
- **WHEN** `ForgiveAmount >= CurrentBalance`
- **THEN** el sistema lanza `DomainException` indicando que el nuevo saldo debe ser mayor a cero

---

### Requirement: Cancelación anticipada (Payoff)
El sistema SHALL permitir cancelar completamente un préstamo pagando el monto total adeudado (`CurrentBalance + AccruedInterest + TotalFees`) en un único pago.

#### Scenario: Payoff exitoso
- **WHEN** se ejecuta `PayoffContract` en un préstamo `Active` o `Delinquent`
- **THEN** se aplica un `ApplyPayment` por el monto total adeudado en ese momento
- **THEN** se emite `PaymentApplied` + `ContractPaidOff`
- **THEN** el contrato transita a `PaidOff` con saldo cero

#### Scenario: Consulta de monto de payoff
- **WHEN** se consulta `GetPayoffAmount` para un contrato existente (con fecha opcional `AsOfDate`)
- **THEN** el sistema retorna el monto exacto necesario para cancelar el préstamo: `CurrentBalance + AccruedInterest + TotalFees`

---

### Requirement: Consultas de préstamos
El sistema SHALL proveer consultas de solo lectura sobre el estado y el historial de los contratos, respondidas desde las proyecciones (read models) sin rehidratar el agregado.

#### Scenario: Consulta de resumen por ID
- **WHEN** se consulta `GET /api/loans/{id}`
- **THEN** el sistema retorna el estado actual del préstamo (saldo, tasa, status, próxima cuota, etc.)
- **WHEN** el ID no existe
- **THEN** el sistema retorna 404

#### Scenario: Consulta de préstamos por cliente
- **WHEN** se consulta `GET /api/loans/customer/{externalCustomerId}`
- **THEN** el sistema retorna todos los préstamos del cliente identificado por su ID externo (CRM)
- **WHEN** el cliente no existe en el sistema local
- **THEN** el sistema retorna 404

#### Scenario: Consulta de historial de pagos
- **WHEN** se consulta `GET /api/loans/{id}/payments`
- **THEN** el sistema retorna la lista de todos los pagos aplicados con su breakdown

#### Scenario: Consulta de préstamos morosos
- **WHEN** se consulta `GET /api/delinquent-loans` con filtros opcionales (`minDaysOverdue`, `collectionStatus`)
- **THEN** el sistema retorna la lista de contratos en estado `Delinquent` que cumplen los filtros

#### Scenario: Consulta de préstamos en default
- **WHEN** se consulta `GET /api/loans/defaulted` con filtros opcionales de fecha
- **THEN** el sistema retorna la lista de contratos en estado `Default`

#### Scenario: Consulta de préstamos cancelados
- **WHEN** se consulta `GET /api/loans/paid-off` con filtros opcionales de fecha y `earlyPayoffOnly`
- **THEN** el sistema retorna la lista de contratos en estado `PaidOff`

#### Scenario: Consulta de historial de reestructuraciones
- **WHEN** se consulta `GET /api/loans/{id}/restructure-history`
- **THEN** el sistema retorna la lista de reestructuraciones aplicadas al contrato
- **WHEN** el ID no existe
- **THEN** el sistema retorna 404

---

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

---

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
