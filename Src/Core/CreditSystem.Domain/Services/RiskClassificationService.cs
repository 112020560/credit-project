using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Services;

public class RiskClassificationService
{
    public (LoanRiskCategory Category, decimal EstimatedProvision) Classify(int daysOverdue, decimal currentBalance)
    {
        var category = RiskCategoryTable.GetCategory(daysOverdue);
        var provision = currentBalance * RiskCategoryTable.GetProvisionRate(category);
        return (category, provision);
    }
}
