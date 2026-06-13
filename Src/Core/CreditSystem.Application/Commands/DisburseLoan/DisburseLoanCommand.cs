using MediatR;

namespace CreditSystem.Application.Commands.DisburseLoan;

public record DisburseLoanCommand : IRequest<DisburseLoanResponse>
{
    public Guid LoanId { get; init; }
    public string DisbursementMethod { get; init; } = null!;  // WIRE, ACH, CHECK
    public string DestinationAccount { get; init; } = null!;
}
