using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.DefaultContract;

public class DefaultContractCommandHandler : IRequestHandler<DefaultContractCommand, DefaultContractResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<DefaultContractCommandHandler> _logger;

    public DefaultContractCommandHandler(
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<DefaultContractCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<DefaultContractResponse> Handle(
        DefaultContractCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return DefaultContractResponse.Failed($"Loan {request.LoanId} not found");
        }

        // 2. Verificar si ya está en default
        if (aggregate.State.Status == ContractStatus.Default)
        {
            return DefaultContractResponse.Failed("Contract is already in default status");
        }

        // 3. Ejecutar comando
        try
        {
            aggregate.MarkAsDefault(request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot default loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return DefaultContractResponse.Failed(ex.Message);
        }

        // Guardar eventos ANTES de persistir
        var events = aggregate.UncommittedEvents.ToList();

        // 4. Persistir (fuente de verdad)
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 5. Proyectar a Read Models
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

        _logger.LogInformation(
            "Loan {LoanId} marked as default. Reason: {Reason}. Total owed: {TotalOwed}",
            request.LoanId,
            request.Reason,
            aggregate.State.TotalOwed.Amount);

        return DefaultContractResponse.Defaulted(
            loanId: aggregate.Id,
            outstandingBalance: aggregate.State.CurrentBalance.Amount,
            accruedInterest: aggregate.State.AccruedInterest.Amount,
            totalOwed: aggregate.State.TotalOwed.Amount,
            defaultedAt: aggregate.State.DefaultedAt!.Value);
    }
}