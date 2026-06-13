using Credit.Application.Abstractions.Messaging;
using Credit.Application.UseCases.Applications.Dtos;
using SharedKernel;

namespace Credit.Application.Commands;

public record ApproveCreditApplicationCommand(
        Guid CreditApplicationId,
        ApproveApplicationDto? Body = null
    ) : ICommand<Guid>;

internal sealed class ApproveCreditApplicationHandler: ICommandHandler<ApproveCreditApplicationCommand, Guid>
{
    public Task<Result<Guid>> Handle(ApproveCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}