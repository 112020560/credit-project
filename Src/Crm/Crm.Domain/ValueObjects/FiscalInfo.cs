namespace Crm.Domain.ValueObjects;

public record FiscalInfo(
    string? TaxId,
    string? TaxRegime,
    string? EconomicActivity,
    string? Industry,
    string? Metadata = null);
