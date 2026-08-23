## Context

El contrato `CustomerCreated` ya incluye un campo `Metadata: IDictionary<string, object>?` que siempre se enviaba como `null`. Servicios downstream (motor de riesgo, retail) consumen este evento y necesitan los datos financieros del cliente desde el primer evento para inicializar sus modelos sin tener que hacer un query extra al CRM.

Adicionalmente, el score crediticio y los ingresos son datos vivos: pueden cambiar después de la creación. El CRM necesita un mecanismo de actualización parcial que garantice que solo los campos explícitamente enviados se sobrescriben.

Estado actual:
- `Customer` no tiene columnas financieras en BD.
- `CreateCustomerCommand` fija `Metadata = null`.
- `CustomerUpdatedContract` ya tiene `Changes: IDictionary<string, object>` pero no se usa para financials.
- No existe endpoint de actualización financiera parcial.

## Goals / Non-Goals

**Goals:**
- Agregar `CreditScore`, `MonthlyIncome`, `MonthlyDebt` como columnas nullable en la tabla `Customers`.
- Poblar `Metadata` en `CustomerCreated` cuando el DTO los trae.
- Endpoint `PATCH /api/v1/customers/{id}/financials` para actualización parcial.
- Semántica COALESCE: campos no enviados conservan el valor actual en BD.
- Publicar `CustomerUpdated` con `Changes` conteniendo solo los campos del request.

**Non-Goals:**
- Historial de cambios de financials (audit log).
- Validación de rangos de CreditScore contra bureaus externos.
- Autenticación/autorización diferenciada para el endpoint de financials.

## Decisions

### Decisión 1: Columnas en tabla `Customers` vs tabla separada `CustomerFinancialProfiles`

**Elegido**: columnas nullable en `Customers`.

**Alternativa**: tabla separada con relación 1-a-1.

**Rationale**: Los datos financieros son escasos (3 campos) y se leen siempre junto al cliente. Una tabla separada añadiría un JOIN en cada lectura sin beneficio real. Si en el futuro crecen significativamente, se puede migrar. YAGNI aplica aquí.

### Decisión 2: COALESCE en EF Core vs raw SQL

**Elegido**: EF Core con lógica condicional en el repositorio — solo se asignan las propiedades que llegaron en `Changes`, dejando las otras sin modificar. Como el DbContext tiene `NoTracking` global, el `UpdateAsync` carga la entidad con `AsTracking()`, aplica solo los campos presentes, y SaveChanges genera un UPDATE solo con las columnas modificadas (change tracking de EF).

**Alternativa**: SQL crudo con `UPDATE ... SET CreditScore = COALESCE(@new, CreditScore)`.

**Rationale**: EF Core con tracking produce el mismo resultado COALESCE y mantiene consistencia con el patrón ya establecido en el proyecto (ver `bugs.md` BUG-003). El SQL crudo rompería la abstracción del repositorio.

### Decisión 3: `IDictionary<string, object>` vs campos tipados en el contrato MQ

**Elegido**: mantener `IDictionary<string, object>` para `Metadata` y `Changes` (ya definido en los contratos existentes).

**Rationale**: El contrato `CreateCustomerContract` ya tiene `Metadata: IDictionary<string, object>?` y `CustomerUpdatedContract` ya tiene `Changes: IDictionary<string, object>`. Cambiar a campos tipados implicaría un breaking change en el contrato MQ y en todos los consumers. La clave de cada campo (`"CreditScore"`, `"MonthlyIncome"`, `"MonthlyDebt"`) es suficientemente estable para ser una convención de string.

### Decisión 4: PATCH semántico vs PUT para actualización financiera

**Elegido**: `PATCH /api/v1/customers/{id}/financials`.

**Rationale**: Solo se actualizan los campos presentes en el body — semántica PATCH. Un PUT requeriría enviar todos los campos siempre, lo que rompe el requisito de COALESCE.

## Risks / Trade-offs

- **[Riesgo] Campos financieros en `Metadata` son strings, no tipados** → Los consumers deben deserializar `decimal`/`int` desde `object`. Mitigación: documentar las claves y tipos esperados en el contrato. Alternativa futura: migrar a campos tipados con versión de contrato.
- **[Riesgo] Race condition en actualizaciones concurrentes** → Si dos eventos de `Changes` llegan simultáneamente, el segundo puede sobrescribir al primero en campos comunes. Mitigación: fuera de scope por ahora; se puede agregar `Version` optimista en iteración futura.
- **[Trade-off] Columnas nullable en Customer** → La entidad Customer crece. Aceptable dado que son 3 campos y es el modelo dominante.

## Migration Plan

1. Crear migration EF Core: `AddFinancialColumnsToCustomers` — agrega `CreditScore NUMERIC`, `MonthlyIncome NUMERIC`, `MonthlyDebt NUMERIC` nullable.
2. Desplegar migration con `dotnet ef database update`.
3. No requiere backfill — valores existentes quedan `NULL`, lo que es comportamiento correcto.
4. Rollback: `dotnet ef database update <MigrationAnterior>` elimina las columnas (son nullable, sin datos críticos en ellas inicialmente).

## Open Questions

- ¿`CreditScore` debe ser `int` o `decimal`? Por ahora `decimal` para aceptar scores con decimales (e.g., modelos europeos). Si el score es siempre entero, cambiar a `int` en la migration.
- ¿El endpoint `PATCH /financials` requiere algún rol específico o cualquier usuario autenticado puede llamarlo?
