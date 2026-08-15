using CreditSystem.Domain.Abstractions.Repositories;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class ProjectionCheckpointRepository : IProjectionCheckpointRepository
{
    private readonly string _connectionString;

    public ProjectionCheckpointRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<long> GetCheckpointAsync(string projectorName, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT last_sequence FROM projection_checkpoints
            WHERE projector_name = @ProjectorName";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<long>(sql, new { ProjectorName = projectorName });
    }

    public async Task SaveCheckpointAsync(string projectorName, long sequence, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO projection_checkpoints (projector_name, last_sequence, updated_at)
            VALUES (@ProjectorName, @Sequence, NOW())
            ON CONFLICT (projector_name) DO UPDATE
                SET last_sequence = @Sequence,
                    updated_at    = NOW()";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { ProjectorName = projectorName, Sequence = sequence });
    }
}
