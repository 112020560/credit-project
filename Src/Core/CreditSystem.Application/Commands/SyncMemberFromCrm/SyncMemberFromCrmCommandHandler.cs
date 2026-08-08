using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.SyncMemberFromCrm;

public class SyncMemberFromCrmCommandHandler : IRequestHandler<SyncMemberFromCrmCommand>
{
    private readonly ICooperativeMemberRepository _repository;
    private readonly ILogger<SyncMemberFromCrmCommandHandler> _logger;

    public SyncMemberFromCrmCommandHandler(
        ICooperativeMemberRepository repository,
        ILogger<SyncMemberFromCrmCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(SyncMemberFromCrmCommand command, CancellationToken cancellationToken)
    {
        var status = Enum.TryParse<MemberStatus>(command.Status, true, out var s)
            ? s
            : MemberStatus.Active;

        var shares = new MemberShare(
            new Money(command.TotalSharesAmount, command.SharesCurrency),
            command.NumberOfContributions,
            command.LastContributionDate);

        await _repository.UpsertAsync(
            externalId: command.ExternalId,
            memberNumber: command.MemberNumber,
            status: status,
            joinedAt: command.JoinedAt,
            shares: shares,
            ct: cancellationToken);

        _logger.LogInformation(
            "Cooperative member synced from CRM for external ID {ExternalId} — member number {MemberNumber}",
            command.ExternalId, command.MemberNumber);
    }
}
