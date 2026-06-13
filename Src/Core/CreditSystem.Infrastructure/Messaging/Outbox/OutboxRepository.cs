using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CreditSystem.Infrastructure.Messaging.Outbox;

public class OutboxRepository : IOutboxRepository
{
    private readonly string _connectionString;
    private readonly ILogger<OutboxRepository> _logger;

    public OutboxRepository(
        IConfiguration configuration,
        ILogger<OutboxRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO outbox_messages
                (id, message_type, payload, correlation_id, status, created_at, retry_count)
            VALUES
                (@Id, @MessageType, @Payload::jsonb, @CorrelationId, @Status, @CreatedAt, @RetryCount)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            message.Id,
            message.MessageType,
            message.Payload,
            message.CorrelationId,
            Status = message.Status.ToString().ToUpperInvariant(),
            message.CreatedAt,
            message.RetryCount
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Outbox message {MessageId} of type {MessageType} saved",
            message.Id, message.MessageType);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                message_type as MessageType,
                payload as Payload,
                correlation_id as CorrelationId,
                status as Status,
                created_at as CreatedAt,
                published_at as PublishedAt,
                retry_count as RetryCount,
                last_error as LastError
            FROM outbox_messages
            WHERE status = 'PENDING'
            ORDER BY created_at ASC
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED";

        await using var connection = new NpgsqlConnection(_connectionString);
        var messages = await connection.QueryAsync<OutboxMessageDto>(
            new CommandDefinition(sql, new { BatchSize = batchSize }, cancellationToken: cancellationToken));

        return messages.Select(dto => new OutboxMessage
        {
            Id = dto.Id,
            MessageType = dto.MessageType,
            Payload = dto.Payload,
            CorrelationId = dto.CorrelationId,
            Status = Enum.Parse<OutboxMessageStatus>(dto.Status, ignoreCase: true),
            CreatedAt = dto.CreatedAt,
            PublishedAt = dto.PublishedAt,
            RetryCount = dto.RetryCount,
            LastError = dto.LastError
        }).ToList();
    }

    public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE outbox_messages
            SET status = 'PUBLISHED', published_at = @PublishedAt
            WHERE id = @MessageId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            MessageId = messageId,
            PublishedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Outbox message {MessageId} marked as published", messageId);
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE outbox_messages
            SET status = 'FAILED', last_error = @Error
            WHERE id = @MessageId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            MessageId = messageId,
            Error = error
        }, cancellationToken: cancellationToken));

        _logger.LogWarning("Outbox message {MessageId} marked as failed: {Error}", messageId, error);
    }

    public async Task IncrementRetryCountAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE outbox_messages
            SET retry_count = retry_count + 1, last_error = @Error
            WHERE id = @MessageId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            MessageId = messageId,
            Error = error
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Outbox message {MessageId} retry count incremented. Error: {Error}",
            messageId, error);
    }

    public async Task CleanupOldMessagesAsync(
        int retentionDays = 7,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM outbox_messages
            WHERE status = 'PUBLISHED'
              AND published_at < @CutoffDate";

        await using var connection = new NpgsqlConnection(_connectionString);
        var deleted = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            CutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays)
        }, cancellationToken: cancellationToken));

        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old outbox messages", deleted);
        }
    }

    private class OutboxMessageDto
    {
        public Guid Id { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public Guid? CorrelationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }
}
