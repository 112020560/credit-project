using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class UnderwritingPolicyRepository : IUnderwritingPolicyRepository
{
    private readonly string _connectionString;

    public UnderwritingPolicyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private const string SelectColumns = """
        SELECT id, base_interest_rate, auto_default_threshold_days, no_score_behavior,
               shares_multiplier_limit, require_active_membership,
               grace_period_days, penalty_rate, origination_fee_rate,
               enforce_shares_capacity_limit, max_dti_ratio
        FROM underwriting_policies
        """;

    public async Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default)
    {
        var sql = SelectColumns + " WHERE id = 'default'";

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleAsync(sql);
        return MapToPolicy(row);
    }

    public async Task<UnderwritingPolicy?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var sql = SelectColumns + " WHERE id = @Id";

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Id = id });
        return row == null ? null : MapToPolicy(row);
    }

    public async Task<IEnumerable<UnderwritingPolicy>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(SelectColumns);
        return rows.Select(r => (UnderwritingPolicy)MapToPolicy(r));
    }

    private static UnderwritingPolicy MapToPolicy(dynamic row)
    {
        var noScoreBehavior = ((string)row.no_score_behavior) switch
        {
            "reject" => NoScoreBehavior.Reject,
            _ => NoScoreBehavior.ApproveWithPenalty
        };

        return new UnderwritingPolicy(
            (decimal)row.base_interest_rate,
            (int)row.auto_default_threshold_days,
            noScoreBehavior,
            (int)row.shares_multiplier_limit,
            (bool)row.require_active_membership,
            (int)row.grace_period_days,
            (decimal)row.penalty_rate,
            (decimal)row.origination_fee_rate,
            (bool)row.enforce_shares_capacity_limit,
            (decimal)row.max_dti_ratio,
            (string)row.id);
    }
}
