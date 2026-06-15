using Crm.Application.Abstractions.Messaging;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record ActivateRiskMatrixCommand(Guid MatrixId) : ICommand;

internal sealed class ActivateRiskMatrixCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<ActivateRiskMatrixCommand>
{
    public async Task<Result> Handle(ActivateRiskMatrixCommand request, CancellationToken cancellationToken)
    {
        var matrix = await unitOfWork.RiskMatrixRepository.GetByIdAsync(request.MatrixId, cancellationToken);
        if (matrix is null)
            return Result.Failure(RiskMatrixError.NotFound(request.MatrixId));

        if (matrix.Status != RiskMatrixStatus.Draft)
            return Result.Failure(RiskMatrixError.NotDraft);

        var current = await unitOfWork.RiskMatrixRepository.GetActiveAsync(cancellationToken);
        if (current is not null)
        {
            current.Status = RiskMatrixStatus.Superseded;
            await unitOfWork.RiskMatrixRepository.UpdateAsync(current, cancellationToken);
        }

        matrix.Status = RiskMatrixStatus.Active;
        await unitOfWork.RiskMatrixRepository.UpdateAsync(matrix, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class ActivateRiskMatrixCommandValidator : AbstractValidator<ActivateRiskMatrixCommand>
{
    public ActivateRiskMatrixCommandValidator()
    {
        RuleFor(x => x.MatrixId).NotEmpty();
    }
}
