using CreditSystem.Domain.Abstractions;
using MediatR;

namespace CreditSystem.Application.Queries.GetPayoffAmount;

public class GetPayoffAmountQueryHandler : IRequestHandler<GetPayoffAmountQuery, PayoffAmountResponse?>
{
    private readonly ILoanContractRepository _repository;

    public GetPayoffAmountQueryHandler(ILoanContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<PayoffAmountResponse?> Handle(
        GetPayoffAmountQuery request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
            return null;

        var state = aggregate.State;
        var asOfDate = request.AsOfDate ?? DateTime.UtcNow;

        // Calcular interés adicional si hay días desde última acumulación
        var additionalInterest = 0m;
        if (state.LastInterestAccrualDate.HasValue)
        {
            var daysSinceAccrual = (asOfDate.Date - state.LastInterestAccrualDate.Value.Date).Days;
            if (daysSinceAccrual > 0)
            {
                var dailyInterest = state.InterestRate.CalculateDailyInterest(state.CurrentBalance);
                additionalInterest = dailyInterest.Amount * daysSinceAccrual;
            }
        }

        var totalInterest = state.AccruedInterest.Amount + additionalInterest;
        var totalPayoff = state.CurrentBalance.Amount + totalInterest + state.TotalFees.Amount;

        return new PayoffAmountResponse
        {
            LoanId = request.LoanId,
            PrincipalBalance = state.CurrentBalance.Amount,
            AccruedInterest = totalInterest,
            PendingFees = state.TotalFees.Amount,
            TotalPayoffAmount = totalPayoff,
            CalculatedAsOf = asOfDate,
            ValidUntil = asOfDate.Date.AddDays(10), // Válido por 10 días
            Currency = state.Principal.Currency
        };
    }
}