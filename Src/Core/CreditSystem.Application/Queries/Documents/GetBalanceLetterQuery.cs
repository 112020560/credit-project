using CreditSystem.Domain.Abstractions.Documents;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models.Documents;
using MediatR;

namespace CreditSystem.Application.Queries.Documents;

public record GetBalanceLetterQuery(Guid LoanId) : IRequest<byte[]?>;

public class GetBalanceLetterQueryHandler : IRequestHandler<GetBalanceLetterQuery, byte[]?>
{
    private readonly ILoanQueryService _queryService;
    private readonly IDocumentGenerator _documentGenerator;

    public GetBalanceLetterQueryHandler(
        ILoanQueryService queryService,
        IDocumentGenerator documentGenerator)
    {
        _queryService = queryService;
        _documentGenerator = documentGenerator;
    }

    public async Task<byte[]?> Handle(GetBalanceLetterQuery request, CancellationToken cancellationToken)
    {
        var loan = await _queryService.GetLoanSummaryAsync(request.LoanId, cancellationToken);
        if (loan is null) return null;

        // Estimate maturity date from term months and disbursement date
        DateTime? maturityDate = loan.DisbursedAt.HasValue
            ? loan.DisbursedAt.Value.AddMonths(loan.TermMonths)
            : null;

        var data = new BalanceLetterData
        {
            LoanId = loan.LoanId,
            LoanNumber = loan.LoanId.ToString("N")[..8].ToUpper(),
            CustomerName = loan.CustomerName ?? "N/A",
            Currency = "CRC",
            DisbursementDate = loan.DisbursedAt ?? loan.CreatedAt,
            OriginalAmount = loan.Principal,
            CurrentBalance = loan.CurrentBalance,
            InterestRate = loan.InterestRate,
            RateType = loan.RateType,
            Spread = loan.Spread > 0 ? loan.Spread : null,
            ReferenceRateId = loan.ReferenceRateId,
            NextPaymentDate = loan.NextPaymentDate,
            NextPaymentAmount = loan.NextPaymentAmount,
            MaturityDate = maturityDate,
            Status = loan.Status,
            IssuedAt = DateTime.UtcNow
        };

        return await _documentGenerator.GenerateAsync(
            "balance-letter.sbn", data, DocumentFormat.Pdf, cancellationToken);
    }
}
