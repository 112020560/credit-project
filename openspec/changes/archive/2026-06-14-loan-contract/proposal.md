## Why

El sistema CreditSystem gestiona préstamos a plazo fijo mediante un agregado de dominio con Event Sourcing. Esta especificación documenta la funcionalidad existente del Loan Contract para establecer un contrato formal de requisitos y reglas de negocio que sirva de referencia para el equipo de desarrollo y validación.

## What Changes

- Documentación formal del ciclo de vida completo de `LoanContractAggregate`
- Especificación del motor de evaluación crediticia (`ContractEngine`) con sus 5 reglas priorizadas
- Definición de criterios de aceptación para cada operación del préstamo
- Captura de reglas de negocio implícitas en el código (umbrales, tasas, transiciones de estado)

## Capabilities

### New Capabilities

- `loan-contract`: Ciclo de vida completo de un préstamo a plazo fijo: evaluación crediticia automática, creación, desembolso, gestión de pagos, acumulación de interés diario, detección de mora, default, reestructuración, cancelación anticipada y consultas de estado.

### Modified Capabilities

## Impact

- No hay cambios de código — es documentación de funcionalidad existente
- Afecta: `CreditSystem.Domain` (agregado, reglas, value objects), `CreditSystem.Application` (comandos y queries), `CreditSystem.Api` (endpoints `/api/loans`, `/api/delinquent-loans`)
