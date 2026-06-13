using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Webhooks;

/// <summary>
/// Service for sending webhook notifications to subscribers.
/// </summary>
public interface IWebhookNotifier
{
    Task NotifyAsync(
        Guid paymentId,
        Guid customerId,
        string eventType,
        object payload,
        CancellationToken cancellationToken = default);
}

public class WebhookNotifier : IWebhookNotifier
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookDeliveryRepository _deliveryRepository;
    private readonly ILogger<WebhookNotifier> _logger;

    public WebhookNotifier(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookDeliveryRepository deliveryRepository,
        ILogger<WebhookNotifier> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
    }

    public async Task NotifyAsync(
        Guid paymentId,
        Guid customerId,
        string eventType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetActiveByCustomerAndEventAsync(
            customerId, eventType, cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions for customer {CustomerId}, event {EventType}",
                customerId, eventType);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        foreach (var subscription in subscriptions)
        {
            try
            {
                var delivery = new WebhookDelivery
                {
                    SubscriptionId = subscription.Id,
                    PaymentId = paymentId,
                    EventType = eventType,
                    Payload = payloadJson,
                    Status = WebhookDeliveryStatus.Pending,
                    AttemptCount = 0
                };

                await _deliveryRepository.CreateAsync(delivery, cancellationToken);

                _logger.LogInformation(
                    "Webhook delivery queued for subscription {SubscriptionId}, payment {PaymentId}, event {EventType}",
                    subscription.Id, paymentId, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue webhook delivery for subscription {SubscriptionId}",
                    subscription.Id);
            }
        }
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for webhook payload.
    /// </summary>
    public static string GenerateSignature(string payload, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
