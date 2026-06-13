using System;
using Credit.Domain.Entities;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class CreditApplicationRepository
{
    private readonly CreditDbContext _context;
    private readonly ILogger<CreditApplicationRepository> _logger;
    public CreditApplicationRepository(CreditDbContext context, ILogger<CreditApplicationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveAsync(CreditApplication creditApplication, CancellationToken cancellationToken)
    {
        await _context.CreditApplications.AddAsync(creditApplication, cancellationToken);
    }

    //Update CreditApplication Status
    public async Task UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        await _context.CreditApplications.Where(ca => ca.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ca => ca.Status, status)
                .SetProperty(ca => ca.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }
    //Check CreditApplication Exists
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.CreditApplications.AnyAsync(ca => ca.Id == id, cancellationToken);
    }

    //Get CreditApplication By Id
    public async Task<CreditApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.CreditApplications.FirstOrDefaultAsync(ca => ca.Id == id, cancellationToken);
    }
}
