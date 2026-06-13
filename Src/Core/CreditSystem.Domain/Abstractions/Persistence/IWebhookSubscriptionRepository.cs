using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Persistence;

/// <summary>
/// Repository for webhook subscriptions.
/// </summary>
public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> GetActiveByCustomerAndEventAsync(
        Guid customerId,
        string eventType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
