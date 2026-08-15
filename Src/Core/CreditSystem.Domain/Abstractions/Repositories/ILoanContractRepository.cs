using CreditSystem.Domain.Aggregates.LoanContract;

namespace CreditSystem.Domain.Abstractions;

public interface ILoanContractRepository
{
    Task<LoanContractAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(LoanContractAggregate aggregate, CancellationToken ct = default);
}