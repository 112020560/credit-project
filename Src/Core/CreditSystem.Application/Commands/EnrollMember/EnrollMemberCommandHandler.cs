using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.CooperativeMember;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.EnrollMember;

public class EnrollMemberCommandHandler : IRequestHandler<EnrollMemberCommand, EnrollMemberResponse>
{
    private readonly ICustomerReadRepository _customerRepository;
    private readonly ICooperativeMemberRepository _memberRepository;
    private readonly IMemberNumberGenerator _memberNumberGenerator;
    private readonly ILogger<EnrollMemberCommandHandler> _logger;

    public EnrollMemberCommandHandler(
        ICustomerReadRepository customerRepository,
        ICooperativeMemberRepository memberRepository,
        IMemberNumberGenerator memberNumberGenerator,
        ILogger<EnrollMemberCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _memberRepository = memberRepository;
        _memberNumberGenerator = memberNumberGenerator;
        _logger = logger;
    }

    public async Task<EnrollMemberResponse> Handle(EnrollMemberCommand command, CancellationToken cancellationToken)
    {
        // 1. Verify customer exists
        var customer = await _customerRepository.GetByExternalIdAsync(command.ExternalCustomerId, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Enrollment failed: customer {ExternalId} not found in credit profiles", command.ExternalCustomerId);
            return EnrollMemberResponse.Failed(
                "Customer not found. The customer must be registered in the credit system before enrollment.");
        }

        // 2. Verify not already a member
        var existing = await _memberRepository.GetByExternalIdAsync(command.ExternalCustomerId, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning("Enrollment failed: customer {ExternalId} is already a cooperative member", command.ExternalCustomerId);
            return EnrollMemberResponse.Failed("Customer is already a cooperative member.");
        }

        // 3. Generate member number
        var memberNumber = await _memberNumberGenerator.GenerateAsync(cancellationToken);

        // 4. Create aggregate
        var initialShares = new MemberShare(
            new Money(command.InitialSharesAmount, command.SharesCurrency),
            numberOfContributions: command.InitialSharesAmount > 0 ? 1 : 0,
            lastContributionDate: command.InitialSharesAmount > 0 ? command.JoinedAt : null);

        var aggregate = CooperativeMemberAggregate.Register(
            externalId: command.ExternalCustomerId,
            memberNumber: memberNumber,
            joinedAt: command.JoinedAt,
            initialShares: initialShares);

        // 5. Persist
        await _memberRepository.UpsertAsync(
            externalId: command.ExternalCustomerId,
            memberNumber: memberNumber,
            status: MemberStatus.Active,
            joinedAt: command.JoinedAt,
            shares: initialShares,
            ct: cancellationToken);

        _logger.LogInformation(
            "Member enrolled: {MemberNumber} (externalId={ExternalId})",
            memberNumber, command.ExternalCustomerId);

        return EnrollMemberResponse.Enrolled(aggregate.Id, memberNumber, command.ExternalCustomerId, command.JoinedAt);
    }
}
