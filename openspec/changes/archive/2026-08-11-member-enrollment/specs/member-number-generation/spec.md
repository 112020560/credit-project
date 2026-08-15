## ADDED Requirements

### Requirement: Generación de número de socio configurable
El sistema SHALL generar números de socio únicos en formato configurable. El formato por defecto SHALL ser `{Prefix}-{año}-{secuencial con padding}` (ej. `CM-2026-00143`). Los parámetros `Prefix` y `SequentialDigits` MUST ser configurables via `appsettings.json`. El secuencial SHALL reiniciarse por año y MUST ser atómico (sin condiciones de carrera bajo concurrencia).

#### Scenario: Generación con formato por defecto
- **WHEN** `MemberNumberFormat:Prefix = "CM"` y `MemberNumberFormat:SequentialDigits = 5` y es el año 2026
- **THEN** el primer número generado es `CM-2026-00001`, el segundo `CM-2026-00002`, y así sucesivamente

#### Scenario: Generación con prefijo personalizado
- **WHEN** `MemberNumberFormat:Prefix = "COOPEMEP"` y `MemberNumberFormat:SequentialDigits = 6`
- **THEN** el primer número generado es `COOPEMEP-2026-000001`

#### Scenario: Reinicio anual del secuencial
- **WHEN** es el último día del año y se genera el número `CM-2026-00999`, y al día siguiente (año siguiente) se genera uno nuevo
- **THEN** el nuevo número es `CM-2027-00001`

#### Scenario: Atomicidad bajo concurrencia
- **WHEN** dos solicitudes de enrolamiento se procesan simultáneamente
- **THEN** cada una recibe un número de socio distinto, sin duplicados

### Requirement: Persistencia de secuencias por año
El sistema SHALL mantener una tabla `member_number_sequences` con la secuencia actual por año. La operación de incremento MUST ser atómica usando `INSERT ... ON CONFLICT DO UPDATE ... RETURNING last_value`.

#### Scenario: Primera generación del año
- **WHEN** no existe fila para el año actual en `member_number_sequences`
- **THEN** se inserta una fila con `last_value = 1` y se retorna `1`

#### Scenario: Generaciones subsiguientes
- **WHEN** ya existe una fila para el año actual con `last_value = N`
- **THEN** se actualiza a `N + 1` y se retorna `N + 1`
