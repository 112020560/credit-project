using System.Text.Json;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models.ReadModels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.SubmitRevolvingPayment;

/// <summary>
/// Handler that submits a revolving credit payment for asynchronous processing.
/// Performs basic validation, creates tracking record, and publishes to message queue.
/// Returns immediately with a tracking ID (202 Accepted pattern).
/// </summary>
public class SubmitRevolvingPaymentCommandHandler
    : IRequestHandler<SubmitRevolvingPaymentCommand, RevolvingPaymentAcceptedResponse>
{
    private readonly IRevolvingCreditRepository _creditRepository;
    private readonly IPaymentTrackingRepository _trackingRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<SubmitRevolvingPaymentCommandHandler> _logger;

    public SubmitRevolvingPaymentCommandHandler(
        IRevolvingCreditRepository creditRepository,
        IPaymentTrackingRepository trackingRepository,
        IOutboxRepository outboxRepository,
        ILogger<SubmitRevolvingPaymentCommandHandler> logger)
    {
        _creditRepository = creditRepository;
        _trackingRepository = trackingRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<RevolvingPaymentAcceptedResponse> Handle(
        SubmitRevolvingPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var paymentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Submitting payment {PaymentId} for credit line {CreditLineId}. Amount: {Amount} {Currency}. CorrelationId: {CorrelationId}",
            paymentId, request.CreditLineId, request.Amount, request.Currency, correlationId);

        // 1. Basic validation - check credit line exists and is active
        var creditLine = await _creditRepository.GetByIdAsync(request.CreditLineId, cancellationToken);

        if (creditLine == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found for payment submission", request.CreditLineId);
            return RevolvingPaymentAcceptedResponse.Rejected($"Credit line {request.CreditLineId} not found");
        }

        if (creditLine.State.Status != RevolvingCreditStatus.Active)
        {
            _logger.LogWarning(
                "Credit line {CreditLineId} is not active (status: {Status}). Payment rejected.",
                request.CreditLineId, creditLine.State.Status);
            return RevolvingPaymentAcceptedResponse.Rejected(
                $"Credit line is not active. Current status: {creditLine.State.Status}");
        }

        // 2. Validate payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out _))
        {
            return RevolvingPaymentAcceptedResponse.Rejected($"Invalid payment method: {request.PaymentMethod}");
        }

        // 3. Create tracking record
        var tracking = new PaymentTrackingReadModel
        {
            PaymentId = paymentId,
            LoanId = null,
            CreditLineId = request.CreditLineId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = PaymentTrackingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        };

        await _trackingRepository.CreateAsync(tracking, cancellationToken);

        // 4. Create outbox message for guaranteed delivery
        var message = new
        {
            PaymentId = paymentId,
            CreditLineId = request.CreditLineId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            CorrelationId = correlationId,
            RequestedAt = DateTimeOffset.UtcNow
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "ProcessRevolvingPaymentMessage",
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = correlationId,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _outboxRepository.SaveAsync(outboxMessage, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} submitted successfully for credit line {CreditLineId}. Tracking URL: /api/payments/{PaymentId}/status",
            paymentId, request.CreditLineId, paymentId);

        return RevolvingPaymentAcceptedResponse.Accepted(
            paymentId,
            request.CreditLineId,
            $"/api/payments/{paymentId}/status");
    }
}
