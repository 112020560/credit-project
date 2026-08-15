## Context

El sistema carece de capacidad nativa de generación de documentos. Actualmente no hay ningún mecanismo de exportación de PDFs ni Excel. Los datos necesarios (pagos, saldos, schedules) ya existen en los read models `rm_payment_history`, `rm_loan_summaries` y en el event store a través del aggregate.

El stack es .NET 9 + Clean Architecture. La restricción principal es no introducir dependencias de runtime externo (Chrome, librerías nativas C) que compliquen el despliegue en contenedores.

## Goals / Non-Goals

**Goals:**
- Motor de templates parametrizable con Scriban (.sbn files) — editable sin recompilar
- Generación de PDF con QuestPDF (puro .NET, sin runtime externo)
- Exportación de Excel con ClosedXML
- Tres documentos: comprobante de pago, carta de saldo, tabla de amortización
- Arquitectura extensible: agregar un nuevo documento = nuevo template + nuevo data contract + nuevo query handler

**Non-Goals:**
- Internacionalización (solo español por ahora)
- Almacenamiento de documentos generados (generación bajo demanda, sin persistencia)
- Firma digital o sellado de tiempo de documentos
- Templates editables desde UI o base de datos (extensión futura)
- Autenticación/autorización de endpoints (se implementará con JWT en fase posterior)

## Decisions

### D1: Scriban como motor de templates

**Elegido sobre**: Razor (RazorLight), Handlebars.NET, Fluid.

**Razón**: Scriban es sandboxed por diseño — no ejecuta código C# arbitrario dentro de la plantilla, lo que lo hace seguro para templates de negocio. Tiene sintaxis expresiva (filtros, bucles, fechas), es rápido, y no requiere pipeline de compilación Razor. Los archivos `.sbn` se pueden editar en cualquier editor de texto.

**Trade-off**: Scriban no tiene el autocompletado de Razor en IDEs. Se mitiga documentando el contrato de datos en comentarios dentro del template.

---

### D2: QuestPDF como renderer de PDF

**Elegido sobre**: PuppeteerSharp (Chrome headless), iText7 (AGPL), DinkToPdf (wkhtmltopdf nativo).

**Razón**: QuestPDF es puro .NET, no requiere ningún proceso externo ni librería nativa. La licencia Community es gratuita para proyectos open source y para organizaciones con ingresos < $1M USD. El API fluente permite layouts profesionales con tablas, headers, footers y paginación automática sin HTML.

**Trade-off**: El layout se define en C# (no en el `.sbn`). Esto significa que cambiar el diseño visual requiere recompilar. Se acepta porque el layout corporativo es estable; solo el contenido (datos) varía por instancia.

**Separación de responsabilidades**:
- Scriban renderiza el **contenido** (datos de negocio → texto estructurado)
- QuestPDF estructura el **layout visual** (encabezados, columnas, paginación)

---

### D3: ClosedXML para Excel

**Razón**: ClosedXML es maduro, sin dependencias de Office, licencia MIT. Para tablas de amortización el formato tabular de Excel es más útil que PDF (el cliente puede filtrar, calcular totales propios).

---

### D4: Templates como archivos en disco (no embedded resources)

**Elegido sobre**: embedded resources compilados en el assembly.

**Razón**: Los archivos en disco permiten editar templates sin recompilar ni redeploy del binario. Se copian al output directory con `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` en el `.csproj`.

**En producción**: Si se desea mayor control, los templates pueden migrarse a base de datos (tabla `document_templates`) con una implementación alternativa de `ITemplateLoader` sin cambiar el resto de la arquitectura.

---

### D5: Data source por tipo de documento

| Documento | Fuente de datos |
|-----------|----------------|
| Comprobante de pago | `rm_payment_history` + `rm_loan_summaries` (sin event store) |
| Carta de saldo | `rm_loan_summaries` exclusivamente (sin event store) |
| Tabla de amortización | Aggregate rehydratado desde event store (`State.PaymentSchedule`) |

La tabla de amortización usa el aggregate porque el schedule puede haber cambiado por reestructuraciones o reajustes de tasa, y el read model no almacena el schedule completo — solo la próxima cuota.

---

### D6: Arquitectura de capas

```
Domain/Abstractions/Documents/
  IDocumentGenerator.cs          ← interfaz genérica con GenerateAsync<TData>
Domain/Models/Documents/
  DocumentFormat.cs              ← enum Pdf | Excel
  PaymentReceiptData.cs
  BalanceLetterData.cs
  AmortizationTableData.cs
  AmortizationRowData.cs         ← fila individual del schedule

Application/Queries/Documents/
  GetPaymentReceiptQuery.cs + Handler
  GetBalanceLetterQuery.cs + Handler
  GetAmortizationTableQuery.cs + Handler

Infrastructure/Documents/
  DocumentGenerator.cs           ← implementa IDocumentGenerator, orquesta Scriban + QuestPDF/ClosedXML
  ScribanTemplateEngine.cs       ← lee .sbn, renderiza con datos
  QuestPdfRenderer.cs            ← layout visual → bytes PDF
  ExcelExporter.cs               ← ClosedXML → bytes .xlsx
  Templates/
    payment-receipt.sbn
    balance-letter.sbn
    amortization-table.sbn

Api/EndPoints/DocumentEndpoints.cs
```

## Risks / Trade-offs

**[QuestPDF licencia Community]** → Verificar que el proyecto cumple con los criterios de la licencia Community antes de producción. Si aplica licencia comercial (~$299/año), es aceptable para una cooperativa.

**[Templates en disco en contenedor]** → Los archivos `.sbn` deben estar en la imagen Docker. Con `CopyToOutputDirectory=Always` quedan en `bin/` junto al executable. Verificar en el Dockerfile que no se excluyen durante la copia del publish output.

**[Performance de rehydratación para tabla de amortización]** → Para préstamos de largo plazo con muchos eventos, rehydratar el aggregate puede ser lento. Mitigación: el aggregate ya soporta snapshots; si hay performance issues, se agrega snapshot para préstamos con > 50 eventos.

**[Schedule completo vs. schedule residual en tabla]** → Se retorna `State.PaymentSchedule` completo (cuotas pasadas y futuras). Si el cliente espera ver solo las cuotas pendientes, hay que filtrar. Decisión: mostrar el schedule completo con indicación visual de pagadas/pendientes en el template.

## Migration Plan

1. Agregar paquetes NuGet: `QuestPDF`, `Scriban`, `ClosedXML` a `CreditSystem.Infrastructure`
2. Crear estructura de carpetas y clases según D6
3. Registrar `IDocumentGenerator → DocumentGenerator` (Scoped) en `DependencyInjection.cs`
4. Registrar `DocumentEndpoints` en `Program.cs`
5. Sin cambios a esquema de base de datos
6. Sin rollback necesario — endpoints nuevos, no modifican funcionalidad existente

## Open Questions

- ¿El logo de la cooperativa debe incluirse en los PDFs? Si sí, ¿desde qué fuente (archivo estático, configuración)?
- ¿Se requiere número de folio/consecutivo en el comprobante de pago para control interno?
- ¿La carta de saldo debe tener vigencia explícita (ej. "válida por 30 días")?
