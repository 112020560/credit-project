using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using MediatR;

namespace CreditSystem.Application.Queries.GetRestructureHistory;

public class GetRestructureHistoryQueryHandler
    : IRequestHandler<GetRestructureHistoryQuery, IReadOnlyList<RestructureHistoryResponse>>
{
    private readonly IEventStore _eventStore;

    public GetRestructureHistoryQueryHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IReadOnlyList<RestructureHistoryResponse>> Handle(
        GetRestructureHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _eventStore.GetEventsAsync(request.LoanId, 0, cancellationToken);

        var restructures = new List<RestructureHistoryResponse>();
        decimal previousRate = 0;
        int previousTerm = 0;

        foreach (var @event in events)
        {
            switch (@event)
            {
                case ContractCreated created:
                    previousRate = created.InterestRate.AnnualRate;
                    previousTerm = created.TermMonths;
                    break;

                case ContractRestructured restructured:
                    restructures.Add(new RestructureHistoryResponse
                    {
                        EventId = restructured.EventId,
                        RestructuredAt = restructured.OccurredAt,
                        PreviousRate = previousRate,
                        NewRate = restructured.NewRate.AnnualRate,
                        PreviousTermMonths = previousTerm,
                        NewTermMonths = restructured.NewTermMonths,
                        AmountForgiven = restructured.ForgiveAmount.Amount,
                        Reason = restructured.RestructureReason
                    });
                    previousRate = restructured.NewRate.AnnualRate;
                    previousTerm = restructured.NewTermMonths;
                    break;
            }
        }

        return restructures.AsReadOnly();
    }
}