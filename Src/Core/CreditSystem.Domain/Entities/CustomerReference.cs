namespace CreditSystem.Domain.Entities;

public class CustomerReference
{
    public Guid Id { get; set; }
    public Guid ExternalId { get; set; }  // ID del CRM
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public int? CreditScore { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public decimal? MonthlyDebt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}