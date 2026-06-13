using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.FreezeCreditLine;

public class FreezeCreditLineCommandHandler : IRequestHandler<FreezeCreditLineCommand, FreezeCreditLineResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<FreezeCreditLineCommandHandler> _logger;

    public FreezeCreditLineCommandHandler(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<FreezeCreditLineCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<FreezeCreditLineResponse> Handle(
        FreezeCreditLineCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return FreezeCreditLineResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        try
        {
            aggregate.Freeze(request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot freeze credit line {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return FreezeCreditLineResponse.Failed(ex.Message);
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

        _logger.LogInformation("Credit line {CreditLineId} frozen. Reason: {Reason}",
            request.CreditLineId, request.Reason);

        return FreezeCreditLineResponse.Frozen(
            aggregate.Id,
            aggregate.State.FrozenAt!.Value);
    }
}