using Credit.Application.Abstractions.Messaging;
using Credit.Application.Abstractions.Persistence;
using Credit.Domain.Entities;
using SharedKernel;

namespace Credit.Application.Commands;

public record OpenCreditLineCommand(
    Guid? ApplicationId,
    Guid CustomerId,
    Guid ProductId,
    decimal Principal,
    decimal Outstanding,
    string Currency,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    string? AmortizationSchedule,
    string? Metadata
) : ICommand<Guid>;


internal sealed class OpenCreditLineHandler(
    ICreditLineRepository creditLineRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<OpenCreditLineCommand, Guid>
{
    public async Task<Result<Guid>> Handle(OpenCreditLineCommand request, CancellationToken cancellationToken)
    {
        var creditLine = new CreditLine
        {
            Id = Guid.NewGuid(),
            ApplicationId = request.ApplicationId,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Principal = request.Principal,
            Outstanding = request.Outstanding,
            Currency = request.Currency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            AmortizationSchedule = request.AmortizationSchedule,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await creditLineRepository.AddAsync(creditLine, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(creditLine.Id);
    }
}
