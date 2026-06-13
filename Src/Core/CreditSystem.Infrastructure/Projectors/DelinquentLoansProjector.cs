using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projectors;

// Infrastructure/Projectors/DelinquentLoansProjector.cs
public class DelinquentLoansProjector : IProjection
{
    private readonly IProjectionStore _store;
    private readonly ICustomerService _customerService;
    private readonly ILogger<DelinquentLoansProjector> _logger;

    public string ProjectionName => "DelinquentLoans";

    public DelinquentLoansProjector(
        IProjectionStore store,
        ICustomerService customerService,
        ILogger<DelinquentLoansProjector> logger)
    {
        _store = store;
        _customerService = customerService;
        _logger = logger;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        switch (@event)
        {
            case PaymentMissed e:
                await HandlePaymentMissed(e, ct);
                break;
            case PaymentApplied e:
                await HandlePaymentApplied(e, ct);
                break;
            case ContractDefaulted e:
                await HandleContractDefaulted(e, ct);
                break;
            case ContractPaidOff e:
                await HandleContractPaidOff(e, ct);
                break;
        }
    }

    private async Task HandlePaymentMissed(PaymentMissed e, CancellationToken ct)
    {
        // Obtener loan summary para datos adicionales
        var loan = await _store.GetByIdAsync<LoanSummaryReadModel>(
            "rm_loan_summaries", "loan_id", e.AggregateId, ct);

        if (loan == null) return;

        var customer = await _customerService.GetByIdAsync(loan.CustomerId, ct);
        
        // Verificar si ya existe en delinquent
        var existing = await _store.GetByIdAsync<DelinquentLoanReadModel>(
            "rm_delinquent_loans", "loan_id", e.AggregateId, ct);

        if (existing != null)
        {
            // Actualizar existente
            const string updateSql = @"
            UPDATE rm_delinquent_loans 
            SET days_overdue = @DaysOverdue,
                payments_missed = @PaymentsMissed,
                total_owed = @TotalOwed,
                updated_at = @Now
            WHERE loan_id = @LoanId";

            await _store.ExecuteAsync(updateSql, new
            {
                LoanId = e.AggregateId,
                DaysOverdue = e.DaysOverdue,
                PaymentsMissed = loan.PaymentsMissed + 1,
                TotalOwed = loan.TotalOwed + e.LateFeeApplied.Amount,
                Now = DateTime.UtcNow
            }, ct);
        }
        else
        {
            var model = new DelinquentLoanReadModel
            {
                LoanId = e.AggregateId,
                CustomerId = loan.CustomerId,
                CustomerName = customer?.FullName,
                CustomerPhone = customer?.Phone,
                CustomerEmail = customer?.Email,
                Principal = loan.Principal,
                CurrentBalance = loan.CurrentBalance,
                TotalOwed = loan.TotalOwed,
                DaysOverdue = e.DaysOverdue,
                PaymentsMissed = loan.PaymentsMissed + 1,
                LastPaymentAt = loan.LastPaymentAt,
                NextActionDate = DateTime.UtcNow.AddDays(3).Date,
                CollectionStatus = "pending_contact",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _store.UpsertAsync("rm_delinquent_loans", model, "loan_id", ct);
        }
        
        _logger.LogDebug("Projected PaymentMissed for loan {LoanId}", e.AggregateId);
    }

    private async Task HandlePaymentApplied(PaymentApplied e, CancellationToken ct)
    {
        // Verificar si ya no está moroso
        var loan = await _store.GetByIdAsync<LoanSummaryReadModel>(
            "rm_loan_summaries", "loan_id", e.AggregateId, ct);

        if (loan == null || loan.PaymentsMissed == 0)
        {
            // Ya no es moroso, remover de la lista
            await _store.DeleteAsync("rm_delinquent_loans", "loan_id", e.AggregateId, ct);
        }
        else
        {
            // Actualizar balance
            const string sql = @"
                UPDATE rm_delinquent_loans 
                SET current_balance = @Balance,
                    total_owed = @TotalOwed,
                    last_payment_at = @Now,
                    updated_at = @Now
                WHERE loan_id = @LoanId";

            await _store.ExecuteAsync(sql, new
            {
                LoanId = e.AggregateId,
                Balance = e.NewBalance.Amount,
                TotalOwed = loan.TotalOwed,
                Now = DateTime.UtcNow
            }, ct);
        }
    }

    private async Task HandleContractDefaulted(ContractDefaulted e, CancellationToken ct)
    {
        const string sql = @"
        UPDATE rm_delinquent_loans 
        SET collection_status = 'defaulted',
            updated_at = @Now
        WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Updated delinquent status to defaulted for loan {LoanId}", e.AggregateId);
    }

    private async Task HandleContractPaidOff(ContractPaidOff e, CancellationToken ct)
    {
        // Remover de lista de morosos si estaba ahí
        await _store.DeleteAsync("rm_delinquent_loans", "loan_id", e.AggregateId, ct);

        _logger.LogDebug("Removed loan {LoanId} from delinquent list after payoff", e.AggregateId);
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        await _store.ExecuteAsync("TRUNCATE TABLE rm_delinquent_loans", ct: ct);

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
        }
    }
    
    private async Task HandleContractRestructured(ContractRestructured e, CancellationToken ct)
    {
        // Remover de la lista de morosos ya que vuelve a estar activo
        await _store.DeleteAsync("rm_delinquent_loans", "loan_id", e.AggregateId, ct);

        _logger.LogDebug("Removed loan {LoanId} from delinquent list after restructure", e.AggregateId);
    }
}