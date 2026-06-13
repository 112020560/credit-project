using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Persistence;

/// <summary>
/// Repository for managing outbox messages.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Saves an outbox message within the current transaction.
    /// </summary>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages for publishing.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as published.
    /// </summary>
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed after max retries.
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments retry count for a message.
    /// </summary>
    Task IncrementRetryCountAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old published messages.
    /// </summary>
    Task CleanupOldMessagesAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
}
