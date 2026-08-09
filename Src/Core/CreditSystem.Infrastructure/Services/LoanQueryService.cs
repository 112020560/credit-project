using CreditSystem.Application.Configuration;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Models.ReadModels;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CreditSystem.Infrastructure.Services;

public class LoanQueryService: ILoanQueryService
{
    private readonly string _connectionString;
    private readonly LateFeeConfiguration _lateFeeConfig;
    private readonly UnderwritingPolicy _policy;

    public LoanQueryService(string connectionString, IOptions<LateFeeConfiguration> lateFeeConfig, UnderwritingPolicy policy)
    {
        _connectionString = connectionString;
        _lateFeeConfig = lateFeeConfig.Value;
        _policy = policy;
    }
    public async Task<bool> HasActiveLoansAsync(Guid customerId, CancellationToken ct = default)
    {
        const string sql = @"
        SELECT EXISTS(
            SELECT 1 FROM rm_loan_summaries 
            WHERE customer_id = @CustomerId 
            AND status IN ('Active', 'Delinquent')
        )";
    
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<bool>(sql, new { CustomerId = customerId });
    }
    
    public async Task<LoanSummaryReadModel?> GetLoanSummaryAsync(Guid loanId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                loan_id               AS LoanId,
                customer_id           AS CustomerId,
                customer_name         AS CustomerName,
                principal             AS Principal,
                current_balance       AS CurrentBalance,
                accrued_interest      AS AccruedInterest,
                total_fees            AS TotalFees,
                origination_fee       AS OriginationFee,
                accrued_penalty_interest AS AccruedPenaltyInterest,
                interest_rate         AS InterestRate,
                term_months           AS TermMonths,
                status                AS Status,
                payments_made         AS PaymentsMade,
                payments_missed       AS PaymentsMissed,
                next_payment_date     AS NextPaymentDate,
                next_payment_amount   AS NextPaymentAmount,
                disbursed_at          AS DisbursedAt,
                created_at            AS CreatedAt,
                last_payment_at       AS LastPaymentAt,
                defaulted_at          AS DefaultedAt,
                paid_off_at           AS PaidOffAt,
                version               AS Version,
                updated_at            AS UpdatedAt
            FROM rm_loan_summaries
            WHERE loan_id = @LoanId";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<LoanSummaryReadModel>(sql, new { LoanId = loanId });
    }

    public async Task<IReadOnlyList<LoanSummaryReadModel>> GetCustomerLoansAsync(Guid customerId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                loan_id               AS LoanId,
                customer_id           AS CustomerId,
                customer_name         AS CustomerName,
                principal             AS Principal,
                current_balance       AS CurrentBalance,
                accrued_interest      AS AccruedInterest,
                total_fees            AS TotalFees,
                origination_fee       AS OriginationFee,
                accrued_penalty_interest AS AccruedPenaltyInterest,
                interest_rate         AS InterestRate,
                term_months           AS TermMonths,
                status                AS Status,
                payments_made         AS PaymentsMade,
                payments_missed       AS PaymentsMissed,
                next_payment_date     AS NextPaymentDate,
                next_payment_amount   AS NextPaymentAmount,
                disbursed_at          AS DisbursedAt,
                created_at            AS CreatedAt,
                last_payment_at       AS LastPaymentAt,
                defaulted_at          AS DefaultedAt,
                paid_off_at           AS PaidOffAt,
                version               AS Version,
                updated_at            AS UpdatedAt
            FROM rm_loan_summaries
            WHERE customer_id = @CustomerId
            ORDER BY created_at DESC";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<LoanSummaryReadModel>(sql, new { CustomerId = customerId });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<DelinquentLoanReadModel>> GetDelinquentLoansAsync(
        int? minDaysOverdue = null, 
        string? collectionStatus = null,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                loan_id              AS LoanId,
                customer_id          AS CustomerId,
                customer_name        AS CustomerName,
                customer_phone       AS CustomerPhone,
                customer_email       AS CustomerEmail,
                principal            AS Principal,
                current_balance      AS CurrentBalance,
                total_owed           AS TotalOwed,
                days_overdue         AS DaysOverdue,
                payments_missed      AS PaymentsMissed,
                last_payment_at      AS LastPaymentAt,
                next_action_date     AS NextActionDate,
                collection_status    AS CollectionStatus,
                assigned_collector   AS AssignedCollector,
                notes                AS Notes,
                created_at           AS CreatedAt,
                updated_at           AS UpdatedAt
            FROM rm_delinquent_loans
            WHERE 1=1";
        var parameters = new DynamicParameters();

        if (minDaysOverdue.HasValue)
        {
            sql += " AND days_overdue >= @MinDays";
            parameters.Add("MinDays", minDaysOverdue.Value);
        }

        if (!string.IsNullOrEmpty(collectionStatus))
        {
            sql += " AND collection_status = @Status";
            parameters.Add("Status", collectionStatus);
        }

        sql += " ORDER BY days_overdue DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<DelinquentLoanReadModel>(sql, parameters);

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<PaymentHistoryReadModel>> GetPaymentHistoryAsync(
        Guid loanId, 
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id             AS Id,
                loan_id        AS LoanId,
                payment_number AS PaymentNumber,
                payment_date   AS PaymentDate,
                total_amount   AS TotalAmount,
                principal_paid AS PrincipalPaid,
                interest_paid  AS InterestPaid,
                fees_paid      AS FeesPaid,
                balance_after  AS BalanceAfter,
                payment_method AS PaymentMethod,
                status         AS Status
            FROM rm_payment_history
            WHERE loan_id = @LoanId
            ORDER BY payment_date DESC";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<PaymentHistoryReadModel>(sql, new { LoanId = loanId });
        return results.ToList().AsReadOnly();
    }

    public async Task<LoanPortfolioReadModel?> GetPortfolioSummaryAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id                        AS Id,
                total_loans               AS TotalLoans,
                active_loans              AS ActiveLoans,
                delinquent_loans          AS DelinquentLoans,
                defaulted_loans           AS DefaultedLoans,
                paid_off_loans            AS PaidOffLoans,
                total_principal           AS TotalPrincipal,
                total_outstanding         AS TotalOutstanding,
                total_interest_accrued    AS TotalInterestAccrued,
                total_collected_principal AS TotalCollectedPrincipal,
                total_collected_interest  AS TotalCollectedInterest,
                total_collected_fees      AS TotalCollectedFees,
                average_interest_rate     AS AverageInterestRate,
                delinquency_rate          AS DelinquencyRate,
                default_rate              AS DefaultRate,
                updated_at                AS UpdatedAt
            FROM rm_loan_portfolio
            WHERE id = 'global'";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<LoanPortfolioReadModel>(sql);
    }

    public async Task<IReadOnlyList<UpcomingPaymentReadModel>> GetUpcomingPaymentsAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id             AS Id,
                loan_id        AS LoanId,
                customer_id    AS CustomerId,
                customer_name  AS CustomerName,
                customer_email AS CustomerEmail,
                payment_number AS PaymentNumber,
                due_date       AS DueDate,
                amount_due     AS AmountDue,
                principal_due  AS PrincipalDue,
                interest_due   AS InterestDue,
                is_overdue     AS IsOverdue,
                days_until_due AS DaysUntilDue,
                reminder_sent  AS ReminderSent
            FROM rm_upcoming_payments
            WHERE due_date BETWEEN @FromDate AND @ToDate
            ORDER BY due_date ASC";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<UpcomingPaymentReadModel>(sql, new { FromDate = fromDate, ToDate = toDate });
        return results.ToList().AsReadOnly();
    }
    
    public async Task<IReadOnlyList<ActiveLoanForAccrual>> GetActiveLoansForAccrualAsync(CancellationToken ct = default)
    {
        const string sql = @"
        SELECT 
            loan_id as LoanId,
            customer_id as CustomerId,
            current_balance as CurrentBalance,
            interest_rate as InterestRate
        FROM rm_loan_summaries 
        WHERE status IN ('Active', 'Delinquent')
        AND current_balance > 0";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<ActiveLoanForAccrual>(sql);
    
        return results.ToList().AsReadOnly();
    }
    
    public async Task<IReadOnlyList<OverdueLoanInfo>> GetLoansWithOverduePaymentsAsync(CancellationToken ct = default)
    {
        const string sql = @"
        SELECT
            ls.loan_id as LoanId,
            ls.customer_id as CustomerId,
            ls.payments_made + 1 as PaymentNumber,
            ls.next_payment_date as DueDate,
            ls.next_payment_amount as AmountDue,
            'USD' as Currency,
            EXTRACT(DAY FROM NOW() - ls.next_payment_date)::INT as DaysOverdue,
            FALSE as AlreadyRecorded
        FROM rm_loan_summaries ls
        WHERE ls.status IN ('Active', 'Delinquent')
        AND ls.next_payment_date < @CutoffDate
        AND ls.current_balance > 0
        AND NOT EXISTS (
            SELECT 1 FROM rm_payment_history ph
            WHERE ph.loan_id = ls.loan_id
            AND ph.payment_number = ls.payments_made + 1
        )
        ORDER BY ls.next_payment_date ASC";

        await using var connection = new NpgsqlConnection(_connectionString);

        // Calcular fecha de corte usando grace period de la política de suscripción
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-_policy.GracePeriodDays);

        var results = await connection.QueryAsync<OverdueLoanInfo>(
            sql, new { CutoffDate = cutoffDate });

        return results.ToList().AsReadOnly();
    }
    
    public async Task<IReadOnlyList<DefaultedLoanReadModel>> GetDefaultedLoansAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var sql = @"
        SELECT 
            loan_id as LoanId,
            customer_id as CustomerId,
            customer_name as CustomerName,
            principal as Principal,
            current_balance as OutstandingBalance,
            accrued_interest as AccruedInterest,
            total_fees as TotalFees,
            (current_balance + accrued_interest + total_fees) as TotalOwed,
            payments_missed as PaymentsMissed,
            last_payment_at as LastPaymentAt,
            defaulted_at as DefaultedAt,
            created_at as CreatedAt
        FROM rm_loan_summaries 
        WHERE status = 'Default'";

        var parameters = new DynamicParameters();

        if (fromDate.HasValue)
        {
            sql += " AND defaulted_at >= @FromDate";
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND defaulted_at <= @ToDate";
            parameters.Add("ToDate", toDate.Value);
        }

        sql += " ORDER BY defaulted_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<DefaultedLoanReadModel>(sql, parameters);

        return results.ToList().AsReadOnly();
    }
    
    public async Task<IReadOnlyList<PaidOffLoanReadModel>> GetPaidOffLoansAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? earlyPayoffOnly = null,
        CancellationToken ct = default)
    {
        var sql = @"
        SELECT 
            loan_id as LoanId,
            customer_id as CustomerId,
            customer_name as CustomerName,
            principal as Principal,
            0 as TotalInterestPaid,  -- TODO: calcular desde eventos
            0 as TotalFeesPaid,
            payments_made as PaymentsMade,
            term_months as OriginalTermMonths,
            (payments_made < term_months) as EarlyPayoff,
            created_at as CreatedAt,
            paid_off_at as PaidOffAt,
            EXTRACT(DAY FROM paid_off_at - created_at)::INT as DaysToPayoff
        FROM rm_loan_summaries 
        WHERE status = 'PaidOff'";

        var parameters = new DynamicParameters();

        if (fromDate.HasValue)
        {
            sql += " AND paid_off_at >= @FromDate";
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND paid_off_at <= @ToDate";
            parameters.Add("ToDate", toDate.Value);
        }

        if (earlyPayoffOnly == true)
        {
            sql += " AND payments_made < term_months";
        }

        sql += " ORDER BY paid_off_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<PaidOffLoanReadModel>(sql, parameters);

        return results.ToList().AsReadOnly();
    }
}