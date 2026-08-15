using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Application.Queries.GetPaymentHistory;

public record PaymentHistoryResponse
{
    public Guid PaymentId { get; init; }
    public int PaymentNumber { get; init; }
    public DateTime PaymentDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PrincipalPaid { get; init; }
    public decimal InterestPaid { get; init; }
    public decimal FeesPaid { get; init; }
    public decimal BalanceAfter { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string Status { get; init; } = null!;

    public static PaymentHistoryResponse FromReadModel(PaymentHistoryReadModel model) => new()
    {
        PaymentId = model.Id,
        PaymentNumber = model.PaymentNumber,
        PaymentDate = model.PaymentDate,
        TotalAmount = model.TotalAmount,
        PrincipalPaid = model.PrincipalPaid,
        InterestPaid = model.InterestPaid,
        FeesPaid = model.FeesPaid,
        BalanceAfter = model.BalanceAfter,
        PaymentMethod = model.PaymentMethod ?? "Unknown",
        Status = model.Status
    };
}