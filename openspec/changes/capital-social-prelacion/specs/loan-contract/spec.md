## MODIFIED Requirements

### Requirement: Aplicación de pago con prelación configurable
El método `ApplyPayment` del `LoanContractAggregate` SHALL recibir un `PaymentWaterfall` como parámetro y aplicar los fondos del pago en el orden definido por ese waterfall, en lugar de usar una prelación hardcodeada.

El agregado NO SHALL conocer el repositorio de productos ni calcular el waterfall internamente. Esa responsabilidad pertenece al handler de Application.

#### Scenario: Pago aplicado con waterfall explícito
- **WHEN** se llama `aggregate.ApplyPayment(paymentId, amount, method, waterfall)` con un waterfall personalizado
- **THEN** los fondos se distribuyen en el orden indicado por el waterfall

#### Scenario: Pago aplicado con waterfall por defecto
- **WHEN** se llama `aggregate.ApplyPayment(paymentId, amount, method, PaymentWaterfall.Default)`
- **THEN** el comportamiento es idéntico al actual: comisiones → interés moratorio → interés corriente → principal

#### Scenario: Capital social con modo IncludedInPayment
- **WHEN** el waterfall incluye el componente `SocialCapital` y el `SocialCapitalContributed` calculado es mayor a cero con `CollectionMode = IncludedInPayment`
- **THEN** el monto del capital social se deduce del total antes de iniciar la prelación del préstamo, y `SocialCapitalContributed` queda registrado en el evento

#### Scenario: Capital social con modo SeparateCollection
- **WHEN** el waterfall incluye el componente `SocialCapital` y `CollectionMode = SeparateCollection`
- **THEN** el monto completo del pago se aplica a los componentes del préstamo y `SocialCapitalContributed` queda registrado en el evento sin afectar la distribución

### Requirement: Estado del préstamo acumula capital social total
El `LoanContractState` SHALL incluir el campo `TotalSocialCapitalContributed` que acumula el monto total aportado al capital social a través de todos los pagos de ese préstamo.

#### Scenario: TotalSocialCapitalContributed crece con cada pago
- **WHEN** se procesa un evento `PaymentApplied` con `SocialCapitalContributed > 0`
- **THEN** `LoanContractState.TotalSocialCapitalContributed` aumenta en ese monto

#### Scenario: TotalSocialCapitalContributed inicia en cero
- **WHEN** se crea un nuevo préstamo
- **THEN** `LoanContractState.TotalSocialCapitalContributed` es `Money.Zero()`

### Requirement: Evento PaymentApplied incluye capital social
El evento `PaymentApplied` SHALL incluir el campo `SocialCapitalContributed` de tipo `Money` en todos los casos, siendo `Money.Zero()` cuando no aplica capital social.

#### Scenario: Evento contiene campo SocialCapitalContributed
- **WHEN** se emite el evento `PaymentApplied`
- **THEN** el evento siempre incluye `SocialCapitalContributed`, ya sea con monto positivo o cero
