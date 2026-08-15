using MediatR;

namespace CreditSystem.Application.Queries.GetPayoffAmount;

public record GetPayoffAmountQuery : IRequest<PayoffAmountResponse>
{
    public Guid LoanId { get; init; }
    public DateTime? AsOfDate { get; init; }
}