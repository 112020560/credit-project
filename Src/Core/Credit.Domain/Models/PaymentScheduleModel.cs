using System;

namespace Credit.Domain.Models;

public class PaymentScheduleModel
{
    public int QuotaNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal CapitalAmmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal Balance { get; set; }
    public string? Status { get; set; }
}
