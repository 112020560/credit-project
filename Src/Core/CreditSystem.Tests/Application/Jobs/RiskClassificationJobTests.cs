using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditSystem.Tests.Application.Jobs;

public class RiskClassificationJobTests
{
    private readonly ILoanQueryService _queryService = Substitute.For<ILoanQueryService>();
    private readonly IRiskClassificationRepository _riskRepository = Substitute.For<IRiskClassificationRepository>();
    private readonly RiskClassificationService _classifier = new();

    private RiskClassificationJob CreateJob() =>
        new(_queryService, _riskRepository, _classifier, NullLogger<RiskClassificationJob>.Instance);

    [Fact]
    public async Task ExecuteAsync_LoanWithUnchangedCategory_SkipsUpdate()
    {
        // A loan already classified as A1 (0 days overdue) → still A1 → no update
        var loan = new LoanRiskInfo
        {
            LoanId = Guid.NewGuid(),
            CurrentBalance = 10_000m,
            DaysOverdue = 0,
            CurrentRiskCategory = "A1"
        };

        _queryService.GetLoansForRiskClassificationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LoanRiskInfo> { loan });

        await CreateJob().ExecuteAsync();

        await _riskRepository.DidNotReceive()
            .UpdateRiskCategoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LoanWithChangedCategory_CallsUpdateRepository()
    {
        // Loan currently A1 but now 45 days overdue → should become B1
        var loanId = Guid.NewGuid();
        var loan = new LoanRiskInfo
        {
            LoanId = loanId,
            CurrentBalance = 20_000m,
            DaysOverdue = 45,
            CurrentRiskCategory = "A1"
        };

        _queryService.GetLoansForRiskClassificationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LoanRiskInfo> { loan });

        await CreateJob().ExecuteAsync();

        await _riskRepository.Received(1)
            .UpdateRiskCategoryAsync(
                loanId,
                "B1",
                20_000m * 0.05m,   // B1 provision rate = 5%
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LoanWithNullCurrentCategory_TreatedAsChanged()
    {
        // First-time classification: CurrentRiskCategory is null
        var loanId = Guid.NewGuid();
        var loan = new LoanRiskInfo
        {
            LoanId = loanId,
            CurrentBalance = 5_000m,
            DaysOverdue = 0,
            CurrentRiskCategory = null
        };

        _queryService.GetLoansForRiskClassificationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LoanRiskInfo> { loan });

        await CreateJob().ExecuteAsync();

        // null ≠ "A1" so update is called
        await _riskRepository.Received(1)
            .UpdateRiskCategoryAsync(
                loanId,
                "A1",
                0m,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MultipleLoans_UpdatesOnlyChanged()
    {
        var loan1 = new LoanRiskInfo { LoanId = Guid.NewGuid(), CurrentBalance = 10_000m, DaysOverdue = 0,   CurrentRiskCategory = "A1" }; // unchanged
        var loan2 = new LoanRiskInfo { LoanId = Guid.NewGuid(), CurrentBalance = 10_000m, DaysOverdue = 45,  CurrentRiskCategory = "A1" }; // changed: A1 → B1
        var loan3 = new LoanRiskInfo { LoanId = Guid.NewGuid(), CurrentBalance = 10_000m, DaysOverdue = 200, CurrentRiskCategory = "D"  }; // unchanged

        _queryService.GetLoansForRiskClassificationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LoanRiskInfo> { loan1, loan2, loan3 });

        await CreateJob().ExecuteAsync();

        // Only loan2 triggers an update
        await _riskRepository.Received(1)
            .UpdateRiskCategoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
        await _riskRepository.Received(1)
            .UpdateRiskCategoryAsync(loan2.LoanId, "B1", 10_000m * 0.05m, Arg.Any<CancellationToken>());
    }
}
