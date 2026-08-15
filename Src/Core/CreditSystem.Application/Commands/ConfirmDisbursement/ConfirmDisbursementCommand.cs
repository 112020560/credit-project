using MediatR;

namespace CreditSystem.Application.Commands.ConfirmDisbursement;

public record ConfirmDisbursementCommand : IRequest<ConfirmDisbursementResponse>
{
    public Guid LoanId { get; init; }
    public string ConfirmedBy { get; init; } = null!;
}
