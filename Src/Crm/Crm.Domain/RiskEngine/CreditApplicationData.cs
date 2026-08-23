namespace Crm.Domain.RiskEngine;

public class CreditApplicationData
{
    public Guid ApplicationId { get; set; }
    public Guid ProspectId { get; set; }
    public int? AgeYears { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public bool HasAddress { get; set; }
    public bool HasWorkInfo { get; set; }
    public bool HasFiscalInfo { get; set; }
    public int DocumentCount { get; set; }
}
