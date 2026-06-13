using CreditSystem.Domain.Enums;
using MediatR;

namespace CreditSystem.Application.Commands.CreateContract;

public record CreateContractCommand : IRequest<CreateContractResponse>
{
    public Guid ExternalCustomerId { get; init; }  // ID del CRM
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public int TermMonths { get; init; }
    public decimal? CollateralValue { get; init; }
    public AmortizationMethod AmortizationMethod { get; init; } = AmortizationMethod.French;
}
