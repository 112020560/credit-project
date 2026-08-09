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

    public async Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT base_interest_rate, auto_default_threshold_days, no_score_behavior,
                   shares_multiplier_limit, require_active_membership,
                   grace_period_days, penalty_rate, origination_fee_rate
            FROM underwriting_policies
            WHERE id = 'default'
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleAsync<(
            decimal BaseInterestRate,
            int AutoDefaultThresholdDays,
            string NoScoreBehavior,
            int SharesMultiplierLimit,
            bool RequireActiveMembership,
            int GracePeriodDays,
            decimal PenaltyRate,
            decimal OriginationFeeRate)>(sql);

        var noScoreBehavior = row.NoScoreBehavior switch
        {
            "reject" => NoScoreBehavior.Reject,
            _ => NoScoreBehavior.ApproveWithPenalty
        };

        return new UnderwritingPolicy(
            row.BaseInterestRate,
            row.AutoDefaultThresholdDays,
            noScoreBehavior,
            row.SharesMultiplierLimit,
            row.RequireActiveMembership,
            row.GracePeriodDays,
            row.PenaltyRate,
            row.OriginationFeeRate);
    }
}
