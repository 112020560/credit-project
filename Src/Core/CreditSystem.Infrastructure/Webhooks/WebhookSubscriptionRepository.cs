using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CreditSystem.Infrastructure.Webhooks;

public class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WebhookSubscriptionRepository> _logger;

    public WebhookSubscriptionRepository(
        IConfiguration configuration,
        ILogger<WebhookSubscriptionRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                customer_id as CustomerId,
                event_type as EventType,
                callback_url as CallbackUrl,
                secret_key as SecretKey,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM webhook_subscriptions
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<WebhookSubscription>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveByCustomerAndEventAsync(
        Guid customerId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                customer_id as CustomerId,
                event_type as EventType,
                callback_url as CallbackUrl,
                secret_key as SecretKey,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM webhook_subscriptions
            WHERE customer_id = @CustomerId
              AND event_type = @EventType
              AND is_active = true";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<WebhookSubscription>(
            new CommandDefinition(sql, new { CustomerId = customerId, EventType = eventType },
                cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                id as Id,
                customer_id as CustomerId,
                event_type as EventType,
                callback_url as CallbackUrl,
                secret_key as SecretKey,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM webhook_subscriptions
            WHERE customer_id = @CustomerId
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<WebhookSubscription>(
            new CommandDefinition(sql, new { CustomerId = customerId },
                cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task<Guid> CreateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO webhook_subscriptions
                (id, customer_id, event_type, callback_url, secret_key, is_active, created_at, updated_at)
            VALUES
                (@Id, @CustomerId, @EventType, @CallbackUrl, @SecretKey, @IsActive, @CreatedAt, @UpdatedAt)
            ON CONFLICT (customer_id, event_type, callback_url) DO UPDATE SET
                secret_key = @SecretKey,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            RETURNING id";

        subscription.Id = subscription.Id == Guid.Empty ? Guid.NewGuid() : subscription.Id;
        subscription.CreatedAt = DateTimeOffset.UtcNow;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, subscription, cancellationToken: cancellationToken));

        _logger.LogInformation(
            "Webhook subscription {SubscriptionId} created/updated for customer {CustomerId}, event {EventType}",
            id, subscription.CustomerId, subscription.EventType);

        return id;
    }

    public async Task UpdateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhook_subscriptions
            SET callback_url = @CallbackUrl,
                secret_key = @SecretKey,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, subscription, cancellationToken: cancellationToken));

        _logger.LogDebug("Webhook subscription {SubscriptionId} updated", subscription.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM webhook_subscriptions WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        _logger.LogInformation("Webhook subscription {SubscriptionId} deleted", id);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE webhook_subscriptions
            SET is_active = false, updated_at = @UpdatedAt
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UpdatedAt = DateTimeOffset.UtcNow },
                cancellationToken: cancellationToken));

        _logger.LogInformation("Webhook subscription {SubscriptionId} deactivated", id);
    }
}
