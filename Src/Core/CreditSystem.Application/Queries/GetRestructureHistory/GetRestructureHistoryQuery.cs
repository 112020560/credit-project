using MediatR;

namespace CreditSystem.Application.Queries.GetRestructureHistory;

public record GetRestructureHistoryQuery : IRequest<IReadOnlyList<RestructureHistoryResponse>>
{
    public Guid LoanId { get; init; }
}
