namespace CreditSystem.Domain.Abstractions.Repositories;

public interface ICustomerCreditProfileRepository
{
    Task UpsertAsync(
        Guid externalId,
        string? fullName,
        string? email,
        string? phone,
        string? documentType,
        string? documentNumber,
        int? creditScore,
        decimal? monthlyIncome,
        decimal? monthlyDebt,
        CancellationToken ct = default);
}
