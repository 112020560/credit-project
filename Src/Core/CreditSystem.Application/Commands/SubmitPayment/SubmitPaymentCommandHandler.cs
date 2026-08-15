using System.Text.Json;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models.ReadModels;
using MediatR;
using Microsoft.Extensions.Logging;

// Use the interface from Domain layer

namespace CreditSystem.Application.Commands.SubmitPayment;

/// <summary>
/// Handler that submits a loan payment for asynchronous processing.
/// Performs basic validation, creates tracking record, and publishes to message queue.
/// Returns immediately with a tracking ID (202 Accepted pattern).
/// </summary>
public class SubmitPaymentCommandHandler : IRequestHandler<SubmitPaymentCommand, PaymentAcceptedResponse>
{
    private readonly ILoanContractRepository _loanRepository;
    private readonly IPaymentTrackingRepository _trackingRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<SubmitPaymentCommandHandler> _logger;

    public SubmitPaymentCommandHandler(
        ILoanContractRepository loanRepository,
        IPaymentTrackingRepository trackingRepository,
        IOutboxRepository outboxRepository,
        ILogger<SubmitPaymentCommandHandler> logger)
    {
        _loanRepository = loanRepository;
        _trackingRepository = trackingRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<PaymentAcceptedResponse> Handle(
        SubmitPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var paymentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Submitting payment {PaymentId} for loan {LoanId}. Amount: {Amount} {Currency}. CorrelationId: {CorrelationId}",
            paymentId, request.LoanId, request.Amount, request.Currency, correlationId);

        // 1. Basic validation - check loan exists and is active
        var loan = await _loanRepository.GetByIdAsync(request.LoanId, cancellationToken);

        if (loan == null)
        {
            _logger.LogWarning("Loan {LoanId} not found for payment submission", request.LoanId);
            return PaymentAcceptedResponse.Rejected($"Loan {request.LoanId} not found");
        }

        if (loan.State.Status != ContractStatus.Active)
        {
            _logger.LogWarning(
                "Loan {LoanId} is not active (status: {Status}). Payment rejected.",
                request.LoanId, loan.State.Status);
            return PaymentAcceptedResponse.Rejected(
                $"Loan is not active. Current status: {loan.State.Status}");
        }

        // 2. Validate payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out _))
        {
            return PaymentAcceptedResponse.Rejected($"Invalid payment method: {request.PaymentMethod}");
        }

        // 3. Create tracking record
        var tracking = new PaymentTrackingReadModel
        {
            PaymentId = paymentId,
            LoanId = request.LoanId,
            CreditLineId = null,
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
            LoanId = request.LoanId,
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
            MessageType = "ProcessPaymentMessage",
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = correlationId,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _outboxRepository.SaveAsync(outboxMessage, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} submitted successfully for loan {LoanId}. Tracking URL: /api/payments/{PaymentId}/status",
            paymentId, request.LoanId, paymentId);

        return PaymentAcceptedResponse.Accepted(
            paymentId,
            request.LoanId,
            $"/api/payments/{paymentId}/status");
    }
}
