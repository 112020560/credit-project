using ClosedXML.Excel;
using DomainDocuments = CreditSystem.Domain.Models.Documents;

namespace CreditSystem.Infrastructure.Documents;

public class ExcelExporter
{
    public Task<byte[]> ExportAmortizationAsync(DomainDocuments.AmortizationTableData data)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tabla de Amortización");

        // Title
        ws.Cell(1, 1).Value = $"Tabla de Amortización — Préstamo {data.LoanNumber}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;

        ws.Cell(2, 1).Value = $"Cliente: {data.CustomerName}";
        ws.Range(2, 1, 2, 6).Merge();

        ws.Cell(3, 1).Value = $"Monto original: {data.OriginalAmount:N2} {data.Currency}   Tasa: {data.InterestRate:N2}%   Plazo: {data.TermMonths} meses";
        ws.Range(3, 1, 3, 6).Merge();

        // Headers (row 5)
        var headers = new[] { "Cuota", "Fecha", $"Pago ({data.Currency})", $"Interés ({data.Currency})", $"Capital ({data.Currency})", $"Saldo ({data.Currency})" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        int row = 6;
        foreach (var entry in data.Rows)
        {
            ws.Cell(row, 1).Value = entry.EntryNumber;
            ws.Cell(row, 2).Value = entry.DueDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = entry.Payment;
            ws.Cell(row, 4).Value = entry.Interest;
            ws.Cell(row, 5).Value = entry.Principal;
            ws.Cell(row, 6).Value = entry.RemainingBalance;

            for (int c = 3; c <= 6; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = data.TotalPayment;
        ws.Cell(row, 4).Value = data.TotalInterest;
        ws.Cell(row, 5).Value = data.TotalPrincipal;
        for (int c = 3; c <= 5; c++)
        {
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
