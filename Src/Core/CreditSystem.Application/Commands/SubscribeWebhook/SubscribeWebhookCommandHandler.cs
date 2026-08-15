using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.SubscribeWebhook;

public class SubscribeWebhookCommandHandler : IRequestHandler<SubscribeWebhookCommand, SubscribeWebhookResponse>
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscribeWebhookCommandHandler> _logger;

    public SubscribeWebhookCommandHandler(
        IWebhookSubscriptionRepository subscriptionRepository,
        ILogger<SubscribeWebhookCommandHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<SubscribeWebhookResponse> Handle(
        SubscribeWebhookCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating webhook subscription for customer {CustomerId}, event {EventType}, URL {CallbackUrl}",
            request.CustomerId, request.EventType, request.CallbackUrl);

        try
        {
            var subscription = new WebhookSubscription
            {
                CustomerId = request.CustomerId,
                EventType = request.EventType,
                CallbackUrl = request.CallbackUrl,
                SecretKey = request.SecretKey,
                IsActive = true
            };

            var subscriptionId = await _subscriptionRepository.CreateAsync(subscription, cancellationToken);

            _logger.LogInformation(
                "Webhook subscription {SubscriptionId} created for customer {CustomerId}",
                subscriptionId, request.CustomerId);

            return SubscribeWebhookResponse.Success(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create webhook subscription for customer {CustomerId}",
                request.CustomerId);

            return SubscribeWebhookResponse.Failed("Failed to create webhook subscription");
        }
    }
}
