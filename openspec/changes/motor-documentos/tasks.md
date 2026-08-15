## 1. Paquetes NuGet e infraestructura base

- [x] 1.1 Agregar `QuestPDF` (licencia Community) a `CreditSystem.Infrastructure.csproj`
- [x] 1.2 Agregar `Scriban` a `CreditSystem.Infrastructure.csproj`
- [x] 1.3 Agregar `ClosedXML` a `CreditSystem.Infrastructure.csproj`
- [x] 1.4 Crear directorio `Src/Core/CreditSystem.Infrastructure/Documents/Templates/` con los tres archivos `.sbn` vacíos: `payment-receipt.sbn`, `balance-letter.sbn`, `amortization-table.sbn`
- [x] 1.5 Configurar los tres `.sbn` como `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` en el `.csproj` de Infrastructure

## 2. Dominio — Abstracciones y data contracts

- [x] 2.1 Crear enum `DocumentFormat` en `CreditSystem.Domain/Models/Documents/`: valores `Pdf = 0`, `Excel = 1`
- [x] 2.2 Crear interfaz `IDocumentGenerator` en `CreditSystem.Domain/Abstractions/Documents/`: `Task<byte[]> GenerateAsync<TData>(string templateName, TData data, DocumentFormat format, CancellationToken ct = default)`
- [x] 2.3 Crear `PaymentReceiptData` en `CreditSystem.Domain/Models/Documents/`: `LoanId`, `LoanNumber` (string), `CustomerName`, `Currency`, `PaymentDate` (DateTime), `PrincipalApplied`, `InterestApplied`, `FeesApplied` (decimal nullable), `TotalPaid`, `RemainingBalance`
- [x] 2.4 Crear `BalanceLetterData` en `CreditSystem.Domain/Models/Documents/`: `LoanId`, `LoanNumber`, `CustomerName`, `Currency`, `DisbursementDate`, `OriginalAmount`, `CurrentBalance`, `InterestRate`, `RateType` (string), `Spread` (decimal nullable), `ReferenceRateId` (string nullable), `NextPaymentDate`, `NextPaymentAmount`, `MaturityDate`, `Status` (string), `IssuedAt` (DateTime)
- [x] 2.5 Crear `AmortizationRowData` en `CreditSystem.Domain/Models/Documents/`: `EntryNumber` (int), `DueDate` (DateTime), `Payment`, `Interest`, `Principal`, `RemainingBalance`, `Currency`
- [x] 2.6 Crear `AmortizationTableData` en `CreditSystem.Domain/Models/Documents/`: `LoanId`, `LoanNumber`, `CustomerName`, `Currency`, `OriginalAmount`, `InterestRate`, `TermMonths` (int), `GeneratedAt` (DateTime), `Rows` (IReadOnlyList\<AmortizationRowData\>), `TotalPayment`, `TotalInterest`, `TotalPrincipal`

## 3. Infraestructura — Motor de documentos

- [x] 3.1 Crear `ScribanTemplateEngine` en `CreditSystem.Infrastructure/Documents/`: método `RenderAsync<TData>(string templateFileName, TData data)` que lee el `.sbn` desde disco (ruta relativa al assembly), crea un `Scriban.Template`, expone el objeto data como `model` en el contexto y retorna el string renderizado
- [x] 3.2 Crear `QuestPdfRenderer` en `CreditSystem.Infrastructure/Documents/`: método `RenderAsync(string title, string content)` que usa QuestPDF para construir un documento con encabezado (título + nombre cooperativa), cuerpo (texto del content), y pie de página (fecha generación + número de página); retorna `byte[]`
- [x] 3.3 Crear `ExcelExporter` en `CreditSystem.Infrastructure/Documents/`: método `ExportAmortizationAsync(AmortizationTableData data)` que usa ClosedXML para crear un workbook con headers `[Cuota, Fecha, Pago, Interés, Capital, Saldo]`, popula filas desde `data.Rows`, agrega fila de totales, retorna `byte[]`
- [x] 3.4 Crear `DocumentGenerator` en `CreditSystem.Infrastructure/Documents/` implementando `IDocumentGenerator`: orquesta `ScribanTemplateEngine` para el contenido y según el `DocumentFormat` llama `QuestPdfRenderer` o `ExcelExporter`; para Excel solo soporta `AmortizationTableData` (lanzar `NotSupportedException` para otros tipos con `Excel`)
- [x] 3.5 Registrar `IDocumentGenerator → DocumentGenerator` (Scoped) en `DependencyInjection.cs` de Infrastructure

## 4. Templates Scriban

- [x] 4.1 Implementar `payment-receipt.sbn`: encabezado con nombre cooperativa, fecha/hora del pago, datos del cliente y préstamo, desglose capital/intereses/comisiones (omitir comisiones si es 0), total pagado, saldo restante
- [x] 4.2 Implementar `balance-letter.sbn`: encabezado formal con fecha de emisión, datos del préstamo (monto original, saldo actual, tasa, tipo de tasa, próximo pago, vencimiento), advertencia de vigencia del documento
- [x] 4.3 Implementar `amortization-table.sbn`: encabezado con datos del préstamo, tabla con columnas [N°, Fecha, Cuota, Interés, Capital, Saldo] iterando `model.rows`, fila de totales al final

## 5. Application — Query handlers

- [x] 5.1 Crear `GetPaymentReceiptQuery` en `CreditSystem.Application/Queries/Documents/`: `Guid LoanId`, `Guid PaymentId`; handler que consulta `rm_loan_summaries` y `rm_payment_history` via Dapper, construye `PaymentReceiptData`, llama `IDocumentGenerator.GenerateAsync` con `DocumentFormat.Pdf`, retorna `byte[]` o null si no encontrado
- [x] 5.2 Crear `GetBalanceLetterQuery` en `CreditSystem.Application/Queries/Documents/`: `Guid LoanId`; handler que consulta `rm_loan_summaries` via Dapper, construye `BalanceLetterData`, llama `IDocumentGenerator.GenerateAsync` con `DocumentFormat.Pdf`, retorna `byte[]` o null
- [x] 5.3 Crear `GetAmortizationTableQuery` en `CreditSystem.Application/Queries/Documents/`: `Guid LoanId`, `DocumentFormat Format`; handler que rehydrata `LoanContractAggregate` via `ILoanContractRepository.GetByIdAsync`, mapea `State.PaymentSchedule` a lista de `AmortizationRowData`, construye `AmortizationTableData`, llama `IDocumentGenerator.GenerateAsync` con el `Format` dado, retorna `byte[]` o null

## 6. API — Endpoints de documentos

- [x] 6.1 Crear `DocumentEndpoints.cs` en `CreditSystem.Api/EndPoints/` con método de extensión `MapDocumentEndpoints(this IEndpointRouteBuilder app)`
- [x] 6.2 Implementar `GET /api/loans/{id}/documents/payment-receipt/{paymentId}`: envía query, retorna 404 si null, o `Results.File(bytes, "application/pdf", $"comprobante-{paymentId}.pdf")` con header `Content-Disposition: attachment`
- [x] 6.3 Implementar `GET /api/loans/{id}/documents/balance-letter`: envía query, retorna 404 si null, o `Results.File(bytes, "application/pdf", $"carta-saldo-{id}.pdf")`
- [x] 6.4 Implementar `GET /api/loans/{id}/documents/amortization-table?format=pdf|xlsx`: valida parámetro `format` (400 si inválido), parsea a `DocumentFormat`, envía query, retorna 404 si null, o `Results.File` con Content-Type correcto según formato
- [x] 6.5 Registrar `app.MapDocumentEndpoints()` en `Program.cs`

## 7. Tests

- [x] 7.1 Test unitario: `ScribanTemplateEngine` renderiza correctamente variables básicas en un template inline (sin leer disco)
- [x] 7.2 Test unitario: `GetPaymentReceiptQuery` handler retorna null cuando el pago no existe
- [x] 7.3 Test unitario: `GetBalanceLetterQuery` handler retorna null cuando el préstamo no existe
- [x] 7.4 Test unitario: `GetAmortizationTableQuery` handler mapea correctamente `State.PaymentSchedule` a `AmortizationTableData` con totales calculados
- [x] 7.5 Test unitario: `DocumentGenerator` lanza `NotSupportedException` al intentar exportar `PaymentReceiptData` en formato `Excel`
