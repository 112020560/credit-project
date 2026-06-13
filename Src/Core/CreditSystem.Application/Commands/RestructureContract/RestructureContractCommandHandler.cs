using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RestructureContract;

public class RestructureContractCommandHandler : IRequestHandler<RestructureContractCommand, RestructureContractResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<RestructureContractCommandHandler> _logger;

    public RestructureContractCommandHandler(
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<RestructureContractCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<RestructureContractResponse> Handle(
        RestructureContractCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return RestructureContractResponse.Failed($"Loan {request.LoanId} not found");
        }

        // 2. Guardar valores anteriores para respuesta
        var previousRate = aggregate.State.InterestRate.AnnualRate;
        var previousTermMonths = aggregate.State.TermMonths;

        // 3. Preparar nuevos valores
        var newRate = new InterestRate(request.NewInterestRate);
        var forgiveAmount = new Money(request.ForgiveAmount ?? 0, aggregate.State.Principal.Currency);

        // 4. Validar que el monto a perdonar no exceda el balance
        if (forgiveAmount.Amount > aggregate.State.CurrentBalance.Amount)
        {
            return RestructureContractResponse.Failed(
                $"Forgive amount ({forgiveAmount.Amount}) cannot exceed current balance ({aggregate.State.CurrentBalance.Amount})");
        }

        // 5. Ejecutar comando
        try
        {
            aggregate.Restructure(
                newRate,
                request.NewTermMonths,
                forgiveAmount,
                request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot restructure loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return RestructureContractResponse.Failed(ex.Message);
        }

        var events = aggregate.UncommittedEvents.ToList();

        // 6. Persistir (fuente de verdad)
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 7. Proyectar a Read Models
        try
        {
            await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to project events for loan {LoanId}. Read models can be rebuilt.",
                aggregate.Id);
        }

        // 8. Calcular nuevo pago mensual
        var newMonthlyPayment = aggregate.State.Schedule.Entries.FirstOrDefault()?.TotalPayment.Amount ?? 0;

        _logger.LogInformation(
            "Loan {LoanId} restructured. Rate: {PrevRate}% -> {NewRate}%, Term: {PrevTerm} -> {NewTerm} months, Forgiven: {Forgiven}",
            request.LoanId,
            previousRate,
            request.NewInterestRate,
            previousTermMonths,
            request.NewTermMonths,
            forgiveAmount.Amount);

        return RestructureContractResponse.Restructured(
            loanId: aggregate.Id,
            previousRate: previousRate,
            newRate: request.NewInterestRate,
            previousTermMonths: previousTermMonths,
            newTermMonths: request.NewTermMonths,
            amountForgiven: forgiveAmount.Amount,
            newBalance: aggregate.State.CurrentBalance.Amount,
            newMonthlyPayment: newMonthlyPayment);
    }
}