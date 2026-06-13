using Credit.Application.Abstractions.Messaging;
using SharedKernel;

namespace Credit.Application.Commands;

public record CreateCreditApplicationCommand(
Guid? CustomerId,
Guid? ProductId,
decimal Amount,
int TermMonths
) : ICommand<Guid>;


internal sealed class CreateCreditApplicationHandler : ICommandHandler<CreateCreditApplicationCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}