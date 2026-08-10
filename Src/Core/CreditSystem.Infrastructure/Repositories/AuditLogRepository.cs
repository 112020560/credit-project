using System.Text.Json;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly string _connectionString;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(string connectionString, ILogger<AuditLogRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = @"
                INSERT INTO audit_log (id, occurred_at, user_id, action, entity_type, entity_id, details)
                VALUES (@Id, @OccurredAt, @UserId, @Action, @EntityType, @EntityId, @Details::jsonb)";

            var detailsJson = entry.Details is null
                ? null
                : JsonSerializer.Serialize(entry.Details);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                entry.Id,
                entry.OccurredAt,
                entry.UserId,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                Details = detailsJson
            }, cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry: action={Action}, entityId={EntityId}",
                entry.Action, entry.EntityId);
        }
    }
}
