using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Crm.Domain.Prospects;
using Crm.Domain.RiskEngine;

namespace Crm.Application.RiskEngine;

public class RiskEvaluationService(IUnitOfWork unitOfWork, IRiskEngine riskEngine)
{
    public async Task<(Domain.RiskEngine.RiskEvaluation evaluation, bool hasActiveMatrix)> EvaluateAsync(
        CreditApplication application,
        Prospect prospect,
        CancellationToken cancellationToken)
    {
        var matrix = await unitOfWork.RiskMatrixRepository.GetActiveAsync(cancellationToken);
        if (matrix is null)
            return (null!, false);

        var data = BuildData(application, prospect);
        var evaluation = riskEngine.Evaluate(matrix, data);

        await unitOfWork.RiskEvaluationsRepository.AddAsync(evaluation, cancellationToken);
        return (evaluation, true);
    }

    private static CreditApplicationData BuildData(CreditApplication application, Prospect prospect)
    {
        int? ageYears = null;
        if (prospect.BirthDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            ageYears = today.Year - prospect.BirthDate.Value.Year;
            if (prospect.BirthDate.Value > today.AddYears(-ageYears.Value)) ageYears--;
        }

        return new CreditApplicationData
        {
            ApplicationId = application.Id,
            ProspectId = prospect.Id,
            AgeYears = ageYears,
            MonthlyIncome = prospect.WorkInfos.FirstOrDefault()?.Salary,
            HasAddress = prospect.Addresses.Any(),
            HasWorkInfo = prospect.WorkInfos.Any(),
            HasFiscalInfo = prospect.FiscalInfos.Any(),
            DocumentCount = application.Documents.Count
        };
    }
}
