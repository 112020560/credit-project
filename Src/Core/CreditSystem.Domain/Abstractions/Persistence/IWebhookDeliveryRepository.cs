using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Persistence;

/// <summary>
/// Repository for webhook delivery tracking.
/// </summary>
public interface IWebhookDeliveryRepository
{
    Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task MarkAsDeliveredAsync(Guid id, int httpStatusCode, string? responseBody, CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(Guid id, int? httpStatusCode, string? responseBody, CancellationToken cancellationToken = default);

    Task ScheduleRetryAsync(Guid id, int attemptCount, DateTimeOffset nextRetryAt, CancellationToken cancellationToken = default);

    Task CleanupOldDeliveriesAsync(int retentionDays = 30, CancellationToken cancellationToken = default);
}
