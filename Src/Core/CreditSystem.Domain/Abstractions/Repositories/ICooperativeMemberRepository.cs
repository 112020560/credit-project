using CreditSystem.Domain.Aggregates.CooperativeMember;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface ICooperativeMemberRepository
{
    Task<CooperativeMemberAggregate?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
    Task<CooperativeMemberAggregate?> GetByMemberNumberAsync(string memberNumber, CancellationToken ct = default);
    Task UpsertAsync(
        Guid externalId,
        string memberNumber,
        MemberStatus status,
        DateTime joinedAt,
        MemberShare shares,
        CancellationToken ct = default);

    Task<decimal?> GetSocialCapitalBalanceAsync(Guid externalId, CancellationToken ct = default);
}
