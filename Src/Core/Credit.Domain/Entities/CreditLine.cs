using System.Text.Json;
using System.Text.Json.Nodes;
using Credit.Domain.Models;

namespace Credit.Domain.Entities;

public partial class CreditLine
{
    public Guid Id { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? ProductId { get; set; }

    public decimal Principal { get; set; }

    public decimal Outstanding { get; set; }

    public string Currency { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public string? AmortizationSchedule { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CreditApplication? Application { get; set; }

    public virtual ICollection<CreditPayment> CreditPayments { get; set; } = new List<CreditPayment>();

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();

    public virtual CreditProduct? Product { get; set; }

    public CreditLineModel ToModel()
    {
        return new CreditLineModel
        {
            Id = Id,
            ApplicationId = ApplicationId,
            CustomerId = CustomerId,
            ProductId = ProductId,
            Principal = Principal,
            Outstanding = Outstanding,
            Currency = Currency,
            StartDate = StartDate,
            EndDate = EndDate,
            Status = Status,
            AmortizationSchedule = !string.IsNullOrEmpty(AmortizationSchedule)
                ? JsonSerializer.Deserialize<PaymentScheduleModel[]>(AmortizationSchedule) : null,
            Metadata = Metadata,
        };
    }
}
