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

    public Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }

    public async Task<List<Customer>> GetAllCustomersAsync(CancellationToken cancellationToken)
    {
        return await _context.Customers.ToListAsync(cancellationToken);
    }

    public async Task<(List<Customer> Items, int TotalCount)> SearchAsync(
        string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            baseQuery = baseQuery.Where(c =>
                EF.Functions.ILike(c.FullName, pattern) ||
                (c.DisplayName != null && EF.Functions.ILike(c.DisplayName, pattern)) ||
                (c.IdentificationNumber != null && EF.Functions.ILike(c.IdentificationNumber, pattern)));
        }

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.CustomerEmails)
            .Include(c => c.CustomerPhones)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}