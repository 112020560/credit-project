using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.ApplyRevolvingPayment;

public class ApplyRevolvingPaymentCommandHandler : IRequestHandler<ApplyRevolvingPaymentCommand, ApplyRevolvingPaymentResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<ApplyRevolvingPaymentCommandHandler> _logger;

    public ApplyRevolvingPaymentCommandHandler(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<ApplyRevolvingPaymentCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<ApplyRevolvingPaymentResponse> Handle(
        ApplyRevolvingPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found", request.CreditLineId);
            return ApplyRevolvingPaymentResponse.Failed($"Credit line {request.CreditLineId} not found");
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return ApplyRevolvingPaymentResponse.Failed($"Invalid payment method: {request.PaymentMethod}");
        }

        var paymentId = Guid.NewGuid();

        try
        {
            aggregate.ApplyPayment(
                paymentId,
                new Money(request.Amount, request.Currency),
                paymentMethod);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Cannot apply payment to credit line {CreditLineId}: {Error}",
                request.CreditLineId, ex.Message);
            return ApplyRevolvingPaymentResponse.Failed(ex.Message);
        }

        var paymentEvent = aggregate.UncommittedEvents
            .OfType<RevolvingPaymentApplied>()
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
            "Payment {PaymentId} of {Amount} applied to credit line {CreditLineId}. New balance: {Balance}",
            paymentId, request.Amount, request.CreditLineId, paymentEvent.NewBalance.Amount);

        return ApplyRevolvingPaymentResponse.Applied(
            paymentId,
            aggregate.Id,
            paymentEvent.TotalAmount.Amount,
            paymentEvent.PrincipalPaid.Amount,
            paymentEvent.InterestPaid.Amount,
            paymentEvent.FeesPaid.Amount,
            paymentEvent.NewBalance.Amount,
            paymentEvent.AvailableCredit.Amount);
    }
}