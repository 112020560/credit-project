## ADDED Requirements

### Requirement: CreditProduct encapsula los parámetros de un producto crediticio
El sistema SHALL disponer de una entidad `CreditProduct` en el domain layer que agrupa todos los parámetros que definen las condiciones bajo las cuales se puede otorgar un préstamo de ese tipo.

Campos:
- `Id` (`Guid`): identificador único del producto
- `Name` (`string`): nombre del producto (ej: "Préstamo Personal", "Microcrédito")
- `Limits` (`ProductLimits`): value object con `MinAmount`, `MaxAmount`, `MinTermMonths`, `MaxTermMonths`
- `Rates` (`ProductRates`): value object con `BaseInterestRate` (`decimal?`, puede ser null si usa la de la política), `MaxLtv` (`decimal?`, solo aplica si requiere colateral)
- `DefaultAmortizationMethod` (`AmortizationMethod`): método de amortización por defecto para este producto
- `RequiresCollateral` (`bool`): indica si el producto exige colateral
- `Status` (`ProductStatus` enum): `Active` | `Inactive`

`ProductLimits` y `ProductRates` son value objects inmutables con validación en construcción.

`ProductStatus` enum: `Active` = 1, `Inactive` = 0.

#### Scenario: Creación de producto con parámetros válidos
- **WHEN** se construye un `CreditProduct` con monto mín < monto máx, plazo mín < plazo máx y tasa > 0
- **THEN** el objeto se crea sin excepciones y su `Status` inicial es `Active`

#### Scenario: ProductLimits rechaza parámetros incoherentes
- **WHEN** se construye `ProductLimits` con `MinAmount >= MaxAmount` o `MinTermMonths >= MaxTermMonths`
- **THEN** el constructor lanza `ArgumentException`

#### Scenario: ProductRates con tasa null usa la tasa de la política global
- **WHEN** `ProductRates.BaseInterestRate` es null
- **THEN** el sistema usa `UnderwritingPolicy.BaseInterestRate` como tasa base para contratos de ese producto

---

### Requirement: ICreditProductRepository permite leer y escribir el catálogo
El sistema SHALL exponer la interfaz `ICreditProductRepository` en el domain layer con operaciones para administrar y consultar el catálogo de productos.

Métodos:
- `GetByIdAsync(Guid id)` → `CreditProduct?`
- `GetAllActiveAsync()` → `IEnumerable<CreditProduct>`
- `GetAllAsync()` → `IEnumerable<CreditProduct>`
- `InsertAsync(CreditProduct product)` → `void`
- `UpdateStatusAsync(Guid id, ProductStatus status)` → `void`

#### Scenario: Lectura de producto activo por ID
- **WHEN** se llama `GetByIdAsync` con un ID que corresponde a un `CreditProduct` activo
- **THEN** el sistema retorna el `CreditProduct` con todos sus campos

#### Scenario: Producto inexistente retorna null
- **WHEN** se llama `GetByIdAsync` con un ID que no existe en la tabla
- **THEN** el sistema retorna `null`

#### Scenario: Listar solo productos activos
- **WHEN** se llama `GetAllActiveAsync`
- **THEN** el sistema retorna únicamente los productos con `Status = Active`

---

### Requirement: Tabla credit_products con seed de 5 productos de cooperativa
El sistema SHALL tener una tabla `credit_products` en PostgreSQL. La migración incluye el INSERT del seed con 5 productos típicos de una cooperativa costarricense.

Productos del seed:

| Nombre | Tasa | Plazo máx | Monto máx | Colateral |
|---|---|---|---|---|
| Préstamo Personal | 14% | 60 meses | 5,000,000 CRC | No |
| Préstamo de Consumo | 18% | 36 meses | 2,000,000 CRC | No |
| Préstamo Vehicular | 12% | 84 meses | 15,000,000 CRC | Sí (LTV 90%) |
| Préstamo Hipotecario | 9% | 300 meses | 100,000,000 CRC | Sí (LTV 80%) |
| Microcrédito | 20% | 24 meses | 500,000 CRC | No |

#### Scenario: Migración aplicada exitosamente
- **WHEN** se ejecuta el script `20260808_AddCreditProductsTable.sql`
- **THEN** existe la tabla `credit_products` con columnas `id`, `name`, `min_amount`, `max_amount`, `min_term_months`, `max_term_months`, `base_interest_rate`, `max_ltv`, `default_amortization_method`, `requires_collateral`, `status`, `created_at`
- **THEN** existen 5 registros seed con los valores de la tabla anterior

---

### Requirement: Endpoints de administración del catálogo de productos
El sistema SHALL exponer endpoints REST para crear, consultar y actualizar el estado de los productos del catálogo.

Endpoints:
- `POST /api/products` — crear un nuevo producto
- `GET /api/products` — listar todos los productos
- `GET /api/products/{id}` — obtener un producto por ID
- `PUT /api/products/{id}/status` — actualizar estado (`Active` / `Inactive`)

#### Scenario: Crear producto exitosamente
- **WHEN** se envía `POST /api/products` con todos los campos requeridos válidos
- **THEN** el sistema persiste el nuevo `CreditProduct` y retorna 201 con el ID generado

#### Scenario: Crear producto con parámetros inválidos
- **WHEN** se envía `POST /api/products` con `MinAmount >= MaxAmount` u otros parámetros incoherentes
- **THEN** el sistema retorna 400 con los errores de validación

#### Scenario: Obtener producto por ID
- **WHEN** se consulta `GET /api/products/{id}` con un ID existente
- **THEN** el sistema retorna 200 con los detalles del producto
- **WHEN** el ID no existe
- **THEN** el sistema retorna 404

#### Scenario: Desactivar producto
- **WHEN** se envía `PUT /api/products/{id}/status` con `{ "status": "Inactive" }`
- **THEN** el sistema actualiza el estado y retorna 204
- **THEN** el producto deja de aparecer en `GET /api/products` (que solo lista activos)
