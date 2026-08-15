## ADDED Requirements

### Requirement: Prelación de pagos configurable por producto
El sistema SHALL soportar una prelación de pagos (`PaymentWaterfall`) configurable por producto crediticio. La prelación define el orden en que se aplican los fondos recibidos de un pago a los distintos componentes de la deuda.

Los componentes válidos son: `Insurance`, `Fees`, `PenaltyInterest`, `RegularInterest`, `SocialCapital`, `Principal`.

Cada componente tiene una prioridad numérica (1 = primero). No puede haber dos componentes con la misma prioridad en un mismo waterfall.

#### Scenario: Pago aplicado según waterfall del producto
- **WHEN** se aplica un pago a un préstamo cuyo producto tiene waterfall `[Fees(1), PenaltyInterest(2), RegularInterest(3), SocialCapital(4), Principal(5)]`
- **THEN** los fondos se aplican en ese orden exacto: primero a comisiones, luego interés moratorio, luego interés corriente, luego capital social, y el remanente a principal

#### Scenario: Fondos insuficientes para cubrir todos los componentes
- **WHEN** el monto del pago no alcanza para cubrir todos los componentes en la prelación
- **THEN** se aplica lo que hay en orden de prioridad y los componentes de menor prioridad quedan con ₡0 aplicado

#### Scenario: Waterfall por defecto para préstamos sin producto
- **WHEN** se aplica un pago a un préstamo que no tiene producto crediticio asignado
- **THEN** el sistema usa el waterfall por defecto: `[Fees(1), PenaltyInterest(2), RegularInterest(3), Principal(4)]` sin capital social

### Requirement: Waterfall persistido en el producto crediticio
El sistema SHALL almacenar el waterfall de cada producto crediticio en la base de datos como configuración JSONB. El waterfall es inmutable una vez que existen préstamos activos del producto.

#### Scenario: Producto creado con waterfall personalizado
- **WHEN** se crea un producto crediticio con una lista ordenada de componentes de pago
- **THEN** el waterfall queda almacenado y es retornado al consultar el producto

#### Scenario: Consulta del waterfall vigente de un producto
- **WHEN** el handler de pago carga el producto del préstamo
- **THEN** obtiene el waterfall serializado y lo convierte a `PaymentWaterfall` para pasarlo al agregado
