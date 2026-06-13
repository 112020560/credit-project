using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Infrastructure.EventStore;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Repositories;

// Infrastructure/Repositories/LoanContractRepository.cs
public class LoanContractRepository : ILoanContractRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<LoanContractRepository> _logger;
    private const int SnapshotInterval = 50; // Snapshot cada 50 eventos

    public LoanContractRepository(IEventStore eventStore, ILogger<LoanContractRepository> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task<LoanContractAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Intentar cargar desde snapshot
        var (snapshot, snapshotVersion) = await _eventStore
            .GetLatestSnapshotAsync<LoanContractState>(id, ct);

        IReadOnlyList<IDomainEvent> events;

        if (snapshot != null)
        {
            // Cargar solo eventos después del snapshot
            events = await _eventStore.GetEventsAsync(id, snapshotVersion + 1, ct);
            _logger.LogDebug(
                "Loaded aggregate {Id} from snapshot v{Version} + {EventCount} events",
                id, snapshotVersion, events.Count);
        }
        else
        {
            // Cargar todos los eventos
            events = await _eventStore.GetEventsAsync(id, 0, ct);
            
            if (!events.Any())
                return null;
        }

        return new LoanContractAggregate(snapshot, events);
    }

    public async Task SaveAsync(LoanContractAggregate aggregate, CancellationToken ct = default)
    {
        var uncommitted = aggregate.UncommittedEvents;

        if (!uncommitted.Any())
            return;

        await _eventStore.AppendAsync(
            aggregate.Id,
            "LoanContract",
            uncommitted,
            aggregate.State.Version - uncommitted.Count,
            ct);

        // Guardar snapshot si es necesario
        if (aggregate.State.Version % SnapshotInterval == 0)
        {
            await _eventStore.SaveSnapshotAsync(
                aggregate.Id,
                aggregate.State,
                aggregate.State.Version,
                ct);
        }

        aggregate.ClearUncommittedEvents();
    }
}