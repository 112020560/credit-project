## Context

El `LoanContractAggregate` es el núcleo del sistema de préstamos a plazo fijo. La funcionalidad documentada en este change ya está implementada y en producción. Este documento describe las decisiones de diseño existentes, no propone cambios.

Stack: .NET 9, Dapper + Npgsql (PostgreSQL), MediatR, FluentValidation, MassTransit.

## Goals / Non-Goals

**Goals:**
- Documentar las decisiones arquitectónicas del Loan Contract como referencia para el equipo
- Servir de base para futuros cambios sobre esta funcionalidad

**Non-Goals:**
- Proponer cambios de implementación
- Documentar el crédito revolvente (`RevolvingCreditAggregate`)
- Describir la sincronización de clientes desde CRM

## Decisions

### Event Sourcing como fuente de verdad
El estado del contrato se reconstruye exclusivamente desde los eventos persisted en `PostgresEventStore`. El `LoanContractState` es un record inmutable actualizado por la función pura `ApplyEvent`. Esto permite rehidratación desde snapshot + delta o desde el historial completo.

**Alternativa descartada**: persistencia directa del estado (CRUD) — descartada por la necesidad de auditoría completa y trazabilidad de transiciones.

### Separación Write/Read mediante proyecciones
Las escrituras van al event store; las lecturas van a read models proyectados en `PostgresProjectionStore` (Dapper). El `ProjectionEngine` aplica todos los `IProjection` registrados tras persistir cada batch de eventos.

**Consistencia**: eventual — los read models pueden quedar temporalmente desactualizados si falla una proyección. Los eventos son la fuente de verdad y las proyecciones son reconstruibles.

### Motor de reglas como servicio de dominio
`ContractEngine` ejecuta reglas en orden de prioridad. Las reglas `IHardStopRule` abortan la evaluación inmediatamente. Cada regla contribuye opcionalmente con un ajuste de tasa acumulado sobre una base de 8%.

**Extensibilidad**: agregar una regla requiere implementar `IContractRule`, registrarla en DI y definir su `Priority`. No requiere cambios en `ContractEngine`.

### Workers como background services
Los procesos periódicos (acumulación de interés, detección de mora) son `IHostedService`. No son eventos de dominio externos; invocan directamente métodos del agregado y persisten eventos resultantes.

## Risks / Trade-offs

- **Consistencia eventual en proyecciones** → Las lecturas pueden no reflejar el último estado si falló la proyección. Mitigación: el log advierte el fallo pero no revierte la escritura; las proyecciones son reconstruibles en cualquier momento.
- **Sin rollback transaccional entre event store y projection store** → Si la proyección falla, el evento ya está guardado. Mitigación: idempotencia en proyectores mediante upsert.
- **Tasa base hardcodeada en `ContractEngine` (8%)** → Cambiar la tasa base requiere modificar código. No es configurable por entorno ni por producto.
- **Cuota estimada en DTI usa tasa fija 12%** → La cuota estimada para calcular el DTI no usa la tasa real aprobada sino una referencial fija. Puede diferir del pago real.
