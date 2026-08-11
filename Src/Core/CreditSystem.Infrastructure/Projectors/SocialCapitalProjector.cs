using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Infrastructure.Projections;

namespace CreditSystem.Infrastructure.Projectors;

public class SocialCapitalProjector : IProjection
{
    private readonly IProjectionStore _store;
    public string ProjectionName => "SocialCapital";

    public SocialCapitalProjector(IProjectionStore store)
    {
        _store = store;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        if (@event is PaymentApplied e && e.SocialCapitalContributed.Amount > 0)
        {
            // rm_loan_summaries.customer_id = customer_credit_profiles.id (internal)
            // cooperative_members.external_id = customer_credit_profiles.external_id (CRM shared key)
            const string sql = """
                UPDATE cooperative_members cm
                SET social_capital_balance = cm.social_capital_balance + @Amount
                WHERE cm.external_id = (
                    SELECT ccp.external_id
                    FROM rm_loan_summaries ls
                    JOIN customer_credit_profiles ccp ON ccp.id = ls.customer_id
                    WHERE ls.loan_id = @LoanId
                    LIMIT 1
                )
                """;

            await _store.ExecuteAsync(sql, new
            {
                Amount = e.SocialCapitalContributed.Amount,
                LoanId = e.AggregateId
            }, ct);
        }
    }

    public Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        // Rebuild is handled by replaying PaymentApplied events through ProjectAsync.
        // Reset is not done here because social_capital_balance is owned by the member aggregate.
        return Task.CompletedTask;
    }
}
