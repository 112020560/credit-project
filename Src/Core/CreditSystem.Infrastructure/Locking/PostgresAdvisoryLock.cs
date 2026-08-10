using System.Collections.Concurrent;
using CreditSystem.Domain.Abstractions;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Locking;

public class PostgresAdvisoryLock : IDistributedLock
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<long, NpgsqlConnection> _connections = new();

    public PostgresAdvisoryLock(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TryAcquireAsync(long lockId, CancellationToken cancellationToken = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var acquired = await conn.QuerySingleAsync<bool>(
            "SELECT pg_try_advisory_lock(@id)", new { id = lockId });

        if (acquired)
        {
            _connections[lockId] = conn;
            return true;
        }

        await conn.CloseAsync();
        conn.Dispose();
        return false;
    }

    public async Task ReleaseAsync(long lockId, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryRemove(lockId, out var conn))
            return;

        try
        {
            await conn.ExecuteAsync("SELECT pg_advisory_unlock(@id)", new { id = lockId });
        }
        finally
        {
            await conn.CloseAsync();
            conn.Dispose();
        }
    }
}
