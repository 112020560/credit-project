using MediatR;

namespace CreditSystem.Application.Commands.PayoffContract;

public record PayoffContractCommand : IRequest<PayoffContractResponse>
{
    public Guid LoanId { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string? ReferenceNumber { get; init; }
}
