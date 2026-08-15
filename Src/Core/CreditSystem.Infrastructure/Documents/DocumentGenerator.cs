using CreditSystem.Domain.Abstractions.Documents;
using DomainDocuments = CreditSystem.Domain.Models.Documents;

namespace CreditSystem.Infrastructure.Documents;

public class DocumentGenerator : IDocumentGenerator
{
    private readonly ScribanTemplateEngine _templateEngine;
    private readonly QuestPdfRenderer _pdfRenderer;
    private readonly ExcelExporter _excelExporter;

    public DocumentGenerator(
        ScribanTemplateEngine templateEngine,
        QuestPdfRenderer pdfRenderer,
        ExcelExporter excelExporter)
    {
        _templateEngine = templateEngine;
        _pdfRenderer = pdfRenderer;
        _excelExporter = excelExporter;
    }

    public async Task<byte[]> GenerateAsync<TData>(
        string templateName,
        TData data,
        DomainDocuments.DocumentFormat format,
        CancellationToken ct = default)
    {
        if (format == DomainDocuments.DocumentFormat.Excel)
        {
            if (data is DomainDocuments.AmortizationTableData tableData)
                return await _excelExporter.ExportAmortizationAsync(tableData);

            throw new NotSupportedException(
                $"Excel export is only supported for {nameof(DomainDocuments.AmortizationTableData)}. " +
                $"Received: {typeof(TData).Name}");
        }

        var content = await _templateEngine.RenderAsync(templateName, data);
        var title = Path.GetFileNameWithoutExtension(templateName)
            .Replace("-", " ")
            .ToUpperInvariant();

        return await _pdfRenderer.RenderAsync(title, content);
    }
}
