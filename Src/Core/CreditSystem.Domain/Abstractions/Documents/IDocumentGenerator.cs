using CreditSystem.Domain.Models.Documents;

namespace CreditSystem.Domain.Abstractions.Documents;

public interface IDocumentGenerator
{
    Task<byte[]> GenerateAsync<TData>(
        string templateName,
        TData data,
        DocumentFormat format,
        CancellationToken ct = default);
}
