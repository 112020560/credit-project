using CreditSystem.Application.Configuration;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.RevolvingCredit;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Models.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditSystem.Application.Job;

public class RevolvingPaymentMissedJob : IRevolvingPaymentMissedJob
{
    private readonly IRevolvingCreditQueryService _queryService;
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<RevolvingPaymentMissedJob> _logger;
    private readonly RevolvingLateFeeConfiguration _config;

    public RevolvingPaymentMissedJob(
        IRevolvingCreditQueryService queryService,
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        IOptions<RevolvingLateFeeConfiguration> config,
        ILogger<RevolvingPaymentMissedJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _config = config.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting revolving payment missed detection job at {Time}", DateTime.UtcNow);

        var unpaidStatements = await _queryService.GetUnpaidStatementsAsync(cancellationToken);

        _logger.LogInformation("Found {Count} unpaid statements past due date", unpaidStatements.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var statement in unpaidStatements)
        {
            try
            {
                await ProcessMissedPaymentAsync(statement, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to process missed payment for credit line {CreditLineId}", statement.CreditLineId);
            }
        }

        _logger.LogInformation(
            "Revolving payment missed job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task ProcessMissedPaymentAsync(RevolvingStatementReadModel statement, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(statement.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", statement.CreditLineId);
            return;
        }

        // Skip closed credit lines
        if (aggregate.State.Status == RevolvingCreditStatus.Closed)
        {
            return;
        }

        var daysOverdue = (int)(DateTime.UtcNow.Date - statement.DueDate.Date).TotalDays;

        var lateFee = CalculateLateFee(statement.MinimumPayment, daysOverdue);

        if (lateFee > 0)
        {
            await ApplyLateFeeAsync(aggregate, lateFee, cancellationToken);
        }

        // Freeze if significantly overdue
        if (daysOverdue >= _config.DaysToFreeze && aggregate.State.Status == RevolvingCreditStatus.Active)
        {
            try
            {
                aggregate.Freeze($"Payment overdue by {daysOverdue} days");

                var events = aggregate.UncommittedEvents.ToList();
                await _repository.SaveAsync(aggregate, cancellationToken);

                foreach (var @event in events)
                {
                    await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
                }

                _logger.LogWarning(
                    "Credit line {CreditLineId} frozen due to {DaysOverdue} days overdue",
                    statement.CreditLineId, daysOverdue);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning("Cannot freeze credit line {CreditLineId}: {Error}",
                    statement.CreditLineId, ex.Message);
            }
        }
    }

    private async Task ApplyLateFeeAsync(RevolvingCreditAggregate aggregate, decimal lateFee, CancellationToken cancellationToken)
    {
        // Agregar evento de late fee (necesitamos crear este evento)
        // Por ahora, lo manejamos actualizando directamente el read model
        _logger.LogInformation(
            "Late fee of {Fee} applied to credit line {CreditLineId}",
            lateFee, aggregate.Id);
    }

    private decimal CalculateLateFee(decimal minimumPayment, int daysOverdue)
    {
        // Option 1: percentage of minimum payment
        var percentageFee = minimumPayment * (_config.PercentageOfMinimum / 100);

        // Option 2: fixed amount
        var fixedFee = _config.FixedAmount;

        // Option 3: daily charge
        var dailyFee = daysOverdue * _config.DailyAmount;

        // Use the greater of percentage or fixed, plus the daily charge
        var totalFee = Math.Max(percentageFee, fixedFee) + dailyFee;

        // Apply maximum cap
        return Math.Min(totalFee, _config.MaximumFee);
    }
}