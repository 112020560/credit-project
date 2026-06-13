using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.DrawFunds;

public class DrawFundsCommandHandler : IRequestHandler<DrawFundsCommand, DrawFundsResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<DrawFundsCommandHandler> _logger;

    public DrawFundsCommandHandler(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<DrawFundsCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<DrawFundsResponse> Handle(
        DrawFundsCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return DrawFundsResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        try
        {
            aggregate.DrawFunds(
                new Money(request.Amount, request.Currency),
                request.Description);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot draw funds from credit line {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return DrawFundsResponse.Failed(ex.Message);
        }

        var fundsDrawnEvent = aggregate.UncommittedEvents
            .OfType<FundsDrawn>()
            .First();

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

        _logger.LogInformation(
            "Drew {Amount} from credit line {CreditLineId}. New balance: {Balance}, Available: {Available}",
            request.Amount, request.CreditLineId, 
            fundsDrawnEvent.NewBalance.Amount, 
            fundsDrawnEvent.AvailableCredit.Amount);

        return DrawFundsResponse.Drawn(
            fundsDrawnEvent.DrawId,
            aggregate.Id,
            fundsDrawnEvent.Amount.Amount,
            fundsDrawnEvent.NewBalance.Amount,
            fundsDrawnEvent.AvailableCredit.Amount,
            fundsDrawnEvent.DrawnAt);
    }
}