using MediatR;

namespace CreditSystem.Application.Commands.SyncCustomerFromCrm;

public record SyncCustomerFromCrmCommand : IRequest
{
    public Guid ExternalId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DocumentType { get; init; }
    public string? DocumentNumber { get; init; }
    public int? CreditScore { get; init; }
    public decimal? MonthlyIncome { get; init; }
    public decimal? MonthlyDebt { get; init; }
}
