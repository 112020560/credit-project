using MediatR;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingCreditSummary;

public record GetRevolvingCreditSummaryQuery : IRequest<RevolvingCreditSummaryResponse?>
{
    public Guid CreditLineId { get; init; }
}