using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Models.ReadModels;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class PaymentTrackingRepository : IPaymentTrackingRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PaymentTrackingRepository> _logger;

    public PaymentTrackingRepository(
        IConfiguration configuration,
        ILogger<PaymentTrackingRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task<PaymentTrackingReadModel?> GetByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                payment_id as PaymentId,
                loan_id as LoanId,
                credit_line_id as CreditLineId,
                customer_id as CustomerId,
                amount as Amount,
                currency as Currency,
                payment_method as PaymentMethod,
                status as StatusStr,
                error_message as ErrorMessage,
                error_code as ErrorCode,
                principal_paid as PrincipalPaid,
                interest_paid as InterestPaid,
                fees_paid as FeesPaid,
                new_balance as NewBalance,
                new_available_credit as NewAvailableCredit,
                is_paid_off as IsPaidOff,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                processed_at as ProcessedAt,
                correlation_id as CorrelationId
            FROM rm_payment_tracking
            WHERE payment_id = @PaymentId";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dto = await connection.QuerySingleOrDefaultAsync<PaymentTrackingDto>(
            new CommandDefinition(sql, new { PaymentId = paymentId }, cancellationToken: cancellationToken));

        return dto?.ToReadModel();
    }

    public async Task<IReadOnlyList<PaymentTrackingReadModel>> GetByLoanIdAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                payment_id as PaymentId,
                loan_id as LoanId,
                credit_line_id as CreditLineId,
                customer_id as CustomerId,
                amount as Amount,
                currency as Currency,
                payment_method as PaymentMethod,
                status as StatusStr,
                error_message as ErrorMessage,
                error_code as ErrorCode,
                principal_paid as PrincipalPaid,
                interest_paid as InterestPaid,
                fees_paid as FeesPaid,
                new_balance as NewBalance,
                new_available_credit as NewAvailableCredit,
                is_paid_off as IsPaidOff,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                processed_at as ProcessedAt,
                correlation_id as CorrelationId
            FROM rm_payment_tracking
            WHERE loan_id = @LoanId
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dtos = await connection.QueryAsync<PaymentTrackingDto>(
            new CommandDefinition(sql, new { LoanId = loanId }, cancellationToken: cancellationToken));

        return dtos.Select(d => d.ToReadModel()).ToList();
    }

    public async Task<IReadOnlyList<PaymentTrackingReadModel>> GetByCreditLineIdAsync(
        Guid creditLineId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                payment_id as PaymentId,
                loan_id as LoanId,
                credit_line_id as CreditLineId,
                customer_id as CustomerId,
                amount as Amount,
                currency as Currency,
                payment_method as PaymentMethod,
                status as StatusStr,
                error_message as ErrorMessage,
                error_code as ErrorCode,
                principal_paid as PrincipalPaid,
                interest_paid as InterestPaid,
                fees_paid as FeesPaid,
                new_balance as NewBalance,
                new_available_credit as NewAvailableCredit,
                is_paid_off as IsPaidOff,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                processed_at as ProcessedAt,
                correlation_id as CorrelationId
            FROM rm_payment_tracking
            WHERE credit_line_id = @CreditLineId
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dtos = await connection.QueryAsync<PaymentTrackingDto>(
            new CommandDefinition(sql, new { CreditLineId = creditLineId }, cancellationToken: cancellationToken));

        return dtos.Select(d => d.ToReadModel()).ToList();
    }

    public async Task<IReadOnlyList<PaymentTrackingReadModel>> GetByCustomerIdAsync(
        Guid customerId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                payment_id as PaymentId,
                loan_id as LoanId,
                credit_line_id as CreditLineId,
                customer_id as CustomerId,
                amount as Amount,
                currency as Currency,
                payment_method as PaymentMethod,
                status as StatusStr,
                error_message as ErrorMessage,
                error_code as ErrorCode,
                principal_paid as PrincipalPaid,
                interest_paid as InterestPaid,
                fees_paid as FeesPaid,
                new_balance as NewBalance,
                new_available_credit as NewAvailableCredit,
                is_paid_off as IsPaidOff,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                processed_at as ProcessedAt,
                correlation_id as CorrelationId
            FROM rm_payment_tracking
            WHERE customer_id = @CustomerId
            ORDER BY created_at DESC
            LIMIT @Limit";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dtos = await connection.QueryAsync<PaymentTrackingDto>(
            new CommandDefinition(sql, new { CustomerId = customerId, Limit = limit },
                cancellationToken: cancellationToken));

        return dtos.Select(d => d.ToReadModel()).ToList();
    }

    public async Task CreateAsync(PaymentTrackingReadModel tracking, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO rm_payment_tracking
                (id, payment_id, loan_id, credit_line_id, customer_id, amount, currency,
                 payment_method, status, created_at, updated_at, correlation_id)
            VALUES
                (@Id, @PaymentId, @LoanId, @CreditLineId, @CustomerId, @Amount, @Currency,
                 @PaymentMethod, @Status, @CreatedAt, @UpdatedAt, @CorrelationId)";

        tracking.Id = tracking.Id == Guid.Empty ? Guid.NewGuid() : tracking.Id;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            tracking.Id,
            tracking.PaymentId,
            tracking.LoanId,
            tracking.CreditLineId,
            tracking.CustomerId,
            tracking.Amount,
            tracking.Currency,
            tracking.PaymentMethod,
            Status = tracking.Status.ToString().ToUpperInvariant(),
            tracking.CreatedAt,
            tracking.UpdatedAt,
            tracking.CorrelationId
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Payment tracking record created for payment {PaymentId}", tracking.PaymentId);
    }

    public async Task UpdateStatusAsync(
        Guid paymentId,
        PaymentTrackingStatus status,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = @Status,
                error_code = @ErrorCode,
                error_message = @ErrorMessage,
                updated_at = @UpdatedAt
            WHERE payment_id = @PaymentId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            PaymentId = paymentId,
            Status = status.ToString().ToUpperInvariant(),
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Payment tracking {PaymentId} status updated to {Status}", paymentId, status);
    }

    private class PaymentTrackingDto
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public Guid? LoanId { get; set; }
        public Guid? CreditLineId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MXN";
        public string PaymentMethod { get; set; } = string.Empty;
        public string StatusStr { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public decimal? PrincipalPaid { get; set; }
        public decimal? InterestPaid { get; set; }
        public decimal? FeesPaid { get; set; }
        public decimal? NewBalance { get; set; }
        public decimal? NewAvailableCredit { get; set; }
        public bool? IsPaidOff { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }
        public Guid CorrelationId { get; set; }

        public PaymentTrackingReadModel ToReadModel() => new()
        {
            Id = Id,
            PaymentId = PaymentId,
            LoanId = LoanId,
            CreditLineId = CreditLineId,
            CustomerId = CustomerId,
            Amount = Amount,
            Currency = Currency,
            PaymentMethod = PaymentMethod,
            Status = Enum.Parse<PaymentTrackingStatus>(StatusStr, ignoreCase: true),
            ErrorMessage = ErrorMessage,
            ErrorCode = ErrorCode,
            PrincipalPaid = PrincipalPaid,
            InterestPaid = InterestPaid,
            FeesPaid = FeesPaid,
            NewBalance = NewBalance,
            NewAvailableCredit = NewAvailableCredit,
            IsPaidOff = IsPaidOff,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            ProcessedAt = ProcessedAt,
            CorrelationId = CorrelationId
        };
    }
}
