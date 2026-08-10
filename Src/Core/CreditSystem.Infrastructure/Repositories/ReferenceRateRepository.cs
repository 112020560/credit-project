using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class ReferenceRateRepository : IReferenceRateRepository
{
    private readonly string _connectionString;

    public ReferenceRateRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
    }

    public async Task<ReferenceRateEntry?> GetCurrentAsync(string id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id          AS Id,
                   name        AS Name,
                   current_value AS CurrentValue,
                   effective_date AS EffectiveDate,
                   source      AS Source,
                   updated_at  AS UpdatedAt
            FROM reference_rates
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<ReferenceRateEntry>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(ReferenceRateEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO reference_rates (id, name, current_value, effective_date, source, updated_at)
            VALUES (@Id, @Name, @CurrentValue, @EffectiveDate, @Source, NOW())
            ON CONFLICT (id) DO UPDATE
                SET name           = EXCLUDED.name,
                    current_value  = EXCLUDED.current_value,
                    effective_date = EXCLUDED.effective_date,
                    source         = EXCLUDED.source,
                    updated_at     = NOW()";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                entry.Id,
                entry.Name,
                entry.CurrentValue,
                entry.EffectiveDate,
                entry.Source
            }, cancellationToken: cancellationToken));
    }
}
