using System.Text;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Webhooks;

/// <summary>
/// Background worker that delivers pending webhook notifications.
/// </summary>
public class WebhookDeliveryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 50;
    private readonly int _maxRetries = 3;
    private readonly TimeSpan[] _retryDelays = new[]
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    };

    public WebhookDeliveryWorker(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Delivery Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in webhook delivery worker");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Webhook Delivery Worker stopped");
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deliveryRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<IWebhookSubscriptionRepository>();

        var pendingDeliveries = await deliveryRepository.GetPendingDeliveriesAsync(_batchSize, cancellationToken);

        if (pendingDeliveries.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} pending webhook deliveries", pendingDeliveries.Count);

        var httpClient = _httpClientFactory.CreateClient("WebhookClient");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        foreach (var delivery in pendingDeliveries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var subscription = await subscriptionRepository.GetByIdAsync(delivery.SubscriptionId, cancellationToken);

            if (subscription == null || !subscription.IsActive)
            {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} not found or inactive. Marking delivery {DeliveryId} as failed",
                    delivery.SubscriptionId, delivery.Id);

                await deliveryRepository.MarkAsFailedAsync(delivery.Id, null, "Subscription not found or inactive",
                    cancellationToken);
                continue;
            }

            await DeliverWebhookAsync(httpClient, delivery, subscription, deliveryRepository, cancellationToken);
        }

        // Periodic cleanup
        if (Random.Shared.Next(100) < 5)
        {
            await deliveryRepository.CleanupOldDeliveriesAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task DeliverWebhookAsync(
        HttpClient httpClient,
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        IWebhookDeliveryRepository deliveryRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var signature = WebhookNotifier.GenerateSignature(delivery.Payload, subscription.SecretKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl);
            request.Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Event-Type", delivery.EventType);
            request.Headers.Add("X-Payment-Id", delivery.PaymentId.ToString());
            request.Headers.Add("X-Delivery-Id", delivery.Id.ToString());
            request.Headers.Add("X-Attempt-Number", (delivery.AttemptCount + 1).ToString());

            _logger.LogDebug(
                "Delivering webhook {DeliveryId} to {CallbackUrl}. Attempt {Attempt}",
                delivery.Id, subscription.CallbackUrl, delivery.AttemptCount + 1);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                await deliveryRepository.MarkAsDeliveredAsync(delivery.Id, statusCode, responseBody, cancellationToken);

                _logger.LogInformation(
                    "Webhook {DeliveryId} delivered successfully to {CallbackUrl}. Status: {StatusCode}",
                    delivery.Id, subscription.CallbackUrl, statusCode);
            }
            else
            {
                await HandleDeliveryFailureAsync(
                    delivery, deliveryRepository, statusCode, responseBody, null, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            await HandleDeliveryFailureAsync(delivery, deliveryRepository, null, null, ex.Message, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await HandleDeliveryFailureAsync(delivery, deliveryRepository, null, null, "Request timeout", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering webhook {DeliveryId}", delivery.Id);
            await HandleDeliveryFailureAsync(delivery, deliveryRepository, null, null, ex.Message, cancellationToken);
        }
    }

    private async Task HandleDeliveryFailureAsync(
        WebhookDelivery delivery,
        IWebhookDeliveryRepository deliveryRepository,
        int? httpStatusCode,
        string? responseBody,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var newAttemptCount = delivery.AttemptCount + 1;

        if (newAttemptCount >= _maxRetries)
        {
            await deliveryRepository.MarkAsFailedAsync(
                delivery.Id,
                httpStatusCode,
                responseBody ?? errorMessage,
                cancellationToken);

            _logger.LogWarning(
                "Webhook {DeliveryId} marked as failed after {Attempts} attempts. Status: {StatusCode}, Error: {Error}",
                delivery.Id, newAttemptCount, httpStatusCode, errorMessage ?? responseBody);
        }
        else
        {
            var retryDelay = _retryDelays[Math.Min(newAttemptCount - 1, _retryDelays.Length - 1)];
            var nextRetryAt = DateTimeOffset.UtcNow.Add(retryDelay);

            await deliveryRepository.ScheduleRetryAsync(delivery.Id, newAttemptCount, nextRetryAt, cancellationToken);

            _logger.LogWarning(
                "Webhook {DeliveryId} delivery failed. Attempt {Attempt}/{MaxRetries}. " +
                "Status: {StatusCode}. Next retry at {NextRetry}",
                delivery.Id, newAttemptCount, _maxRetries, httpStatusCode, nextRetryAt);
        }
    }
}
