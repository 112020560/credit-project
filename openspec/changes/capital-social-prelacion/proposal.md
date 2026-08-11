## Why

En el modelo cooperativo costarricense, cada pago de un préstamo incluye un aporte al capital social del socio — su cuota de propiedad en la cooperativa. Este concepto es inexistente en el sistema actual: los pagos no registran ni acumulan capital social, los comprobantes no lo muestran, y la prelación de pagos está quemada en código sin posibilidad de adaptarse a la política interna de cada institución. Esto impide que el sistema sea usable por cualquier cooperativa regulada en Costa Rica.

## What Changes

- **NUEVO** Motor de prelación de pagos configurable por producto crediticio: orden de aplicación de fondos definido por política institucional, no por código.
- **NUEVO** Configuración de capital social por producto: tipo de cálculo (monto fijo, % de cuota, % de capital amortizado, % del monto original) y modo de cobro (incluido en cuota o cobro separado).
- **NUEVO** Acumulación de capital social por socio: saldo creciente que refleja la participación del socio como propietario de la cooperativa.
- **MODIFICADO** `LoanContractAggregate.ApplyPayment()`: usa el waterfall del producto en lugar de la prelación hardcodeada. Recibe `PaymentWaterfall` como parámetro.
- **MODIFICADO** Evento `PaymentApplied`: agrega campo `SocialCapitalContributed`.
- **MODIFICADO** `LoanContractState`: agrega `TotalSocialCapitalContributed` acumulado.
- **MODIFICADO** Comprobante de pago PDF: muestra línea de capital social cuando es mayor a cero.
- **MODIFICADO** **BREAKING** `ApplyPaymentCommandHandler`: debe cargar el producto del préstamo y calcular el capital social antes de llamar al agregado.

## Capabilities

### New Capabilities

- `payment-waterfall`: Motor de prelación de pagos configurable por producto. Define el orden en que se aplican los fondos recibidos: seguros, comisiones, interés moratorio, interés corriente, capital social, principal.
- `social-capital`: Aporte al capital social cooperativo por pago de préstamo. Incluye configuración de cálculo, modo de cobro, acumulación por socio y visibilidad en comprobantes.

### Modified Capabilities

- `loan-contract`: El método `ApplyPayment` deja de tener prelación interna hardcodeada; ahora recibe el waterfall como parámetro. El evento `PaymentApplied` agrega `SocialCapitalContributed`. El estado agrega `TotalSocialCapitalContributed`.
- `cooperative-member`: El perfil del socio agrega `SocialCapitalBalance` acumulado a través de todos sus préstamos.

## Impact

- **Dominio**: nuevos enums, records y value objects para waterfall y capital social. Firma de `ApplyPayment()` cambia (breaking).
- **Application**: `ApplyPaymentCommandHandler` requiere acceso al repositorio de productos crediticios.
- **Infraestructura**: nueva columna en `credit_products` (waterfall_config, social_capital_config JSONB), nueva columna en `cooperative_members` (social_capital_balance), nueva columna en `rm_payment_history` (social_capital_contributed).
- **API**: nuevo endpoint `GET /members/{id}/social-capital`.
- **Documentos**: template `payment-receipt.sbn` agrega línea condicional de capital social.
- **Tests**: todos los tests que usan `ApplyPayment()` directamente deben actualizarse para pasar un waterfall.
