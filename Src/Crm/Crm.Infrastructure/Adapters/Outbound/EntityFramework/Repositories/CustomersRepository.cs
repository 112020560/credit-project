using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class CustomersRepository : ICustomersRepository
{
    private readonly CrmDbContext _context;
    public CustomersRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .Include(c => c.CustomerEmails)
            .Include(c => c.CustomerPhones)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }

    public async Task<(List<Customer> Items, int TotalCount)> SearchAsync(
        CustomerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = $"%{criteria.Query.Trim()}%";
            baseQuery = baseQuery.Where(c =>
                EF.Functions.ILike(c.FullName, pattern) ||
                (c.DisplayName != null && EF.Functions.ILike(c.DisplayName, pattern)) ||
                (c.IdentificationNumber != null && EF.Functions.ILike(c.IdentificationNumber, pattern)));
        }

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(c => c.FullName)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Include(c => c.CustomerEmails)
            .Include(c => c.CustomerPhones)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}