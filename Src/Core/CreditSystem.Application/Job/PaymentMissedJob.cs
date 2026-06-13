using CreditSystem.Application.Configuration;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditSystem.Application.Job;

public class PaymentMissedJob : IPaymentMissedJob
{
    private readonly ILoanQueryService _queryService;
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<PaymentMissedJob> _logger;
    private readonly LateFeeConfiguration _lateFeeConfig;

    public PaymentMissedJob(
        ILoanQueryService queryService,
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        IOptions<LateFeeConfiguration> lateFeeConfig,
        ILogger<PaymentMissedJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _lateFeeConfig = lateFeeConfig.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting payment missed detection job at {Time}", DateTime.UtcNow);

        // 1. Obtener préstamos con pagos vencidos
        var overdueLoans = await _queryService.GetLoansWithOverduePaymentsAsync(cancellationToken);

        _logger.LogInformation("Found {Count} loans with overdue payments", overdueLoans.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var loan in overdueLoans)
        {
            try
            {
                await ProcessOverduePaymentAsync(loan, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to process overdue payment for loan {LoanId}", loan.LoanId);
            }
        }

        _logger.LogInformation(
            "Payment missed job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task ProcessOverduePaymentAsync(
        OverdueLoanInfo loan,
        CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(loan.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found for overdue processing", loan.LoanId);
            return;
        }

        // 2. Calcular late fee
        var lateFee = CalculateLateFee(loan);

        // 3. Ejecutar registro de pago perdido
        try
        {
            aggregate.RecordMissedPayment(
                loan.PaymentNumber,
                loan.DueDate,
                lateFee);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot record missed payment for loan {LoanId}: {Error}",
                loan.LoanId, ex.Message);
            return;
        }

        // 4. Persistir
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 5. Proyectar
        foreach (var @event in aggregate.UncommittedEvents)
        {
            await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Recorded missed payment #{PaymentNumber} for loan {LoanId}. Days overdue: {DaysOverdue}, Late fee: {LateFee}",
            loan.PaymentNumber,
            loan.LoanId,
            loan.DaysOverdue,
            lateFee.Amount);
    }

    private Money CalculateLateFee(OverdueLoanInfo loan)
    {
        // Opción 1: Porcentaje del pago
        var percentageFee = loan.AmountDue * (_lateFeeConfig.PercentageOfPayment / 100);

        // Opción 2: Monto fijo
        var fixedFee = _lateFeeConfig.FixedAmount;

        // Opción 3: Por día de atraso
        var dailyFee = loan.DaysOverdue * _lateFeeConfig.DailyAmount;

        // Usar el mayor entre porcentaje y fijo, más el cargo diario
        var baseFee = Math.Max(percentageFee, fixedFee);
        var totalFee = baseFee + dailyFee;

        // Aplicar tope máximo
        var cappedFee = Math.Min(totalFee, _lateFeeConfig.MaximumFee);

        return new Money(cappedFee, loan.Currency);
    }
}
