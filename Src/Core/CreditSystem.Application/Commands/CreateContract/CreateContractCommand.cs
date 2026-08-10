using CreditSystem.Domain.Enums;
using MediatR;

namespace CreditSystem.Application.Commands.CreateContract;

public record GuaranteeInput(
    GuaranteeType Type,
    string Description,
    decimal AppraisalValue,
    decimal CoverageRate);

public record CreateContractCommand : IRequest<CreateContractResponse>
{
    public Guid ExternalCustomerId { get; init; }  // ID del CRM
    public Guid ProductId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public int TermMonths { get; init; }
    public IReadOnlyList<GuaranteeInput>? Guarantees { get; init; }
    public AmortizationMethod AmortizationMethod { get; init; } = AmortizationMethod.French;
    public string RateType { get; init; } = "Fixed";
    public decimal? Spread { get; init; }
    public string? ReferenceRateId { get; init; }
}
