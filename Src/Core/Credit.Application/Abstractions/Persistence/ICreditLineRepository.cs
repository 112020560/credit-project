using Credit.Domain.Entities;

namespace Credit.Application.Abstractions.Persistence;

public interface ICreditLineRepository
{
    Task<CreditLine?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CreditLine>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<CreditLine>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CreditLine creditLine, CancellationToken ct = default);
    Task UpdateAmortizationScheduleAsync(Guid creditLineId, string scheduleJson, CancellationToken ct = default);
    Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);
}
