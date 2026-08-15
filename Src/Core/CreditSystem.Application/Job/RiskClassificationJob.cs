using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Job;

public class RiskClassificationJob : IRiskClassificationJob
{
    private readonly ILoanQueryService _queryService;
    private readonly IRiskClassificationRepository _riskRepository;
    private readonly RiskClassificationService _classifier;
    private readonly ILogger<RiskClassificationJob> _logger;

    public RiskClassificationJob(
        ILoanQueryService queryService,
        IRiskClassificationRepository riskRepository,
        RiskClassificationService classifier,
        ILogger<RiskClassificationJob> logger)
    {
        _queryService = queryService;
        _riskRepository = riskRepository;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting risk classification job at {Time}", DateTime.UtcNow);

        var loans = await _queryService.GetLoansForRiskClassificationAsync(cancellationToken);

        _logger.LogInformation("Classifying {Count} loans", loans.Count);

        var changed = 0;
        var errors = 0;

        foreach (var loan in loans)
        {
            try
            {
                var (category, provision) = _classifier.Classify(loan.DaysOverdue, loan.CurrentBalance);
                var categoryName = category.ToString();

                if (categoryName == loan.CurrentRiskCategory)
                    continue;

                await _riskRepository.UpdateRiskCategoryAsync(
                    loan.LoanId, categoryName, provision, cancellationToken);

                var previousCategory = Enum.TryParse<Domain.Enums.LoanRiskCategory>(
                    loan.CurrentRiskCategory, out var prev) ? prev : (Domain.Enums.LoanRiskCategory?)null;

                _logger.LogInformation(
                    "Loan {LoanId} reclassified: {Previous} → {New} (days overdue: {Days}, provision: {Provision:N2})",
                    loan.LoanId, loan.CurrentRiskCategory ?? "none", categoryName,
                    loan.DaysOverdue, provision);

                changed++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Failed to classify loan {LoanId}", loan.LoanId);
            }
        }

        _logger.LogInformation(
            "Risk classification job completed. Changed: {Changed}, Errors: {Errors}",
            changed, errors);
    }
}
