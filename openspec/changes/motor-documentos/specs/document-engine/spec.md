## ADDED Requirements

### Requirement: Motor de templates Scriban
El sistema SHALL disponer de un motor de templates basado en Scriban que renderice archivos `.sbn` almacenados en `Infrastructure/Documents/Templates/` substituyendo variables con datos de negocio.

El motor SHALL:
- Leer la plantilla desde disco por nombre de archivo
- Exponer los datos del data contract como variables Scriban accesibles en la plantilla
- Retornar el contenido renderizado como string

#### Scenario: Renderizado exitoso de template
- **WHEN** se solicita renderizar `payment-receipt.sbn` con datos válidos de pago
- **THEN** el motor retorna el contenido con todas las variables sustituidas correctamente

#### Scenario: Template no encontrado
- **WHEN** se solicita un template cuyo archivo `.sbn` no existe en disco
- **THEN** el motor lanza una excepción con mensaje indicando el archivo faltante

#### Scenario: Variable no definida en datos
- **WHEN** la plantilla referencia una variable no presente en el data contract
- **THEN** Scriban renderiza la variable como cadena vacía sin lanzar excepción

---

### Requirement: Renderizador de PDF con QuestPDF
El sistema SHALL convertir el contenido estructurado de un documento a bytes PDF usando QuestPDF, sin dependencias de runtime externo (Chrome, wkhtmltopdf, etc.).

El renderer SHALL producir documentos con:
- Encabezado con nombre de la cooperativa y tipo de documento
- Cuerpo con el contenido del documento
- Pie de página con fecha de generación y número de página

#### Scenario: Generación de PDF exitosa
- **WHEN** se llama `RenderPdfAsync` con datos válidos de documento
- **THEN** retorna un `byte[]` no vacío con Content-Type `application/pdf`

#### Scenario: PDF con múltiples páginas
- **WHEN** el contenido (ej. tabla de amortización larga) excede una página
- **THEN** QuestPDF pagina automáticamente y el pie de página muestra `Página N de M`

---

### Requirement: Exportador Excel con ClosedXML
El sistema SHALL exportar tablas de datos a formato `.xlsx` usando ClosedXML para documentos que requieran formato tabular editable.

#### Scenario: Generación de Excel exitosa
- **WHEN** se llama `ExportExcelAsync` con una lista de filas de amortización
- **THEN** retorna un `byte[]` con Content-Type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

#### Scenario: Encabezados de columna presentes
- **WHEN** se genera el Excel de tabla de amortización
- **THEN** la primera fila contiene los headers: Cuota, Fecha, Pago, Interés, Capital, Saldo

---

### Requirement: Data contracts de documentos
El sistema SHALL definir un POCO por tipo de documento que represente todos los datos necesarios para renderizar la plantilla correspondiente.

Data contracts requeridos:
- `PaymentReceiptData`: datos del pago, préstamo y cliente para comprobante
- `BalanceLetterData`: datos de saldo actual, tasa, próximo pago y cliente para carta de saldo
- `AmortizationTableData`: schedule completo con metadatos del préstamo para tabla de amortización

#### Scenario: Data contract desacoplado de la fuente de datos
- **WHEN** se construye un `PaymentReceiptData`
- **THEN** no contiene referencias a entidades de dominio ni a DbContext — solo tipos primitivos y colecciones simples

---

### Requirement: Interfaz IDocumentGenerator
El sistema SHALL definir una interfaz `IDocumentGenerator` en el dominio que abstraiga la generación de bytes de documento, desacoplando la Application de la implementación de infraestructura.

```
Task<byte[]> GenerateAsync<TData>(string templateName, TData data, DocumentFormat format, CancellationToken ct)
```

Donde `DocumentFormat` es un enum: `Pdf`, `Excel`.

#### Scenario: Resolución por formato
- **WHEN** se llama `GenerateAsync` con `DocumentFormat.Pdf`
- **THEN** se usa el QuestPdfRenderer interno
- **WHEN** se llama con `DocumentFormat.Excel`
- **THEN** se usa el ExcelExporter interno
