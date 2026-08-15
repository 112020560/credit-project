## Why

El sistema no tiene capacidad de generar documentos formales para los clientes. Operaciones como entregar un comprobante de pago, una carta de saldo o una tabla de amortización requieren actualmente procesos manuales fuera del sistema, lo que introduce demora, errores y falta de trazabilidad.

## What Changes

- Nuevo motor de templates basado en **Scriban** (archivos `.sbn`) que permite parametrizar el contenido de cualquier documento sin recompilar
- Generación de **PDF** via QuestPDF (fluent API, sin dependencias externas nativas)
- Exportación a **Excel** via ClosedXML para tabla de amortización
- Tres nuevos endpoints de descarga de documentos bajo `/api/loans/{id}/documents/`
- Data contracts por tipo de documento (POCOs) que desacoplan la plantilla de la fuente de datos
- Queries de aplicación que arman cada data contract desde los read models existentes (`rm_payment_history`, `rm_loan_summaries`) y el aggregate rehydratado

## Capabilities

### New Capabilities

- `document-engine`: Motor central de generación de documentos — ScribanTemplateEngine, QuestPdfRenderer, ExcelExporter, interfaces IDocumentGenerator y data contracts
- `payment-receipt`: Comprobante de pago por transacción — desglose de capital, intereses y comisiones aplicadas
- `balance-letter`: Carta de saldo del préstamo — saldo actual, tasa vigente, próximo pago y fecha de vencimiento
- `amortization-table`: Tabla de amortización exportable — schedule completo en PDF o Excel

### Modified Capabilities

- `loan-contract`: Se agregan tres nuevos endpoints de descarga de documentos asociados al contrato

## Impact

- **Nuevos paquetes NuGet**: `QuestPDF`, `Scriban`, `ClosedXML` en `CreditSystem.Infrastructure`
- **Nuevas capas**: `Infrastructure/Documents/` (engine + renderers + templates), `Application/Queries/Documents/`, `Domain/Abstractions/Documents/`, `Domain/Models/Documents/`
- **API**: nuevo grupo de endpoints `DocumentEndpoints.cs` registrado en `Program.cs`
- Sin cambios a esquema de base de datos — usa read models y event store existentes
- Sin cambios a lógica de dominio ni agregados existentes
