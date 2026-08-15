using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class ExternalCustomerRefRepository : IExternalCustomerRefRepository
{
    private readonly CrmDbContext _context;

    public ExternalCustomerRefRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(ExternalCustomerRef externalCustomerRef, CancellationToken cancellationToken)
    {
        var existing = await _context.ExternalCustomerRefs
            .AsTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalCustomerRef.ExternalId, cancellationToken);

        if (existing is null)
        {
            externalCustomerRef.CreatedAt = DateTime.UtcNow;
            externalCustomerRef.UpdatedAt = DateTime.UtcNow;
            externalCustomerRef.Version = 1;
            await _context.ExternalCustomerRefs.AddAsync(externalCustomerRef, cancellationToken);
        }
        else
        {
            existing.DisplayName = externalCustomerRef.DisplayName;
            existing.LegalName = externalCustomerRef.LegalName;
            existing.RiskScore = externalCustomerRef.RiskScore;
            existing.Metadata = externalCustomerRef.Metadata;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version += 1;
        }
    }

    public async Task<ExternalCustomerRef?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return await _context.ExternalCustomerRefs
            .FirstOrDefaultAsync(r => r.ExternalId == externalId, cancellationToken);
    }
}
