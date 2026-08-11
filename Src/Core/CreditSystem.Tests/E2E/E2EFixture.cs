using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Infrastructure.EventStore;
using CreditSystem.Infrastructure.Projections;
using CreditSystem.Infrastructure.Projectors;
using CreditSystem.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace CreditSystem.Tests.E2E;

/// <summary>
/// Shared infrastructure for E2E tests against the real database.
/// Each test is responsible for cleaning up its own data using the tracked IDs.
/// </summary>
public class E2EFixture
{
    // Connection string from appsettings.Development.json
    public const string ConnectionString =
        "Host=interchange.proxy.rlwy.net;Database=credit;Username=postgres;" +
        "Password=VIwCMnzKlshSsqCuFgcpzbkpXXqllyFu;Port=30299";

    public ILoanContractRepository BuildLoanRepository()
    {
        var serializer = new JsonEventSerializer();
        var hasher = new Sha256HashGenerator();
        var store = new PostgresEventStore(ConnectionString, serializer, hasher,
            NullLogger<PostgresEventStore>.Instance);
        return new LoanContractRepository(store, NullLogger<LoanContractRepository>.Instance);
    }

    public PostgresProjectionStore BuildProjectionStore() =>
        new(ConnectionString, NullLogger<PostgresProjectionStore>.Instance);

    public CreditProductRepository BuildProductRepository() =>
        new(ConnectionString);

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task CleanupAsync(
        IEnumerable<Guid>? loanIds = null,
        IEnumerable<Guid>? productIds = null,
        IEnumerable<Guid>? memberIds = null,
        IEnumerable<Guid>? customerProfileIds = null)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);

        if (loanIds != null)
        {
            var ids = loanIds.ToList();
            if (ids.Count > 0)
            {
                await conn.ExecuteAsync("DELETE FROM rm_payment_history WHERE loan_id = ANY(@Ids)", new { Ids = ids });
                await conn.ExecuteAsync("DELETE FROM rm_loan_summaries WHERE loan_id = ANY(@Ids)", new { Ids = ids });
                await conn.ExecuteAsync("DELETE FROM stored_events WHERE stream_id = ANY(@Ids)", new { Ids = ids });
                await conn.ExecuteAsync("DELETE FROM event_streams WHERE stream_id = ANY(@Ids)", new { Ids = ids });
            }
        }

        if (productIds != null)
        {
            var ids = productIds.ToList();
            if (ids.Count > 0)
                await conn.ExecuteAsync("DELETE FROM credit_products WHERE id = ANY(@Ids)", new { Ids = ids });
        }

        if (memberIds != null)
        {
            var ids = memberIds.ToList();
            if (ids.Count > 0)
                await conn.ExecuteAsync("DELETE FROM cooperative_members WHERE id = ANY(@Ids)", new { Ids = ids });
        }

        if (customerProfileIds != null)
        {
            var ids = customerProfileIds.ToList();
            if (ids.Count > 0)
                await conn.ExecuteAsync("DELETE FROM customer_credit_profiles WHERE id = ANY(@Ids)", new { Ids = ids });
        }
    }
}
