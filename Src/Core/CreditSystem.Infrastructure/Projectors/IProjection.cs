using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Infrastructure.Projectors;

public interface IProjection
{
    string ProjectionName { get; }
    Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default);
    Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default);
}

public interface IProjection<TEvent> : IProjection where TEvent : IDomainEvent
{
    Task ProjectAsync(TEvent @event, CancellationToken ct = default);
}