using System.Text.Json.Serialization;

namespace CreditSystem.Domain.ValueObjects;

public record AmortizationEntry
{
    public int PaymentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public Money TotalPayment { get; init; }
    public Money Principal { get; init; }
    public Money Interest { get; init; }
    public Money Balance { get; init; }

    // Constructor sin parámetros para deserialización
    [JsonConstructor]
    public AmortizationEntry()
    {
        TotalPayment = Money.Zero();
        Principal = Money.Zero();
        Interest = Money.Zero();
        Balance = Money.Zero();
    }

    public AmortizationEntry(
        int paymentNumber,
        DateTime dueDate,
        Money totalPayment,
        Money principal,
        Money interest,
        Money balance)
    {
        PaymentNumber = paymentNumber;
        DueDate = dueDate;
        TotalPayment = totalPayment;
        Principal = principal;
        Interest = interest;
        Balance = balance;
    }
}