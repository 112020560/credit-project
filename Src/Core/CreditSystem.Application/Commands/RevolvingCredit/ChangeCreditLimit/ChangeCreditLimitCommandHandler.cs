using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.ChangeCreditLimit;

public class ChangeCreditLimitCommandHandler : IRequestHandler<ChangeCreditLimitCommand, ChangeCreditLimitResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly ILogger<ChangeCreditLimitCommandHandler> _logger;

    public ChangeCreditLimitCommandHandler(
        IRevolvingCreditRepository repository,
        ILogger<ChangeCreditLimitCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ChangeCreditLimitResponse> Handle(
        ChangeCreditLimitCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return ChangeCreditLimitResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        var previousLimit = aggregate.State.CreditLimit.Amount;

        try
        {
            aggregate.ChangeCreditLimit(
                new Money(request.NewLimit, request.Currency),
                request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot change credit limit for {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return ChangeCreditLimitResponse.Failed(ex.Message);
        }

        await _repository.SaveAsync(aggregate, cancellationToken);

        _logger.LogInformation(
            "Credit limit changed for {CreditLineId}: {Previous} -> {New}",
            request.CreditLineId, previousLimit, request.NewLimit);

        return ChangeCreditLimitResponse.Changed(
            aggregate.Id,
            previousLimit,
            request.NewLimit,
            aggregate.State.AvailableCredit.Amount);
    }
}