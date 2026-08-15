using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface ILoanGuaranteeRepository
{
    Task<LoanGuarantee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<LoanGuarantee>> GetByContractAsync(Guid loanContractId, CancellationToken ct = default);
    Task InsertAsync(LoanGuarantee guarantee, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, GuaranteeStatus status, CancellationToken ct = default);
}
