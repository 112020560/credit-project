using CreditSystem.Infrastructure.EventStore.Models;

namespace CreditSystem.Infrastructure.EventStore;

public interface IHashGenerator
{
    string GenerateHash(Guid streamId, string eventType, string eventData, int version, string? previousHash);
    bool VerifyChain(IEnumerable<StoredEvent> events);
}