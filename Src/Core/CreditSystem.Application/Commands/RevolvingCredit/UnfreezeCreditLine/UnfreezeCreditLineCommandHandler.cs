using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.UnfreezeCreditLine;

public class UnfreezeCreditLineCommandHandler : IRequestHandler<UnfreezeCreditLineCommand, UnfreezeCreditLineResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<UnfreezeCreditLineCommandHandler> _logger;

    public UnfreezeCreditLineCommandHandler(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<UnfreezeCreditLineCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<UnfreezeCreditLineResponse> Handle(
        UnfreezeCreditLineCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return UnfreezeCreditLineResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        try
        {
            aggregate.Unfreeze(request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot unfreeze credit line {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return UnfreezeCreditLineResponse.Failed(ex.Message);
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

        _logger.LogInformation("Credit line {CreditLineId} unfrozen. Reason: {Reason}",
            request.CreditLineId, request.Reason);

        var unfrozenEvent = events.OfType<CreditLineUnfrozen>().First();

        return UnfreezeCreditLineResponse.Unfrozen(
            aggregate.Id,
            unfrozenEvent.UnfrozenAt);
    }
}