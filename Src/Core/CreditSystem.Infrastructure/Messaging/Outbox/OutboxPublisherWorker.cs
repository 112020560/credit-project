using System.Text.Json;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Infrastructure.Messaging.RabbitMq.Messages;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Contracts.Payments;

namespace CreditSystem.Infrastructure.Messaging.Outbox;

/// <summary>
/// Background worker that publishes pending outbox messages to RabbitMQ.
/// Implements the Outbox Pattern for guaranteed message delivery.
/// </summary>
public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 3;

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in outbox publisher worker");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Worker stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendingMessages = await outboxRepository.GetPendingMessagesAsync(_batchSize, cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await PublishMessageAsync(publishEndpoint, message.MessageType, message.Payload, cancellationToken);
                await outboxRepository.MarkAsPublishedAsync(message.Id, cancellationToken);

                _logger.LogDebug("Published outbox message {MessageId} of type {MessageType}",
                    message.Id, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);

                if (message.RetryCount >= _maxRetries)
                {
                    await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
                    _logger.LogWarning(
                        "Outbox message {MessageId} marked as failed after {RetryCount} retries",
                        message.Id, message.RetryCount);
                }
                else
                {
                    await outboxRepository.IncrementRetryCountAsync(message.Id, ex.Message, cancellationToken);
                }
            }
        }

        // Periodic cleanup of old messages
        if (Random.Shared.Next(100) < 5) // ~5% chance per batch
        {
            await outboxRepository.CleanupOldMessagesAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        IPublishEndpoint publishEndpoint,
        string messageType,
        string payload,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        object? message = messageType switch
        {
            nameof(ProcessPaymentCommand) or "ProcessPaymentMessage" =>
                JsonSerializer.Deserialize<ProcessPaymentMessage>(payload, options),

            nameof(ProcessRevolvingPaymentCommand) or "ProcessRevolvingPaymentMessage" =>
                JsonSerializer.Deserialize<ProcessRevolvingPaymentMessage>(payload, options),

            nameof(PaymentProcessed) or "PaymentProcessedMessage" =>
                JsonSerializer.Deserialize<PaymentProcessedMessage>(payload, options),

            nameof(RevolvingPaymentProcessed) or "RevolvingPaymentProcessedMessage" =>
                JsonSerializer.Deserialize<RevolvingPaymentProcessedMessage>(payload, options),

            nameof(PaymentFailed) or "PaymentFailedMessage" =>
                JsonSerializer.Deserialize<PaymentFailedMessage>(payload, options),

            nameof(PaymentRejected) or "PaymentRejectedMessage" =>
                JsonSerializer.Deserialize<PaymentRejectedMessage>(payload, options),

            _ => throw new InvalidOperationException($"Unknown message type: {messageType}")
        };

        if (message == null)
        {
            throw new InvalidOperationException($"Failed to deserialize message of type {messageType}");
        }

        await publishEndpoint.Publish(message, cancellationToken);
    }
}
