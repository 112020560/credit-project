using CreditSystem.Application.Commands.SyncMemberFromCrm;
using MassTransit;
using MediatR;
using SharedKernel.Contracts.Crm.Members;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;

public class MemberSyncedConsumer : IConsumer<MemberSynced>
{
    private readonly IMediator _mediator;

    public MemberSyncedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<MemberSynced> context)
    {
        var message = context.Message;

        var command = new SyncMemberFromCrmCommand
        {
            ExternalId = message.ExternalId,
            MemberNumber = message.MemberNumber,
            Status = message.Status,
            JoinedAt = message.JoinedAt,
            TotalSharesAmount = message.TotalSharesAmount,
            SharesCurrency = message.SharesCurrency,
            NumberOfContributions = message.NumberOfContributions,
            LastContributionDate = message.LastContributionDate
        };

        await _mediator.Send(command, context.CancellationToken);
    }
}
