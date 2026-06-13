using Credit.Application.Abstractions.Persistence;
using Credit.Domain.Entities;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

internal sealed class CreditLineRepository(CreditDbContext context) : ICreditLineRepository
{
    public async Task<CreditLine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.CreditLines
            .Include(cl => cl.Product)
            .FirstOrDefaultAsync(cl => cl.Id == id, ct);

    public async Task<IReadOnlyList<CreditLine>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await context.CreditLines
            .Include(cl => cl.Product)
            .Where(cl => cl.CustomerId == customerId)
            .OrderByDescending(cl => cl.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CreditLine>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await context.CreditLines
            .Include(cl => cl.Product)
            .Where(cl => cl.CustomerId == customerId && cl.Status == "Active")
            .OrderByDescending(cl => cl.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(CreditLine creditLine, CancellationToken ct = default)
        => await context.CreditLines.AddAsync(creditLine, ct);

    public async Task UpdateAmortizationScheduleAsync(Guid creditLineId, string scheduleJson, CancellationToken ct = default)
        => await context.CreditLines
            .Where(cl => cl.Id == creditLineId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cl => cl.AmortizationSchedule, scheduleJson)
                .SetProperty(cl => cl.UpdatedAt, DateTime.UtcNow), ct);

    public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => await context.CreditLines
            .AnyAsync(cl => cl.Metadata != null && cl.Metadata.Contains(invoiceNumber), ct);
}
