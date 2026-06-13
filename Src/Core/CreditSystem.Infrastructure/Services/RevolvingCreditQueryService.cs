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

    public async Task<RevolvingCreditSummaryReadModel?> GetSummaryAsync(Guid creditLineId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM rm_revolving_credit_summaries WHERE credit_line_id = @CreditLineId";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<RevolvingCreditSummaryReadModel>(sql, new { CreditLineId = creditLineId });
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_credit_summaries 
            WHERE customer_id = @CustomerId 
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql, new { CustomerId = customerId });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetActiveForInterestAccrualAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_credit_summaries 
            WHERE status IN ('Active', 'Frozen')
            AND current_balance > 0";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql);
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetDueForStatementAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_credit_summaries 
            WHERE status IN ('Active', 'Frozen')
            AND next_statement_date <= @AsOfDate";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingCreditSummaryReadModel>(sql, new { AsOfDate = asOfDate });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingTransactionReadModel>> GetTransactionsAsync(Guid creditLineId, int limit = 50, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_transactions 
            WHERE credit_line_id = @CreditLineId 
            ORDER BY transaction_date DESC 
            LIMIT @Limit";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingTransactionReadModel>(sql, new { CreditLineId = creditLineId, Limit = limit });
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RevolvingStatementReadModel>> GetStatementsAsync(Guid creditLineId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_statements 
            WHERE credit_line_id = @CreditLineId 
            ORDER BY statement_date DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingStatementReadModel>(sql, new { CreditLineId = creditLineId });
        return results.ToList().AsReadOnly();
    }

    public async Task<RevolvingStatementReadModel?> GetLatestStatementAsync(Guid creditLineId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_statements 
            WHERE credit_line_id = @CreditLineId 
            ORDER BY statement_date DESC 
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<RevolvingStatementReadModel>(sql, new { CreditLineId = creditLineId });
    }

    public async Task<IReadOnlyList<RevolvingStatementReadModel>> GetUnpaidStatementsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM rm_revolving_statements 
            WHERE is_paid = FALSE 
            AND due_date < @Now
            ORDER BY due_date ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<RevolvingStatementReadModel>(sql, new { Now = DateTime.UtcNow });
        return results.ToList().AsReadOnly();
    }
}