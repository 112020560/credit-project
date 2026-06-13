using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projectors;

public class RevolvingCreditSummaryProjector : IProjection
{
    private readonly IProjectionStore _store;
    private readonly ICustomerService _customerService;
    private readonly ILogger<RevolvingCreditSummaryProjector> _logger;
    
    public string ProjectionName => "RevolvingCreditSummary";

    public RevolvingCreditSummaryProjector(
        IProjectionStore store,
        ICustomerService customerService,
        ILogger<RevolvingCreditSummaryProjector> logger)
    {
        _store = store;
        _customerService = customerService;
        _logger = logger;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        var task = @event switch
        {
            CreditLineCreated e => HandleCreditLineCreated(e, ct),
            CreditLineActivated e => HandleCreditLineActivated(e, ct),
            FundsDrawn e => HandleFundsDrawn(e, ct),
            RevolvingPaymentApplied e => HandlePaymentApplied(e, ct),
            RevolvingInterestAccrued e => HandleInterestAccrued(e, ct),
            StatementGenerated e => HandleStatementGenerated(e, ct),
            CreditLineFrozen e => HandleCreditLineFrozen(e, ct),
            CreditLineUnfrozen e => HandleCreditLineUnfrozen(e, ct),
            CreditLimitChanged e => HandleCreditLimitChanged(e, ct),
            CreditLineClosed e => HandleCreditLineClosed(e, ct),
            _ => Task.CompletedTask
        };

        await task;
    }
    
    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding {ProjectionName} projection...", ProjectionName);

        // Limpiar tablas
        await _store.ExecuteAsync("DELETE FROM rm_revolving_transactions", null, ct);
        await _store.ExecuteAsync("DELETE FROM rm_revolving_statements", null, ct);
        await _store.ExecuteAsync("DELETE FROM rm_revolving_credit_summaries", null, ct);

        var count = 0;

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
            count++;

            if (count % 100 == 0)
            {
                _logger.LogDebug("Rebuilt {Count} events for {ProjectionName}", count, ProjectionName);
            }
        }

        _logger.LogInformation("Completed rebuilding {ProjectionName}. Total events: {Count}", ProjectionName, count);
    }

    private async Task HandleCreditLineCreated(CreditLineCreated e, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(e.CustomerId, ct);

        var model = new RevolvingCreditSummaryReadModel
        {
            CreditLineId = e.AggregateId,
            CustomerId = e.CustomerId,
            CustomerName = customer?.FullName,
            Status = RevolvingCreditStatus.Pending.ToString(),
            CreditLimit = e.CreditLimit.Amount,
            CurrentBalance = 0,
            AvailableCredit = e.CreditLimit.Amount,
            AccruedInterest = 0,
            PendingFees = 0,
            InterestRate = e.InterestRate.AnnualRate,
            MinimumPaymentPercentage = e.MinimumPaymentPercentage,
            MinimumPaymentAmount = e.MinimumPaymentAmount.Amount,
            BillingCycleDay = e.BillingCycleDay,
            GracePeriodDays = e.GracePeriodDays,
            ConsecutiveMissedPayments = 0,
            Currency = e.CreditLimit.Currency,
            Version = e.Version,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_revolving_credit_summaries", model, "credit_line_id", ct);

        _logger.LogDebug("Projected CreditLineCreated for {CreditLineId}", e.AggregateId);
    }

    private async Task HandleCreditLineActivated(CreditLineActivated e, CancellationToken ct)
    {
        // Calcular próxima fecha de estado de cuenta
        var nextStatementDate = CalculateNextStatementDate(e.ActivatedAt);

        const string sql = @"
            UPDATE rm_revolving_credit_summaries 
            SET status = @Status,
                activated_at = @ActivatedAt,
                next_statement_date = @NextStatementDate,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = e.AggregateId,
            Status = RevolvingCreditStatus.Active.ToString(),
            ActivatedAt = e.ActivatedAt,
            NextStatementDate = nextStatementDate,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected CreditLineActivated for {CreditLineId}", e.AggregateId);
    }

    private async Task HandleFundsDrawn(FundsDrawn e, CancellationToken ct)
    {
        // Actualizar resumen
        const string updateSql = @"
            UPDATE rm_revolving_credit_summaries 
            SET current_balance = @NewBalance,
                available_credit = @AvailableCredit,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(updateSql, new
        {
            CreditLineId = e.AggregateId,
            NewBalance = e.NewBalance.Amount,
            AvailableCredit = e.AvailableCredit.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        // Insertar transacción
        var transaction = new RevolvingTransactionReadModel
        {
            Id = e.DrawId,
            CreditLineId = e.AggregateId,
            TransactionType = "Draw",
            Amount = e.Amount.Amount,
            BalanceAfter = e.NewBalance.Amount,
            Description = e.Description,
            TransactionDate = e.DrawnAt,
            CreatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_revolving_transactions", transaction, "id", ct);

        _logger.LogDebug("Projected FundsDrawn for {CreditLineId}: {Amount}", e.AggregateId, e.Amount.Amount);
    }

    private async Task HandlePaymentApplied(RevolvingPaymentApplied e, CancellationToken ct)
    {
        // Actualizar resumen
        const string updateSql = @"
            UPDATE rm_revolving_credit_summaries 
            SET current_balance = @NewBalance,
                available_credit = @AvailableCredit,
                accrued_interest = GREATEST(0, accrued_interest - @InterestPaid),
                pending_fees = GREATEST(0, pending_fees - @FeesPaid),
                consecutive_missed_payments = 0,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(updateSql, new
        {
            CreditLineId = e.AggregateId,
            NewBalance = e.NewBalance.Amount,
            AvailableCredit = e.AvailableCredit.Amount,
            InterestPaid = e.InterestPaid.Amount,
            FeesPaid = e.FeesPaid.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        // Insertar transacción
        var transaction = new RevolvingTransactionReadModel
        {
            Id = e.PaymentId,
            CreditLineId = e.AggregateId,
            TransactionType = "Payment",
            Amount = -e.TotalAmount.Amount, // Negativo porque reduce el balance
            BalanceAfter = e.NewBalance.Amount,
            Description = $"Payment via {e.Method}",
            Reference = e.PaymentId.ToString(),
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_revolving_transactions", transaction, "id", ct);

        // Verificar si hay estado de cuenta pendiente y marcarlo como pagado
        await TryMarkStatementAsPaid(e.AggregateId, e.TotalAmount.Amount, ct);

        _logger.LogDebug("Projected RevolvingPaymentApplied for {CreditLineId}: {Amount}", 
            e.AggregateId, e.TotalAmount.Amount);
    }

    private async Task HandleInterestAccrued(RevolvingInterestAccrued e, CancellationToken ct)
    {
        const string updateSql = @"
            UPDATE rm_revolving_credit_summaries 
            SET accrued_interest = accrued_interest + @Amount,
                last_interest_accrual_date = @PeriodEnd,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(updateSql, new
        {
            CreditLineId = e.AggregateId,
            Amount = e.Amount.Amount,
            PeriodEnd = e.PeriodEnd,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        // Insertar transacción de interés
        var transaction = new RevolvingTransactionReadModel
        {
            Id = Guid.NewGuid(),
            CreditLineId = e.AggregateId,
            TransactionType = "Interest",
            Amount = e.Amount.Amount,
            BalanceAfter = 0, // Se actualizará en la próxima lectura
            Description = $"Interest accrued from {e.PeriodStart:d} to {e.PeriodEnd:d}",
            TransactionDate = e.PeriodEnd,
            CreatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_revolving_transactions", transaction, "id", ct);

        _logger.LogDebug("Projected RevolvingInterestAccrued for {CreditLineId}: {Amount}", 
            e.AggregateId, e.Amount.Amount);
    }

    private async Task HandleStatementGenerated(StatementGenerated e, CancellationToken ct)
    {
        // Actualizar resumen
        const string updateSql = @"
            UPDATE rm_revolving_credit_summaries 
            SET last_statement_date = @StatementDate,
                next_statement_date = @NextStatementDate,
                payment_due_date = @DueDate,
                current_minimum_payment = @MinimumPayment,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(updateSql, new
        {
            CreditLineId = e.AggregateId,
            StatementDate = e.StatementDate,
            NextStatementDate = e.StatementDate.AddMonths(1),
            DueDate = e.DueDate,
            MinimumPayment = e.MinimumPayment.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        // Insertar estado de cuenta
        var statement = new RevolvingStatementReadModel
        {
            StatementId = e.StatementId,
            CreditLineId = e.AggregateId,
            StatementDate = e.StatementDate,
            DueDate = e.DueDate,
            PreviousBalance = e.PreviousBalance.Amount,
            Purchases = e.Purchases.Amount,
            Payments = e.Payments.Amount,
            InterestCharged = e.InterestCharged.Amount,
            FeesCharged = e.FeesCharged.Amount,
            NewBalance = e.NewBalance.Amount,
            MinimumPayment = e.MinimumPayment.Amount,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_revolving_statements", statement, "statement_id", ct);

        _logger.LogDebug("Projected StatementGenerated for {CreditLineId}: Statement {StatementId}", 
            e.AggregateId, e.StatementId);
    }

    private async Task HandleCreditLineFrozen(CreditLineFrozen e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_revolving_credit_summaries 
            SET status = @Status,
                frozen_at = @FrozenAt,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = e.AggregateId,
            Status = RevolvingCreditStatus.Frozen.ToString(),
            FrozenAt = e.FrozenAt,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected CreditLineFrozen for {CreditLineId}", e.AggregateId);
    }

    private async Task HandleCreditLineUnfrozen(CreditLineUnfrozen e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_revolving_credit_summaries 
            SET status = @Status,
                frozen_at = NULL,
                consecutive_missed_payments = 0,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = e.AggregateId,
            Status = RevolvingCreditStatus.Active.ToString(),
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected CreditLineUnfrozen for {CreditLineId}", e.AggregateId);
    }

    private async Task HandleCreditLimitChanged(CreditLimitChanged e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_revolving_credit_summaries 
            SET credit_limit = @NewLimit,
                available_credit = @NewLimit - current_balance,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = e.AggregateId,
            NewLimit = e.NewLimit.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected CreditLimitChanged for {CreditLineId}: {Previous} -> {New}", 
            e.AggregateId, e.PreviousLimit.Amount, e.NewLimit.Amount);
    }

    private async Task HandleCreditLineClosed(CreditLineClosed e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_revolving_credit_summaries 
            SET status = @Status,
                available_credit = 0,
                closed_at = @ClosedAt,
                version = @Version,
                updated_at = @Now
            WHERE credit_line_id = @CreditLineId";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = e.AggregateId,
            Status = RevolvingCreditStatus.Closed.ToString(),
            ClosedAt = e.ClosedAt,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected CreditLineClosed for {CreditLineId}", e.AggregateId);
    }

    private async Task TryMarkStatementAsPaid(Guid creditLineId, decimal paymentAmount, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_revolving_statements 
            SET is_paid = TRUE,
                paid_at = @Now
            WHERE credit_line_id = @CreditLineId
            AND is_paid = FALSE
            AND minimum_payment <= @PaymentAmount
            AND due_date >= @Now";

        await _store.ExecuteAsync(sql, new
        {
            CreditLineId = creditLineId,
            PaymentAmount = paymentAmount,
            Now = DateTime.UtcNow
        }, ct);
    }

    private DateTime CalculateNextStatementDate(DateTime fromDate)
    {
        // Por defecto, primer día del siguiente mes
        return new DateTime(fromDate.Year, fromDate.Month, 1).AddMonths(1);
    }
}