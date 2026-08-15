namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IRiskClassificationRepository
{
    Task UpdateRiskCategoryAsync(Guid loanId, string category, decimal provision, CancellationToken ct = default);
}
