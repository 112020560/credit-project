namespace CreditSystem.Domain.Entities;

public class CustomerCreditProfile
{
    private CustomerCreditProfile() { }

    public Guid Id { get; internal set; }
    public Guid ExternalId { get; internal set; }
    public string FullName { get; internal set; } = null!;
    public string? Email { get; internal set; }
    public string? Phone { get; internal set; }
    public string? DocumentType { get; internal set; }
    public string? DocumentNumber { get; internal set; }
    public int? CreditScore { get; internal set; }
    public decimal? MonthlyIncome { get; internal set; }
    public decimal? MonthlyDebt { get; internal set; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime UpdatedAt { get; internal set; }

    public static CustomerCreditProfile Create(
        Guid externalId,
        string fullName,
        string documentType,
        string documentNumber)
    {
        return new CustomerCreditProfile
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            FullName = fullName,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
