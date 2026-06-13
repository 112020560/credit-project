using System.Security.Cryptography;
using System.Text;
using CreditSystem.Infrastructure.EventStore.Models;

namespace CreditSystem.Infrastructure.EventStore;

public class Sha256HashGenerator : IHashGenerator
{
    public string GenerateHash(
        Guid streamId, 
        string eventType, 
        string eventData, 
        int version, 
        string? previousHash)
    {
        var input = $"{streamId}|{eventType}|{eventData}|{version}|{previousHash ?? "GENESIS"}";
        
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyChain(IEnumerable<StoredEvent> events)
    {
        var eventList = events.OrderBy(e => e.Version).ToList();
        
        if (!eventList.Any())
            return true;

        // Verificar primer evento
        var first = eventList[0];
        var expectedFirstHash = GenerateHash(
            first.StreamId, 
            first.EventType, 
            first.EventData, 
            first.Version, 
            null);
        
        if (first.Hash != expectedFirstHash || first.PreviousHash != null)
            return false;

        // Verificar cadena
        for (int i = 1; i < eventList.Count; i++)
        {
            var current = eventList[i];
            var previous = eventList[i - 1];

            // Verificar que previousHash apunta al anterior
            if (current.PreviousHash != previous.Hash)
                return false;

            // Verificar hash del evento actual
            var expectedHash = GenerateHash(
                current.StreamId,
                current.EventType,
                current.EventData,
                current.Version,
                current.PreviousHash);

            if (current.Hash != expectedHash)
                return false;
        }

        return true;
    }
}