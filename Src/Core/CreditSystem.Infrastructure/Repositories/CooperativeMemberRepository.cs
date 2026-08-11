using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.CooperativeMember;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class CooperativeMemberRepository : ICooperativeMemberRepository
{
    private readonly string _connectionString;

    public CooperativeMemberRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CooperativeMemberAggregate?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, external_id, member_number, status, joined_at,
                   total_shares_amount, shares_currency, number_of_contributions, last_contribution_date
            FROM cooperative_members
            WHERE external_id = @ExternalId
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { ExternalId = externalId });

        return row == null ? null : MapToAggregate(row);
    }

    public async Task<CooperativeMemberAggregate?> GetByMemberNumberAsync(string memberNumber, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, external_id, member_number, status, joined_at,
                   total_shares_amount, shares_currency, number_of_contributions, last_contribution_date
            FROM cooperative_members
            WHERE member_number = @MemberNumber
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { MemberNumber = memberNumber });

        return row == null ? null : MapToAggregate(row);
    }

    public async Task UpsertAsync(
        Guid externalId,
        string memberNumber,
        MemberStatus status,
        DateTime joinedAt,
        MemberShare shares,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO cooperative_members
                (external_id, member_number, status, joined_at,
                 total_shares_amount, shares_currency, number_of_contributions, last_contribution_date,
                 created_at, updated_at)
            VALUES
                (@ExternalId, @MemberNumber, @Status, @JoinedAt,
                 @TotalSharesAmount, @SharesCurrency, @NumberOfContributions, @LastContributionDate,
                 NOW(), NOW())
            ON CONFLICT (external_id) DO UPDATE SET
                member_number           = @MemberNumber,
                status                  = @Status,
                joined_at               = @JoinedAt,
                total_shares_amount     = @TotalSharesAmount,
                shares_currency         = @SharesCurrency,
                number_of_contributions = @NumberOfContributions,
                last_contribution_date  = @LastContributionDate,
                updated_at              = NOW()
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ExternalId = externalId,
            MemberNumber = memberNumber,
            Status = status.ToString(),
            JoinedAt = joinedAt,
            TotalSharesAmount = shares.TotalAmount.Amount,
            SharesCurrency = shares.TotalAmount.Currency,
            NumberOfContributions = shares.NumberOfContributions,
            LastContributionDate = shares.LastContributionDate
        }, cancellationToken: ct));
    }

    public async Task<decimal?> GetSocialCapitalBalanceAsync(Guid externalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT social_capital_balance
            FROM cooperative_members
            WHERE external_id = @ExternalId
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<decimal?>(sql, new { ExternalId = externalId });
    }

    private static CooperativeMemberAggregate MapToAggregate(dynamic row)
    {
        var status = Enum.TryParse<MemberStatus>((string)row.status, out var s) ? s : MemberStatus.Active;
        var shares = new MemberShare(
            new Money((decimal)row.total_shares_amount, (string)row.shares_currency),
            (int)row.number_of_contributions,
            (DateTime?)row.last_contribution_date);

        var state = new MemberState
        {
            Id = (Guid)row.id,
            ExternalId = (Guid)row.external_id,
            MemberNumber = (string)row.member_number,
            Status = status,
            JoinedAt = (DateTime)row.joined_at,
            Shares = shares
        };

        return new CooperativeMemberAggregate(state, Enumerable.Empty<Domain.Abstractions.Events.IDomainEvent>());
    }
}
