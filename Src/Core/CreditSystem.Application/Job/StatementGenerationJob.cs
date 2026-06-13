using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Job;

public class StatementGenerationJob : IStatementGenerationJob
{
    private readonly IRevolvingCreditQueryService _queryService;
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<StatementGenerationJob> _logger;

    public StatementGenerationJob(
        IRevolvingCreditQueryService queryService,
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<StatementGenerationJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting statement generation job at {Time}", DateTime.UtcNow);

        var today = DateTime.UtcNow.Date;
        var creditLinesDueForStatement = await _queryService.GetDueForStatementAsync(today, cancellationToken);

        _logger.LogInformation("Found {Count} credit lines due for statement generation", creditLinesDueForStatement.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var creditLine in creditLinesDueForStatement)
        {
            try
            {
                await GenerateStatementForCreditLineAsync(creditLine.CreditLineId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to generate statement for credit line {CreditLineId}", creditLine.CreditLineId);
            }
        }

        _logger.LogInformation(
            "Statement generation job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task GenerateStatementForCreditLineAsync(Guid creditLineId, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(creditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found for statement generation", creditLineId);
            return;
        }

        try
        {
            aggregate.GenerateStatement();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot generate statement for credit line {CreditLineId}: {Error}",
                creditLineId, ex.Message);
            return;
        }

        var events = aggregate.UncommittedEvents.ToList();
        await _repository.SaveAsync(aggregate, cancellationToken);

        foreach (var @event in events)
        {
            await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Statement generated for credit line {CreditLineId}. Due date: {DueDate}",
            creditLineId, aggregate.State.PaymentDueDate);
    }
}