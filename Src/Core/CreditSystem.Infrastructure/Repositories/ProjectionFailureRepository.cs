using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class ProjectionFailureRepository : IProjectionFailureRepository
{
    private readonly string _connectionString;

    public ProjectionFailureRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RecordFailureAsync(
        Guid eventId,
        Guid streamId,
        string eventType,
        string projectorName,
        string errorMessage,
        int attempts,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO projection_failures
                (event_id, stream_id, event_type, projector_name, error_message, attempts)
            VALUES
                (@EventId, @StreamId, @EventType, @ProjectorName, @ErrorMessage, @Attempts)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            EventId = eventId,
            StreamId = streamId,
            EventType = eventType,
            ProjectorName = projectorName,
            ErrorMessage = errorMessage,
            Attempts = attempts
        });
    }

    public async Task<bool> ResolveAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE projection_failures
            SET resolved = true, resolved_at = NOW()
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<IEnumerable<ProjectionFailure>> GetUnresolvedAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id as Id, event_id as EventId, stream_id as StreamId,
                   event_type as EventType, projector_name as ProjectorName,
                   error_message as ErrorMessage, occurred_at as OccurredAt,
                   attempts as Attempts, resolved as Resolved, resolved_at as ResolvedAt
            FROM projection_failures
            WHERE resolved = false
            ORDER BY occurred_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<ProjectionFailure>(sql);
    }

    public async Task<IEnumerable<ProjectionFailure>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id as Id, event_id as EventId, stream_id as StreamId,
                   event_type as EventType, projector_name as ProjectorName,
                   error_message as ErrorMessage, occurred_at as OccurredAt,
                   attempts as Attempts, resolved as Resolved, resolved_at as ResolvedAt
            FROM projection_failures
            ORDER BY occurred_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<ProjectionFailure>(sql);
    }

    public async Task<int> CountRecentUnresolvedAsync(TimeSpan window, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(*) FROM projection_failures
            WHERE resolved = false
              AND occurred_at > @Since";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<int>(sql, new { Since = DateTime.UtcNow - window });
    }
}
