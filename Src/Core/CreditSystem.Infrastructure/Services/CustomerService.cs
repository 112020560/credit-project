using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Entities;

namespace CreditSystem.Infrastructure.Services;

using Npgsql;
using Dapper;

public class CustomerService : ICustomerService
{
    private readonly string _connectionString;

    public CustomerService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CustomerReference?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_references WHERE id = @Id";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerReference>(sql, new { Id = id });
    }

    public async Task<CustomerReference?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_references WHERE external_id = @ExternalId";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerReference>(sql, new { ExternalId = externalId });
    }

    public async Task<CustomerReference?> GetByDocumentAsync(string documentNumber, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_references WHERE document_number = @DocumentNumber";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerReference>(sql, new { DocumentNumber = documentNumber });
    }
}