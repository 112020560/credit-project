using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CreditSystem.Infrastructure.Webhooks;

public class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WebhookDeliveryRepository> _logger;

    public WebhookDeliveryRepository(
        IConfiguration configuration,
        ILogger<WebhookDeliveryRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                subscription_id as SubscriptionId,
                payment_id as PaymentId,
                event_type as EventType,
                payload as Payload,
                status as StatusStr,
                http_status_code as HttpStatusCode,
                response_body as ResponseBody,
                attempt_count as AttemptCount,
                created_at as CreatedAt,
                delivered_at as DeliveredAt,
                next_retry_at as NextRetryAt
            FROM webhook_deliveries
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dto = await connection.QuerySingleOrDefaultAsync<WebhookDeliveryDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return dto?.ToEntity();
    }

    public async Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                subscription_id as SubscriptionId,
                payment_id as PaymentId,
                event_type as EventType,
                payload as Payload,
                status as StatusStr,
                http_status_code as HttpStatusCode,
                response_body as ResponseBody,
                attempt_count as AttemptCount,
                created_at as CreatedAt,
                delivered_at as DeliveredAt,
                next_retry_at as NextRetryAt
            FROM webhook_deliveries
            WHERE status = 'PENDING'
              AND (next_retry_at IS NULL OR next_retry_at <= @Now)
            ORDER BY created_at ASC
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED";

        await using var connection = new NpgsqlConnection(_connectionString);
        var dtos = await connection.QueryAsync<WebhookDeliveryDto>(
            new CommandDefinition(sql, new { Now = DateTimeOffset.UtcNow, BatchSize = batchSize },
                cancellationToken: cancellationToken));

        return dtos.Select(d => d.ToEntity()).ToList();
    }

    public async Task<Guid> CreateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO webhook_deliveries
                (id, subscription_id, payment_id, event_type, payload, status, attempt_count, created_at)
            VALUES
                (@Id, @SubscriptionId, @PaymentId, @EventType, @Payload::jsonb, @Status, @AttemptCount, @CreatedAt)
            RETURNING id";

        delivery.Id = delivery.Id == Guid.Empty ? Guid.NewGuid() : delivery.Id;
        delivery.CreatedAt = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, new
            {
                delivery.Id,
                delivery.SubscriptionId,
                delivery.PaymentId,
                delivery.EventType,
                delivery.Payload,
                Status = delivery.Status.ToString().ToUpperInvariant(),
                delivery.AttemptCount,
                delivery.CreatedAt
            }, cancellationToken: cancellationToken));

        _logger.LogDebug("Webhook delivery {DeliveryId} created for subscription {SubscriptionId}",
            id, delivery.SubscriptionId);

        return id;
    }

    public async Task MarkAsDeliveredAsync(
        Guid id,
        int httpStatusCode,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhook_deliveries
            SET status = 'DELIVERED',
                http_status_code = @HttpStatusCode,
                response_body = @ResponseBody,
                delivered_at = @DeliveredAt,
                attempt_count = attempt_count + 1
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = id,
                HttpStatusCode = httpStatusCode,
                ResponseBody = responseBody,
                DeliveredAt = DateTimeOffset.UtcNow
            }, cancellationToken: cancellationToken));

        _logger.LogDebug("Webhook delivery {DeliveryId} marked as delivered", id);
    }

    public async Task MarkAsFailedAsync(
        Guid id,
        int? httpStatusCode,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhook_deliveries
            SET status = 'FAILED',
                http_status_code = @HttpStatusCode,
                response_body = @ResponseBody
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = id,
                HttpStatusCode = httpStatusCode,
                ResponseBody = responseBody
            }, cancellationToken: cancellationToken));

        _logger.LogWarning("Webhook delivery {DeliveryId} marked as failed", id);
    }

    public async Task ScheduleRetryAsync(
        Guid id,
        int attemptCount,
        DateTimeOffset nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhook_deliveries
            SET attempt_count = @AttemptCount,
                next_retry_at = @NextRetryAt
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = id,
                AttemptCount = attemptCount,
                NextRetryAt = nextRetryAt
            }, cancellationToken: cancellationToken));

        _logger.LogDebug("Webhook delivery {DeliveryId} scheduled for retry at {RetryAt}",
            id, nextRetryAt);
    }

    public async Task CleanupOldDeliveriesAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM webhook_deliveries
            WHERE status IN ('DELIVERED', 'FAILED')
              AND created_at < @CutoffDate";

        await using var connection = new NpgsqlConnection(_connectionString);
        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { CutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays) },
                cancellationToken: cancellationToken));

        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old webhook deliveries", deleted);
        }
    }

    private class WebhookDeliveryDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid PaymentId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string StatusStr { get; set; } = string.Empty;
        public int? HttpStatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public int AttemptCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? NextRetryAt { get; set; }

        public WebhookDelivery ToEntity() => new()
        {
            Id = Id,
            SubscriptionId = SubscriptionId,
            PaymentId = PaymentId,
            EventType = EventType,
            Payload = Payload,
            Status = Enum.Parse<WebhookDeliveryStatus>(StatusStr, ignoreCase: true),
            HttpStatusCode = HttpStatusCode,
            ResponseBody = ResponseBody,
            AttemptCount = AttemptCount,
            CreatedAt = CreatedAt,
            DeliveredAt = DeliveredAt,
            NextRetryAt = NextRetryAt
        };
    }
}
