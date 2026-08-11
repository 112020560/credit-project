using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Documents;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models.Documents;
using MediatR;

namespace CreditSystem.Application.Queries.Documents;

public record GetAmortizationTableQuery(Guid LoanId, DocumentFormat Format) : IRequest<byte[]?>;

public class GetAmortizationTableQueryHandler : IRequestHandler<GetAmortizationTableQuery, byte[]?>
{
    private readonly ILoanContractRepository _repository;
    private readonly ILoanQueryService _queryService;
    private readonly IDocumentGenerator _documentGenerator;

    public GetAmortizationTableQueryHandler(
        ILoanContractRepository repository,
        ILoanQueryService queryService,
        IDocumentGenerator documentGenerator)
    {
        _repository = repository;
        _queryService = queryService;
        _documentGenerator = documentGenerator;
    }

    public async Task<byte[]?> Handle(GetAmortizationTableQuery request, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);
        if (aggregate is null) return null;

        var loan = await _queryService.GetLoanSummaryAsync(request.LoanId, cancellationToken);

        var rows = aggregate.State.Schedule.Entries
            .Select((e, idx) => new AmortizationRowData
            {
                EntryNumber = e.PaymentNumber > 0 ? e.PaymentNumber : idx + 1,
                DueDate = e.DueDate,
                Payment = e.TotalPayment.Amount,
                Interest = e.Interest.Amount,
                Principal = e.Principal.Amount,
                RemainingBalance = e.Balance.Amount,
                Currency = e.TotalPayment.Currency
            })
            .ToList();

        var data = new AmortizationTableData
        {
            LoanId = aggregate.State.Id,
            LoanNumber = aggregate.State.Id.ToString("N")[..8].ToUpper(),
            CustomerName = loan?.CustomerName ?? "N/A",
            Currency = aggregate.State.Principal.Currency,
            OriginalAmount = aggregate.State.Principal.Amount,
            InterestRate = aggregate.State.InterestRate.AnnualRate,
            TermMonths = aggregate.State.TermMonths,
            GeneratedAt = DateTime.UtcNow,
            Rows = rows,
            TotalPayment = rows.Sum(r => r.Payment),
            TotalInterest = rows.Sum(r => r.Interest),
            TotalPrincipal = rows.Sum(r => r.Principal)
        };

        return await _documentGenerator.GenerateAsync(
            "amortization-table.sbn", data, request.Format, cancellationToken);
    }
}
