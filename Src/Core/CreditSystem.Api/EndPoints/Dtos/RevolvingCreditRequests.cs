namespace CreditSystem.Api.EndPoints.Dtos;

public record CreateCreditLineRequest
{
    public Guid ExternalCustomerId { get; init; }
    public decimal CreditLimit { get; init; }
    public string? Currency { get; init; }
    public decimal? InterestRate { get; init; }
    public decimal? MinimumPaymentPercentage { get; init; }
    public decimal? MinimumPaymentAmount { get; init; }
    public int? BillingCycleDay { get; init; }
    public int? GracePeriodDays { get; init; }
}

public record DrawFundsRequest
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string Description { get; init; } = null!;
}

public record ApplyRevolvingPaymentRequest
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string PaymentMethod { get; init; } = null!;
}

public record ChangeCreditLimitRequest
{
    public decimal NewLimit { get; init; }
    public string? Currency { get; init; }
    public string Reason { get; init; } = null!;
}

public record FreezeCreditLineRequest
{
    public string Reason { get; init; } = null!;
}

public record UnfreezeCreditLineRequest
{
    public string Reason { get; init; } = null!;
}

public record CloseCreditLineRequest
{
    public string Reason { get; init; } = null!;
}