using System;

namespace Credit.Domain.Models;

public class CreditLineModel
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

    public PaymentScheduleModel[]? AmortizationSchedule { get; set; }

    public string? Metadata { get; set; }
}
