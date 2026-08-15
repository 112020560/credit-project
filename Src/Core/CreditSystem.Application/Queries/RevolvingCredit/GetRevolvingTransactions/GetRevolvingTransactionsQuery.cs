using MediatR;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingTransactions;

public record GetRevolvingTransactionsQuery : IRequest<IReadOnlyList<RevolvingTransactionResponse>>
{
    public Guid CreditLineId { get; init; }
    public int Limit { get; init; } = 50;
}
