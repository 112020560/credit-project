## ADDED Requirements

### Requirement: Configuración de capital social por producto
El sistema SHALL permitir configurar en cada producto crediticio cómo se calcula el aporte al capital social cooperativo por cada pago realizado.

Los tipos de cálculo soportados son:
- `FixedAmount`: monto fijo por pago (ej. ₡ 500 siempre)
- `PercentageOfPayment`: porcentaje del monto total del pago
- `PercentageOfPrincipalPaid`: porcentaje del capital amortizado en esa cuota
- `PercentageOfOriginalAmount`: porcentaje del monto original del préstamo

Los modos de cobro son:
- `IncludedInPayment`: el capital social se descuenta del monto recibido antes de aplicar la prelación al préstamo
- `SeparateCollection`: el capital social se registra informativamente pero no altera el monto aplicado al préstamo

#### Scenario: Producto con capital social de monto fijo incluido en cuota
- **WHEN** el producto tiene `SocialCapitalConfig { CalculationType: FixedAmount, Value: 500, CollectionMode: IncludedInPayment }` y el socio paga ₡ 50,000
- **THEN** ₡ 500 se registran como `SocialCapitalContributed` y los ₡ 49,500 restantes se aplican al préstamo según la prelación

#### Scenario: Producto con capital social como porcentaje del pago, cobro separado
- **WHEN** el producto tiene `SocialCapitalConfig { CalculationType: PercentageOfPayment, Value: 2, CollectionMode: SeparateCollection }` y el socio paga ₡ 50,000
- **THEN** ₡ 1,000 se registran como `SocialCapitalContributed` de forma informativa y los ₡ 50,000 completos se aplican al préstamo según la prelación

#### Scenario: Producto sin configuración de capital social
- **WHEN** un producto no tiene `SocialCapitalConfig`
- **THEN** el campo `SocialCapitalContributed` del evento `PaymentApplied` es ₡ 0 y el saldo del socio no se modifica

### Requirement: Registro del capital social en el evento de pago
El sistema SHALL registrar el monto de capital social aportado en cada evento `PaymentApplied` mediante el campo `SocialCapitalContributed`.

#### Scenario: Capital social mayor a cero en el evento
- **WHEN** se aplica un pago con capital social calculado mayor a ₡ 0
- **THEN** el evento `PaymentApplied` contiene `SocialCapitalContributed` con el monto exacto calculado

#### Scenario: Capital social cero cuando no aplica
- **WHEN** el producto no tiene capital social o el monto calculado es ₡ 0
- **THEN** el evento `PaymentApplied` contiene `SocialCapitalContributed = Money.Zero()`

### Requirement: Acumulación de capital social en el perfil del socio
El sistema SHALL mantener un saldo acumulado de capital social por socio (`social_capital_balance`) que crece con cada pago que genere un aporte mayor a cero, a través de todos sus préstamos activos o cerrados.

#### Scenario: Saldo de capital social se incrementa con cada pago
- **WHEN** se procesa un evento `PaymentApplied` con `SocialCapitalContributed > 0`
- **THEN** el `social_capital_balance` del socio aumenta en exactamente ese monto

#### Scenario: Saldo de capital social consultable por API
- **WHEN** se consulta `GET /members/{id}/social-capital`
- **THEN** el sistema retorna el saldo acumulado total, la moneda y la fecha de última actualización

#### Scenario: Saldo de capital social no decrece por pagos normales
- **WHEN** se aplica un pago que no incluye capital social (producto sin config o monto ₡ 0)
- **THEN** el `social_capital_balance` del socio no cambia

### Requirement: Visibilidad del capital social en el comprobante de pago
El sistema SHALL mostrar el aporte al capital social en el comprobante de pago PDF cuando el monto sea mayor a cero.

#### Scenario: Comprobante muestra línea de capital social
- **WHEN** se genera el comprobante de pago para un pago con `SocialCapitalContributed > 0`
- **THEN** el PDF incluye una línea "Aporte Capital Social: ₡ X,XXX.XX" en el desglose del pago

#### Scenario: Comprobante omite línea de capital social cuando es cero
- **WHEN** se genera el comprobante de pago para un pago con `SocialCapitalContributed = 0`
- **THEN** el PDF no incluye línea de capital social en el desglose
