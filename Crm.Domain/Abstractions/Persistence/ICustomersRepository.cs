using System;
using Crm.Domain.Customers;

namespace Crm.Domain.Abstractions.Persistence;

public interface ICustomersRepository
{
    Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task<Customer?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task<List<Customer>> GetAllCustomersAsync(CancellationToken cancellationToken);
    Task<(List<Customer> Items, int TotalCount)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken);
}
