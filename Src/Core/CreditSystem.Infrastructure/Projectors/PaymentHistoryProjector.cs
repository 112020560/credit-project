using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;

namespace CreditSystem.Infrastructure.Projectors;

// Infrastructure/Projectors/PaymentHistoryProjector.cs
public class PaymentHistoryProjector : IProjection
{
    private readonly IProjectionStore _store;
    public string ProjectionName => "PaymentHistory";

    public PaymentHistoryProjector(IProjectionStore store)
    {
        _store = store;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        if (@event is PaymentApplied e)
        {
            var model = new PaymentHistoryReadModel
            {
                Id = e.PaymentId,
                LoanId = e.AggregateId,
                PaymentNumber = e.PaymentNumber,
                PaymentDate = e.OccurredAt,
                TotalAmount = e.TotalAmount.Amount,
                PrincipalPaid = e.PrincipalPaid.Amount,
                InterestPaid = e.InterestPaid.Amount,
                FeesPaid = e.FeePaid.Amount,
                BalanceAfter = e.NewBalance.Amount,
                PaymentMethod = e.Method.ToString(),
                Status = "completed"
            };

            await _store.UpsertAsync("rm_payment_history", model, "id", ct);
        }
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        await _store.ExecuteAsync("TRUNCATE TABLE rm_payment_history", ct: ct);

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
        }
    }
}