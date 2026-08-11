using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CreditSystem.Infrastructure.Documents;

public class QuestPdfRenderer
{
    private const string CooperativeName = "Cooperativa de Crédito";

    public Task<byte[]> RenderAsync(string title, string content)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(CooperativeName)
                        .Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                    col.Item().Text(title)
                        .FontSize(12).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    foreach (var line in content.Split('\n'))
                    {
                        col.Item().Text(line).FontSize(10);
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}   ");
                    text.Span("Página ").FontSize(9);
                    text.CurrentPageNumber().FontSize(9);
                    text.Span(" de ").FontSize(9);
                    text.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
