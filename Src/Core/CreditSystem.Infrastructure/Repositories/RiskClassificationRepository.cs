using CreditSystem.Domain.Abstractions.Repositories;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class RiskClassificationRepository : IRiskClassificationRepository
{
    private readonly string _connectionString;

    public RiskClassificationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task UpdateRiskCategoryAsync(Guid loanId, string category, decimal provision, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE rm_loan_summaries
            SET risk_category      = @Category,
                estimated_provision = @Provision,
                updated_at          = NOW()
            WHERE loan_id = @LoanId";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            LoanId = loanId,
            Category = category,
            Provision = provision
        }, cancellationToken: ct));
    }
}
