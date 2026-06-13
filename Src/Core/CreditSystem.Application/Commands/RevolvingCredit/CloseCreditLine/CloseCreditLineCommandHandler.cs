using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.CloseCreditLine;

public class CloseCreditLineCommandHandler : IRequestHandler<CloseCreditLineCommand, CloseCreditLineResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<CloseCreditLineCommandHandler> _logger;

    public CloseCreditLineCommandHandler(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<CloseCreditLineCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<CloseCreditLineResponse> Handle(
        CloseCreditLineCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return CloseCreditLineResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        try
        {
            aggregate.Close(request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot close credit line {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return CloseCreditLineResponse.Failed(ex.Message);
        }

        var events = aggregate.UncommittedEvents.ToList();
        await _repository.SaveAsync(aggregate, cancellationToken);

        // Proyectar a Read Models
        try
        {
            await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to project events for credit line {CreditLineId}. Read models can be rebuilt.",
                aggregate.Id);
        }

        _logger.LogInformation("Credit line {CreditLineId} closed. Reason: {Reason}",
            request.CreditLineId, request.Reason);

        return CloseCreditLineResponse.Closed(
            aggregate.Id,
            aggregate.State.ClosedAt!.Value);
    }
}