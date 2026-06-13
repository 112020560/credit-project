using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Infrastructure.EventStore.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using Dapper;

namespace CreditSystem.Infrastructure.EventStore;

// Infrastructure/EventStore/PostgresEventStore.cs


public class PostgresEventStore : IEventStore
{
    private readonly string _connectionString;
    private readonly IEventSerializer _serializer;
    private readonly IHashGenerator _hashGenerator;
    private readonly ILogger<PostgresEventStore> _logger;

    public PostgresEventStore(
        string connectionString,
        IEventSerializer serializer,
        IHashGenerator hashGenerator,
        ILogger<PostgresEventStore> logger)
    {
        _connectionString = connectionString;
        _serializer = serializer;
        _hashGenerator = hashGenerator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        int fromVersion = 0,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id as Id, stream_id as StreamId, event_type as EventType, event_data as EventData, metadata as Metadata, version as Version, 
                   hash as Hash, previous_hash as PreviousHash, occurred_at as OccurredAt, stored_at as StoredAt
            FROM stored_events
            WHERE stream_id = @StreamId AND version >= @FromVersion
            ORDER BY version ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var storedEvents = await connection.QueryAsync<StoredEvent>(
            sql, 
            new { StreamId = streamId, FromVersion = fromVersion });

        return storedEvents
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        DateTime fromDate,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, stream_id, event_type, event_data, metadata, version,
                   hash, previous_hash, occurred_at, stored_at
            FROM stored_events
            WHERE stream_id = @StreamId AND occurred_at >= @FromDate
            ORDER BY version ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var storedEvents = await connection.QueryAsync<StoredEvent>(
            sql,
            new { StreamId = streamId, FromDate = fromDate });

        return storedEvents
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList()
            .AsReadOnly();
    }

    public async Task AppendAsync(
        Guid streamId,
        string streamType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken ct = default)
    {
        var eventList = events.ToList();
        
        if (!eventList.Any())
            return;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // Verificar/crear stream
            var currentVersion = await GetOrCreateStreamAsync(
                connection, 
                transaction, 
                streamId, 
                streamType, 
                expectedVersion, 
                ct);

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(
                    $"Expected version {expectedVersion}, but found {currentVersion}");
            }

            // Obtener último hash
            var lastHash = await GetLastHashAsync(connection, transaction, streamId, ct);

            // Insertar eventos
            var version = expectedVersion;
            var previousHash = lastHash;

            foreach (var @event in eventList)
            {
                version++;
                
                var eventType = @event.GetType().Name;
                var eventData = _serializer.Serialize(@event);
                var hash = _hashGenerator.GenerateHash(
                    streamId, 
                    eventType, 
                    eventData, 
                    version, 
                    previousHash);

                var storedEvent = new StoredEvent
                {
                    Id = Guid.NewGuid(),
                    StreamId = streamId,
                    EventType = eventType,
                    EventData = eventData,
                    Metadata = _serializer.SerializeMetadata(ExtractMetadata(@event)),
                    Version = version,
                    Hash = hash,
                    PreviousHash = previousHash,
                    OccurredAt = @event.OccurredAt,
                    StoredAt = DateTime.UtcNow
                };

                await InsertEventAsync(connection, transaction, storedEvent, ct);
                
                // Insertar en outbox para publicación
                await InsertOutboxAsync(connection, transaction, storedEvent.Id, ct);

                previousHash = hash;
            }

            // Actualizar versión del stream
            await UpdateStreamVersionAsync(connection, transaction, streamId, version, ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Appended {Count} events to stream {StreamId}, new version: {Version}",
                eventList.Count, streamId, version);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> GetCurrentVersionAsync(Guid streamId, CancellationToken ct = default)
    {
        const string sql = "SELECT version FROM event_streams WHERE stream_id = @StreamId";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        
        return await connection.QuerySingleOrDefaultAsync<int>(sql, new { StreamId = streamId });
    }

    public async Task<bool> StreamExistsAsync(Guid streamId, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM event_streams WHERE stream_id = @StreamId)";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        
        return await connection.QuerySingleAsync<bool>(sql, new { StreamId = streamId });
    }

    #region Snapshots

    public async Task SaveSnapshotAsync<TState>(
        Guid streamId,
        TState state,
        int version,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO event_snapshots (id, stream_id, state_data, version, created_at)
            VALUES (@Id, @StreamId, @StateData::jsonb, @Version, @CreatedAt)
            ON CONFLICT (stream_id, version) DO UPDATE SET
                state_data = @StateData::jsonb,
                created_at = @CreatedAt";

        await using var connection = new NpgsqlConnection(_connectionString);
        
        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            StateData = _serializer.SerializeState(state),
            Version = version,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Saved snapshot for stream {StreamId} at version {Version}",
            streamId, version);
    }

    public async Task<(TState? State, int Version)> GetLatestSnapshotAsync<TState>(
        Guid streamId,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT state_data, version
            FROM event_snapshots
            WHERE stream_id = @StreamId
            ORDER BY version DESC
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var result = await connection.QuerySingleOrDefaultAsync<(string StateData, int Version)?>(
            sql, 
            new { StreamId = streamId });

        if (result == null)
            return (default, 0);

        var state = _serializer.DeserializeState<TState>(result.Value.StateData);
        return (state, result.Value.Version);
    }

    #endregion

    #region Global Queries

    public async Task<IReadOnlyList<IDomainEvent>> GetAllEventsAsync(
        string? eventType = null,
        DateTime? fromDate = null,
        int limit = 1000,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT id, stream_id, event_type, event_data, metadata, version,
                   hash, previous_hash, occurred_at, stored_at
            FROM stored_events
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(eventType))
        {
            sql += " AND event_type = @EventType";
            parameters.Add("EventType", eventType);
        }

        if (fromDate.HasValue)
        {
            sql += " AND occurred_at >= @FromDate";
            parameters.Add("FromDate", fromDate.Value);
        }

        sql += " ORDER BY occurred_at ASC LIMIT @Limit";
        parameters.Add("Limit", limit);

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var storedEvents = await connection.QueryAsync<StoredEvent>(sql, parameters);

        return storedEvents
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList()
            .AsReadOnly();
    }

    #endregion

    #region Verification

    public async Task<bool> VerifyStreamIntegrityAsync(Guid streamId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, stream_id, event_type, event_data, metadata, version,
                   hash, previous_hash, occurred_at, stored_at
            FROM stored_events
            WHERE stream_id = @StreamId
            ORDER BY version ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var events = await connection.QueryAsync<StoredEvent>(sql, new { StreamId = streamId });
        
        return _hashGenerator.VerifyChain(events);
    }

    #endregion

    #region Private Helpers

    private async Task<int> GetOrCreateStreamAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid streamId,
        string streamType,
        int expectedVersion,
        CancellationToken ct)
    {
        const string selectSql = @"
            SELECT version FROM event_streams 
            WHERE stream_id = @StreamId 
            FOR UPDATE";

        const string insertSql = @"
            INSERT INTO event_streams (stream_id, stream_type, version, created_at, updated_at)
            VALUES (@StreamId, @StreamType, 0, @Now, @Now)
            ON CONFLICT (stream_id) DO NOTHING";

        var version = await connection.QuerySingleOrDefaultAsync<int?>(
            selectSql, 
            new { StreamId = streamId }, 
            transaction);

        if (version == null)
        {
            if (expectedVersion != 0)
            {
                throw new ConcurrencyException(
                    $"Stream {streamId} does not exist, expected version must be 0");
            }

            await connection.ExecuteAsync(
                insertSql,
                new { StreamId = streamId, StreamType = streamType, Now = DateTime.UtcNow },
                transaction);
            
            return 0;
        }

        return version.Value;
    }

    private async Task<string?> GetLastHashAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid streamId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT hash FROM stored_events
            WHERE stream_id = @StreamId
            ORDER BY version DESC
            LIMIT 1";

        return await connection.QuerySingleOrDefaultAsync<string>(
            sql, 
            new { StreamId = streamId }, 
            transaction);
    }

    private async Task InsertEventAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        StoredEvent @event,
        CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO stored_events 
                (id, stream_id, event_type, event_data, metadata, version, hash, previous_hash, occurred_at, stored_at)
            VALUES 
                (@Id, @StreamId, @EventType, @EventData::jsonb, @Metadata::jsonb, @Version, @Hash, @PreviousHash, @OccurredAt, @StoredAt)";

        await connection.ExecuteAsync(sql, @event, transaction);
    }

    private async Task InsertOutboxAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid eventId,
        CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO event_outbox (id, event_id, destination, status, created_at)
            VALUES (@Id, @EventId, 'default', 'pending', @Now)";

        await connection.ExecuteAsync(
            sql,
            new { Id = Guid.NewGuid(), EventId = eventId, Now = DateTime.UtcNow },
            transaction);
    }

    private async Task UpdateStreamVersionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid streamId,
        int version,
        CancellationToken ct)
    {
        const string sql = @"
            UPDATE event_streams 
            SET version = @Version, updated_at = @Now
            WHERE stream_id = @StreamId";

        await connection.ExecuteAsync(
            sql,
            new { StreamId = streamId, Version = version, Now = DateTime.UtcNow },
            transaction);
    }

    private static EventMetadata? ExtractMetadata(IDomainEvent @event)
    {
        // Extraer metadata si el evento lo tiene
        if (@event is IHasMetadata withMeta)
            return withMeta.Metadata;
        
        return null;
    }

    #endregion
}

// Excepciones
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

public class EventStoreException : Exception
{
    public EventStoreException(string message, Exception? inner = null) : base(message, inner) { }
}

// Interface opcional para eventos con metadata
public interface IHasMetadata
{
    EventMetadata? Metadata { get; }
}