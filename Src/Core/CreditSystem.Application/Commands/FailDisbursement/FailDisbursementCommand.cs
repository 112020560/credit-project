using MediatR;

namespace CreditSystem.Application.Commands.FailDisbursement;

public record FailDisbursementCommand : IRequest<FailDisbursementResponse>
{
    public Guid LoanId { get; init; }
    public string Reason { get; init; } = null!;
}
