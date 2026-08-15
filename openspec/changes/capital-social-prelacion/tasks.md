## 1. Dominio — Enums y tipos base

- [x] 1.1 Crear enum `PaymentComponent` en `CreditSystem.Domain/Enums/`: `Insurance = 1`, `Fees = 2`, `PenaltyInterest = 3`, `RegularInterest = 4`, `SocialCapital = 5`, `Principal = 6`
- [x] 1.2 Crear enum `SocialCapitalCalculationType` en `CreditSystem.Domain/Enums/`: `FixedAmount = 0`, `PercentageOfPayment = 1`, `PercentageOfPrincipalPaid = 2`, `PercentageOfOriginalAmount = 3`
- [x] 1.3 Crear enum `SocialCapitalCollectionMode` en `CreditSystem.Domain/Enums/`: `IncludedInPayment = 0`, `SeparateCollection = 1`

## 2. Dominio — Value Objects del motor de prelación

- [x] 2.1 Crear record `PaymentWaterfallStep` en `CreditSystem.Domain/ValueObjects/`: `Priority (int)`, `Component (PaymentComponent)`; inmutable, comparable por Priority
- [x] 2.2 Crear record `SocialCapitalConfig` en `CreditSystem.Domain/ValueObjects/`: `CalculationType`, `Value (decimal)`, `CollectionMode`; con método `Calculate(Money paymentAmount, Money principalPaid, Money originalAmount, string currency) → Money`
- [x] 2.3 Crear record `PaymentWaterfall` en `CreditSystem.Domain/ValueObjects/`: `IReadOnlyList<PaymentWaterfallStep> Steps`; con propiedad estática `Default` (Fees→PenaltyInterest→RegularInterest→Principal); con método `Apply(Money amount, LoanPaymentContext context) → PaymentDistribution`
- [x] 2.4 Crear record `PaymentDistribution` en `CreditSystem.Domain/ValueObjects/`: `Money InsurancePaid`, `Money FeesPaid`, `Money PenaltyInterestPaid`, `Money InterestPaid`, `Money SocialCapitalPaid`, `Money PrincipalPaid`; con propiedad `Total` (suma de todos los campos)

## 3. Dominio — Cambios en LoanContract

- [x] 3.1 Agregar campo `SocialCapitalContributed (Money)` al evento `PaymentApplied` en `CreditSystem.Domain/Aggregates/LoanContract/Events/PaymentApplied.cs`
- [x] 3.2 Agregar campo `TotalSocialCapitalContributed (Money)` a `LoanContractState`; inicializar en `Money.Zero()` en `LoanContractState.Initial`
- [x] 3.3 Actualizar `ApplyEvent` en `LoanContractAggregate` para el caso `PaymentApplied`: acumular `TotalSocialCapitalContributed += event.SocialCapitalContributed`
- [x] 3.4 Modificar la firma de `LoanContractAggregate.ApplyPayment()` para recibir `PaymentWaterfall waterfall` y `Money socialCapitalContributed` como parámetros adicionales
- [x] 3.5 Reemplazar la prelación hardcodeada en `ApplyPayment()` por llamada a `waterfall.Apply(...)` usando los valores del estado actual del préstamo (`TotalFees`, `AccruedPenaltyInterest`, `AccruedInterest`, `CurrentBalance`)
- [x] 3.6 Incluir `SocialCapitalContributed` en el evento `PaymentApplied` emitido dentro de `ApplyPayment()`

## 4. Dominio — Cambios en CreditProduct

- [x] 4.1 Agregar propiedades `PaymentWaterfall Waterfall` y `SocialCapitalConfig? SocialCapitalConfig` a la entidad `CreditProduct` en `CreditSystem.Domain/Entities/CreditProduct.cs`
- [x] 4.2 Actualizar el constructor o factory de `CreditProduct` para aceptar waterfall y social capital config opcionales; si no se provee waterfall, usar `PaymentWaterfall.Default`

## 5. Infraestructura — Migración de base de datos

- [x] 5.1 Crear `Src/Core/CreditSystem.Infrastructure/Migrations/20260810_AddSocialCapitalAndWaterfall.sql` con:
  - `ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS waterfall_config JSONB`
  - `ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS social_capital_config JSONB`
  - `ALTER TABLE cooperative_members ADD COLUMN IF NOT EXISTS social_capital_balance DECIMAL(18,2) NOT NULL DEFAULT 0`
  - `ALTER TABLE rm_payment_history ADD COLUMN IF NOT EXISTS social_capital_contributed DECIMAL(18,2) NOT NULL DEFAULT 0`

## 6. Infraestructura — Repositorio de productos

- [x] 6.1 Actualizar `CreditProductRepository` (o el repositorio/query service de productos existente): al leer un producto desde BD, deserializar `waterfall_config` JSONB a `PaymentWaterfall` y `social_capital_config` JSONB a `SocialCapitalConfig?`
- [x] 6.2 Actualizar el método de escritura/creación de producto: serializar `Waterfall` y `SocialCapitalConfig` a JSONB al guardar

## 7. Infraestructura — Proyector de capital social del socio

- [x] 7.1 Actualizar `PaymentHistoryProjector` (o crear `SocialCapitalProjector` si el primero no escucha `PaymentApplied` de LoanContract): al recibir `PaymentApplied` con `SocialCapitalContributed.Amount > 0`, hacer `UPDATE cooperative_members SET social_capital_balance = social_capital_balance + @amount, updated_at = NOW() WHERE id = @memberId`
- [x] 7.2 Actualizar la proyección de `rm_payment_history` para incluir la columna `social_capital_contributed` al insertar registros de pago

## 8. Application — ApplyPaymentCommandHandler

- [x] 8.1 Inyectar `ICreditProductRepository` (o el servicio correspondiente) en `ApplyPaymentCommandHandler`
- [x] 8.2 En el handler, después de cargar el agregado: obtener el `ProductId` del préstamo desde su estado; si existe, cargar el producto y extraer su `Waterfall` y `SocialCapitalConfig`; si no existe, usar `PaymentWaterfall.Default` y `SocialCapitalConfig = null`
- [x] 8.3 Calcular `socialCapitalContributed` usando `SocialCapitalConfig?.Calculate(amount, principalScheduled, originalAmount, currency) ?? Money.Zero()`
- [x] 8.4 Ajustar la llamada a `aggregate.ApplyPayment(paymentId, amount, method, waterfall, socialCapitalContributed)`

## 9. API — Endpoint de capital social del socio

- [x] 9.1 Agregar endpoint `GET /members/{id}/social-capital` en `MemberEndpoints.cs`: consulta `cooperative_members.social_capital_balance` por id de socio; retorna `{ memberId, socialCapitalBalance, currency, lastUpdatedAt }` o 404 si no existe
- [x] 9.2 Actualizar el response de `GET /members/{id}` para incluir el campo `socialCapitalBalance` si está disponible

## 10. Documentos — Comprobante de pago

- [x] 10.1 Agregar campo `SocialCapitalContributed (Money?)` a `PaymentReceiptData` en `CreditSystem.Domain/Models/Documents/PaymentReceiptData.cs`
- [x] 10.2 Actualizar `GetPaymentReceiptQueryHandler` para mapear `social_capital_contributed` de `rm_payment_history` al campo `SocialCapitalContributed` del data contract
- [x] 10.3 Actualizar `payment-receipt.sbn`: agregar línea condicional `{{ if model.social_capital_contributed > 0 }} Aporte Capital Social: {{ model.social_capital_contributed | math.format "N2" }} {{ model.currency }} {{ end }}`

## 11. Tests

- [x] 11.1 Actualizar todos los tests existentes que llaman `aggregate.ApplyPayment()` directamente para pasar `PaymentWaterfall.Default` y `Money.Zero()` como nuevos parámetros
- [x] 11.2 Test unitario: `PaymentWaterfall` aplica fondos en el orden correcto para un pago que no cubre todos los componentes
- [x] 11.3 Test unitario: `SocialCapitalConfig.Calculate()` retorna el monto correcto para cada tipo (`FixedAmount`, `PercentageOfPayment`, `PercentageOfPrincipalPaid`, `PercentageOfOriginalAmount`)
- [x] 11.4 Test unitario: `ApplyPayment()` con `CollectionMode = IncludedInPayment` descuenta el capital social antes de la prelación
- [x] 11.5 Test unitario: `ApplyPayment()` con `CollectionMode = SeparateCollection` aplica el monto completo al préstamo y registra capital social informativamente
- [x] 11.6 Test unitario: `LoanContractState.TotalSocialCapitalContributed` acumula correctamente tras múltiples pagos
- [x] 11.7 Test unitario: `PaymentWaterfall.Default` produce el mismo resultado que la prelación legacy actual
