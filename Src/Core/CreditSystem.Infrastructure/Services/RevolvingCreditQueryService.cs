using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models.ReadModels;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Services;

public class RevolvingCreditQueryService : IRevolvingCreditQueryService
{
    private readonly string _connectionString;

    public RevolvingCreditQueryService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private const string RevolvingSummaryCols = @"
        credit_line_id                AS CreditLineId,
        customer_id                   AS CustomerId,
        customer_name                 AS CustomerName,
        status                        AS Status,
        credit_limit                  AS CreditLimit,
        current_balance               AS CurrentBalance,
        available_credit              AS AvailableCredit,
        accrued_interest              AS AccruedInterest,
        pending_fees                  AS PendingFees,
        interest_rate                 AS InterestRate,
        minimum_payment_percentage    AS MinimumPaymentPercentage,
        minimum_payment_amount        AS MinimumPaymentAmount,
        billing_cycle_day             AS BillingCycleDay,
        grace_period_days             AS GracePeriodDays,
        activated_at                  AS ActivatedAt,
        last_statement_date           AS LastStatementDate,
        next_statement_date           AS NextStatementDate,
        payment_due_date              AS PaymentDueDate,
        current_minimum_payment       AS CurrentMinimumPayment,
        consecutive_missed_payments   AS ConsecutiveMissedPayments,
        last_interest_accrual_date    AS LastInterestAccrualDate,
        frozen_at                     AS FrozenAt,
        closed_at                     AS ClosedAt,
        currency                      AS Currency,
        version                       AS Version,
        created_at                    AS CreatedAt,
        updated_at                    AS UpdatedAt";

    private const string RevolvingTransactionCols = @"
        id               AS Id,
        credit_line_id   AS CreditLineId,
        transaction_type AS TransactionType,
        amount           AS Amount,
        balance_after    AS BalanceAfter,
        description      AS Description,
        reference        AS Reference,
        transaction_date AS TransactionDate,
        created_at       AS CreatedAt";

    private const string RevolvingStatementCols = @"
        statement_id      AS StatementId,
        credit_line_id    AS CreditLineId,
        statement_date    AS StatementDate,
        due_date          AS DueDate,
        previous_balance  AS PreviousBalance,
        purchases         AS Purchases,
        payments          AS Payments,
        interest_charged  AS InterestCharged,
        fees_charged      AS FeesCharged,
        new_balance       AS NewBalance,
        minimum_payment   AS MinimumPayment,
        is_paid           AS IsPaid,
        paid_at           AS PaidAt,
        created_at        AS CreatedAt";

    public async Task<RevolvingCreditSummaryReadModel?> GetSummaryAsync(Guid creditLineId, CancellationToken ct = default)
    {
        var sql = $"SELECT {RevolvingSummaryCols} FROM rm_revolving_credit_summaries WHERE credit_line_id = @CreditLineId";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<RevolvingCreditSummaryReadModel>(sql, new { CreditLineId = creditLineId });
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingSummaryCols}
            FROM rm_revolving_credit_summaries
            WHERE customer_id = @CustomerId
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql, new { CustomerId = customerId });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetActiveForInterestAccrualAsync(CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingSummaryCols}
            FROM rm_revolving_credit_summaries
            WHERE status IN ('Active', 'Frozen')
            AND current_balance > 0";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql);
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetDueForStatementAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingSummaryCols}
            FROM rm_revolving_credit_summaries
            WHERE status IN ('Active', 'Frozen')
            AND next_statement_date <= @AsOfDate";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql, new { AsOfDate = asOfDate });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingTransactionReadModel>> GetTransactionsAsync(Guid creditLineId, int limit = 50, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingTransactionCols}
            FROM rm_revolving_transactions
            WHERE credit_line_id = @CreditLineId
            ORDER BY transaction_date DESC
            LIMIT @Limit";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingTransactionReadModel>(sql, new { CreditLineId = creditLineId, Limit = limit });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingStatementReadModel>> GetStatementsAsync(Guid creditLineId, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingStatementCols}
            FROM rm_revolving_statements
            WHERE credit_line_id = @CreditLineId
            ORDER BY statement_date DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingStatementReadModel>(sql, new { CreditLineId = creditLineId });
        return results.ToList().AsReadOnly();
    }

    public async Task<RevolvingStatementReadModel?> GetLatestStatementAsync(Guid creditLineId, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingStatementCols}
            FROM rm_revolving_statements
            WHERE credit_line_id = @CreditLineId
            ORDER BY statement_date DESC
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<RevolvingStatementReadModel>(sql, new { CreditLineId = creditLineId });
    }

    public async Task<IReadOnlyList<RevolvingStatementReadModel>> GetUnpaidStatementsAsync(CancellationToken ct = default)
    {
        var sql = $@"
            SELECT {RevolvingStatementCols}
            FROM rm_revolving_statements
            WHERE is_paid = FALSE
            AND due_date < @Now
            ORDER BY due_date ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingStatementReadModel>(sql, new { Now = DateTime.UtcNow });
        return results.ToList().AsReadOnly();
    }
}
