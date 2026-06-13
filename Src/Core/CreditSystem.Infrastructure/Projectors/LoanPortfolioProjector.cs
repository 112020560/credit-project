using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Infrastructure.Projections;

namespace CreditSystem.Infrastructure.Projectors;

// Infrastructure/Projectors/LoanPortfolioProjector.cs
public class LoanPortfolioProjector : IProjection
{
    private readonly IProjectionStore _store;
    public string ProjectionName => "LoanPortfolio";

    public LoanPortfolioProjector(IProjectionStore store)
    {
        _store = store;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        switch (@event)
        {
            case ContractCreated e:
                await IncrementCounter("total_loans", ct);
                await IncrementAmount("total_principal", e.Principal.Amount, ct);
                await IncrementAmount("total_outstanding", e.Principal.Amount, ct);
                break;

            case LoanDisbursed:
                await IncrementCounter("active_loans", ct);
                break;

            case PaymentApplied e:
                await IncrementAmount("total_collected_principal", e.PrincipalPaid.Amount, ct);
                await IncrementAmount("total_collected_interest", e.InterestPaid.Amount, ct);
                await IncrementAmount("total_collected_fees", e.FeePaid.Amount, ct);
                await DecrementAmount("total_outstanding", e.PrincipalPaid.Amount, ct);
                break;

            case InterestAccrued e:
                await IncrementAmount("total_interest_accrued", e.Amount.Amount, ct);
                break;

            case PaymentMissed:
                await HandleDelinquencyChange(1, ct);
                break;

            case ContractDefaulted:
                await HandleStatusChange("delinquent_loans", -1, "defaulted_loans", 1, ct);
                break;

            case ContractPaidOff e:
                await HandleStatusChange("active_loans", -1, "paid_off_loans", 1, ct);
                await HandleContractPaidOff(e, ct);
                break;
            
            case ContractRestructured:
                await HandleContractRestructured(ct);
                break;
        }

        await RecalculateRates(ct);
    }
    
    private async Task HandleContractPaidOff(ContractPaidOff e, CancellationToken ct)
    {
        // Determinar de qué estado viene
        const string sql = @"
        UPDATE rm_loan_portfolio 
        SET active_loans = GREATEST(0, active_loans - 1),
            paid_off_loans = paid_off_loans + 1,
            total_outstanding = total_outstanding - @PrincipalPaid,
            updated_at = @Now
        WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new
        {
            PrincipalPaid = e.TotalPrincipalPaid.Amount,
            Now = DateTime.UtcNow
        }, ct);

        await RecalculateRates(ct);
    }

    private async Task IncrementCounter(string column, CancellationToken ct)
    {
        var sql = $@"
            INSERT INTO rm_loan_portfolio (id, {column}, updated_at)
            VALUES ('global', 1, @Now)
            ON CONFLICT (id) DO UPDATE SET
                {column} = rm_loan_portfolio.{column} + 1,
                updated_at = @Now";

        await _store.ExecuteAsync(sql, new { Now = DateTime.UtcNow }, ct);
    }
    
    private async Task HandleContractRestructured(CancellationToken ct)
    {
        // Mover de delinquent/default a active
        const string sql = @"
        UPDATE rm_loan_portfolio 
        SET delinquent_loans = GREATEST(0, delinquent_loans - 1),
            active_loans = active_loans + 1,
            updated_at = @Now
        WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new { Now = DateTime.UtcNow }, ct);
    
        await RecalculateRates(ct);
    }

    private async Task IncrementAmount(string column, decimal amount, CancellationToken ct)
    {
        var sql = $@"
            INSERT INTO rm_loan_portfolio (id, {column}, updated_at)
            VALUES ('global', @Amount, @Now)
            ON CONFLICT (id) DO UPDATE SET
                {column} = rm_loan_portfolio.{column} + @Amount,
                updated_at = @Now";

        await _store.ExecuteAsync(sql, new { Amount = amount, Now = DateTime.UtcNow }, ct);
    }

    private async Task DecrementAmount(string column, decimal amount, CancellationToken ct)
    {
        var sql = $@"
            UPDATE rm_loan_portfolio 
            SET {column} = {column} - @Amount,
                updated_at = @Now
            WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new { Amount = amount, Now = DateTime.UtcNow }, ct);
    }

    private async Task HandleDelinquencyChange(int delta, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_portfolio 
            SET delinquent_loans = delinquent_loans + @Delta,
                updated_at = @Now
            WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new { Delta = delta, Now = DateTime.UtcNow }, ct);
    }

    private async Task HandleStatusChange(
        string decrementColumn, 
        int decrementDelta,
        string incrementColumn, 
        int incrementDelta, 
        CancellationToken ct)
    {
        var sql = $@"
            UPDATE rm_loan_portfolio 
            SET {decrementColumn} = {decrementColumn} + @DecrementDelta,
                {incrementColumn} = {incrementColumn} + @IncrementDelta,
                updated_at = @Now
            WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new
        {
            DecrementDelta = decrementDelta,
            IncrementDelta = incrementDelta,
            Now = DateTime.UtcNow
        }, ct);
    }

    private async Task RecalculateRates(CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_portfolio 
            SET delinquency_rate = CASE WHEN total_loans > 0 
                    THEN delinquent_loans::decimal / total_loans 
                    ELSE 0 END,
                default_rate = CASE WHEN total_loans > 0 
                    THEN defaulted_loans::decimal / total_loans 
                    ELSE 0 END,
                updated_at = @Now
            WHERE id = 'global'";

        await _store.ExecuteAsync(sql, new { Now = DateTime.UtcNow }, ct);
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        await _store.ExecuteAsync("TRUNCATE TABLE rm_loan_portfolio", ct: ct);

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
        }
    }
}