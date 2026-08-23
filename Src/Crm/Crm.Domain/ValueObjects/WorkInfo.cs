namespace Crm.Domain.ValueObjects;

public record WorkInfo(
    string? Occupation,
    string? EmployerName,
    decimal? Salary,
    string? WorkAddress = null,
    string? Metadata = null);
