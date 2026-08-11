namespace CreditSystem.Domain.Abstractions;

public interface IMemberNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
