using MediatR;

namespace CreditSystem.Application.Commands.DefaultContract;

public record DefaultContractCommand : IRequest<DefaultContractResponse>
{
    public Guid LoanId { get; init; }
    public string Reason { get; init; } = null!;
}
