using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Api.EndPoints.Dtos;

public record ConfirmDisbursementRequest
{
    public string ConfirmedBy { get; init; } = null!;
}

public record FailDisbursementRequest
{
    public string Reason { get; init; } = null!;
}

public record PendingDisbursementResponse
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Principal { get; init; }
    public string Currency { get; init; } = null!;
    public string? DisbursementMethod { get; init; }
    public string? DestinationAccount { get; init; }
    public DateTime ApprovedAt { get; init; }
    public DateTime? DisbursementInstructedAt { get; init; }
    public string Status { get; init; } = null!;
    public DateTime UpdatedAt { get; init; }

    public static PendingDisbursementResponse FromReadModel(PendingDisbursementReadModel m) => new()
    {
        LoanId = m.LoanId,
        CustomerId = m.CustomerId,
        CustomerName = m.CustomerName,
        Principal = m.Principal,
        Currency = m.Currency,
        DisbursementMethod = m.DisbursementMethod,
        DestinationAccount = m.DestinationAccount,
        ApprovedAt = m.ApprovedAt,
        DisbursementInstructedAt = m.DisbursementInstructedAt,
        Status = m.Status,
        UpdatedAt = m.UpdatedAt
    };
}
