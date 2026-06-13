using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit;

namespace CreditSystem.Infrastructure.Repositories;

// Infrastructure/Repositories/RevolvingCreditRepository.cs

public class RevolvingCreditRepository : IRevolvingCreditRepository
{
    private readonly IEventStore _eventStore;
    private const string StreamType = "RevolvingCredit";

    public RevolvingCreditRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<RevolvingCreditAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var events = await _eventStore.GetEventsAsync(id, 0, ct);

        if (!events.Any())
            return null;

        return new RevolvingCreditAggregate(null, events);
    }

    public async Task SaveAsync(RevolvingCreditAggregate aggregate, CancellationToken ct = default)
    {
        var uncommitted = aggregate.UncommittedEvents.ToList();

        if (!uncommitted.Any())
            return;

        var expectedVersion = aggregate.State.Version - uncommitted.Count;

        await _eventStore.AppendAsync(
            aggregate.Id,
            StreamType,
            uncommitted,
            expectedVersion,
            ct);

        aggregate.ClearUncommittedEvents();
    }
}