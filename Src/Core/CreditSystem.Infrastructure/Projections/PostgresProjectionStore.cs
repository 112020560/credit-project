using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projections;

// Infrastructure/Projections/PostgresProjectionStore.cs
using Npgsql;
using Dapper;
using System.Reflection;
using System.Text.Json.Serialization;

public class PostgresProjectionStore : IProjectionStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresProjectionStore> _logger;

    public PostgresProjectionStore(string connectionString, ILogger<PostgresProjectionStore> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<long> GetCheckpointAsync(string projectionName, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT last_position 
            FROM projection_checkpoints 
            WHERE projection_name = @ProjectionName";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<long>(sql, new { ProjectionName = projectionName });
    }

    public async Task SaveCheckpointAsync(
        string projectionName, 
        long position, 
        Guid? lastEventId, 
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO projection_checkpoints (projection_name, last_position, last_event_id, last_updated_at)
            VALUES (@ProjectionName, @Position, @LastEventId, @Now)
            ON CONFLICT (projection_name) DO UPDATE SET
                last_position = @Position,
                last_event_id = @LastEventId,
                last_updated_at = @Now";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            ProjectionName = projectionName,
            Position = position,
            LastEventId = lastEventId,
            Now = DateTime.UtcNow
        });
    }

    public async Task UpsertAsync<T>(
        string tableName, 
        T model, 
        string keyColumn, 
        CancellationToken ct = default)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead
            && p.GetValue(model) != null
            && p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .ToList();

        var columns = properties.Select(p => ToSnakeCase(p.Name)).ToList();
        var values = properties.Select(p => $"@{p.Name}").ToList();
        var updates = properties
            .Where(p => ToSnakeCase(p.Name) != keyColumn)
            .Select(p => $"{ToSnakeCase(p.Name)} = @{p.Name}")
            .ToList();

        var sql = $@"
            INSERT INTO {tableName} ({string.Join(", ", columns)})
            VALUES ({string.Join(", ", values)})
            ON CONFLICT ({keyColumn}) DO UPDATE SET
                {string.Join(", ", updates)}";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, model);
    }

    public async Task DeleteAsync(
        string tableName, 
        string keyColumn, 
        object keyValue, 
        CancellationToken ct = default)
    {
        var sql = $"DELETE FROM {tableName} WHERE {keyColumn} = @KeyValue";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { KeyValue = keyValue });
    }

    public async Task<T?> GetByIdAsync<T>(
        string tableName, 
        string keyColumn, 
        object keyValue, 
        CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM {tableName} WHERE {keyColumn} = @KeyValue";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<T>(sql, new { KeyValue = keyValue });
    }

    public async Task ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, parameters);
    }

    private static string ToSnakeCase(string input)
    {
        return string.Concat(
            input.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()))
            .ToLowerInvariant();
    }
}