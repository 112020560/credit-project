using CreditSystem.Domain.Abstractions.Documents;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models.Documents;
using MediatR;

namespace CreditSystem.Application.Queries.Documents;

public record GetPaymentReceiptQuery(Guid LoanId, Guid PaymentId) : IRequest<byte[]?>;

public class GetPaymentReceiptQueryHandler : IRequestHandler<GetPaymentReceiptQuery, byte[]?>
{
    private readonly ILoanQueryService _queryService;
    private readonly IDocumentGenerator _documentGenerator;

    public GetPaymentReceiptQueryHandler(
        ILoanQueryService queryService,
        IDocumentGenerator documentGenerator)
    {
        _queryService = queryService;
        _documentGenerator = documentGenerator;
    }

    public async Task<byte[]?> Handle(GetPaymentReceiptQuery request, CancellationToken cancellationToken)
    {
        var loan = await _queryService.GetLoanSummaryAsync(request.LoanId, cancellationToken);
        if (loan is null) return null;

        var history = await _queryService.GetPaymentHistoryAsync(request.LoanId, cancellationToken);
        var payment = history.FirstOrDefault(p => p.Id == request.PaymentId);
        if (payment is null) return null;

        var data = new PaymentReceiptData
        {
            LoanId = loan.LoanId,
            LoanNumber = loan.LoanId.ToString("N")[..8].ToUpper(),
            CustomerName = loan.CustomerName ?? "N/A",
            Currency = "CRC",
            PaymentDate = payment.PaymentDate,
            PrincipalApplied = payment.PrincipalPaid,
            InterestApplied = payment.InterestPaid,
            FeesApplied = payment.FeesPaid > 0 ? payment.FeesPaid : null,
            TotalPaid = payment.TotalAmount,
            RemainingBalance = payment.BalanceAfter
        };

        return await _documentGenerator.GenerateAsync(
            "payment-receipt.sbn", data, DocumentFormat.Pdf, cancellationToken);
    }
}
