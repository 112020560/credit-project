using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class CustomerReadRepository : ICustomerReadRepository
{
    private readonly string _connectionString;

    public CustomerReadRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CustomerCreditProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_credit_profiles WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerCreditProfile>(sql, new { Id = id });
    }

    public async Task<CustomerCreditProfile?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_credit_profiles WHERE external_id = @ExternalId";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerCreditProfile>(sql, new { ExternalId = externalId });
    }

    public async Task<CustomerCreditProfile?> GetByDocumentAsync(string documentNumber, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customer_credit_profiles WHERE document_number = @DocumentNumber";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<CustomerCreditProfile>(sql, new { DocumentNumber = documentNumber });
    }
}
