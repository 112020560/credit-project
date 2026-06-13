using System;

namespace Credit.Domain.Models;

public class CreditLineBalanceModel
{
    public decimal PrincipalOutstanding { get; set; }
    public decimal InterestOutstanding { get; set; }
    public decimal FeesOutstanding { get; set; }
    public DateTime NextInstallmentDue { get; set; }
}
