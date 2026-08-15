using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly string _connectionString;

    public IdempotencyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                idempotency_key AS Key,
                response_status AS ResponseStatus,
                response_body   AS ResponseBody,
                expires_at      AS ExpiresAt
            FROM idempotency_keys
            WHERE idempotency_key = @Key
              AND expires_at > NOW()";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<IdempotencyRecord>(
            new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO idempotency_keys (idempotency_key, response_status, response_body, expires_at)
            VALUES (@Key, @ResponseStatus, @ResponseBody, @ExpiresAt)
            ON CONFLICT DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            record.Key,
            record.ResponseStatus,
            record.ResponseBody,
            record.ExpiresAt
        }, cancellationToken: cancellationToken));
    }
}
