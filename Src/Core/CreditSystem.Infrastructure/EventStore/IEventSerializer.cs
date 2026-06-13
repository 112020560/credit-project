using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Infrastructure.EventStore.Models;

namespace CreditSystem.Infrastructure.EventStore;

public interface IEventSerializer
{
    string Serialize(IDomainEvent @event);
    IDomainEvent Deserialize(string eventType, string data);
    string SerializeMetadata(EventMetadata? metadata);
    EventMetadata? DeserializeMetadata(string? data);
    string SerializeState<TState>(TState state);
    TState? DeserializeState<TState>(string data);
}