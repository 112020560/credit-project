using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.ValueObjects;
using CreditSystem.Infrastructure.Messaging.RabbitMq.Messages;
using Dapper;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel.Contracts.Payments;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;

public class ProcessRevolvingPaymentConsumer : IConsumer<ProcessRevolvingPaymentMessage>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _connectionString;
    private readonly ILogger<ProcessRevolvingPaymentConsumer> _logger;

    public ProcessRevolvingPaymentConsumer(
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<ProcessRevolvingPaymentConsumer> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _publishEndpoint = publishEndpoint;
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessRevolvingPaymentMessage> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation(
            "Processing revolving payment {PaymentId} for credit line {CreditLineId}. Amount: {Amount} {Currency}",
            message.PaymentId, message.CreditLineId, message.Amount, message.Currency);

        try
        {
            // Update status to Processing
            await UpdatePaymentTrackingStatusAsync(
                message.PaymentId,
                PaymentTrackingStatus.Processing,
                cancellationToken);

            // Load aggregate
            var aggregate = await _repository.GetByIdAsync(message.CreditLineId, cancellationToken);

            if (aggregate == null)
            {
                _logger.LogWarning("Credit line {CreditLineId} not found for payment {PaymentId}",
                    message.CreditLineId, message.PaymentId);

                await HandlePaymentRejectedAsync(message, "CREDIT_LINE_NOT_FOUND",
                    $"Credit line {message.CreditLineId} not found", cancellationToken);
                return;
            }

            // Validate credit line status
            if (aggregate.State.Status != RevolvingCreditStatus.Active)
            {
                _logger.LogWarning(
                    "Credit line {CreditLineId} is not active (status: {Status}) for payment {PaymentId}",
                    message.CreditLineId, aggregate.State.Status, message.PaymentId);

                await HandlePaymentRejectedAsync(message, "CREDIT_LINE_NOT_ACTIVE",
                    $"Credit line is not active. Current status: {aggregate.State.Status}", cancellationToken);
                return;
            }

            // Parse payment method
            if (!Enum.TryParse<PaymentMethod>(message.PaymentMethod, true, out var paymentMethod))
            {
                await HandlePaymentRejectedAsync(message, "INVALID_PAYMENT_METHOD",
                    $"Invalid payment method: {message.PaymentMethod}", cancellationToken);
                return;
            }

            // Apply payment to aggregate
            var amount = new Money(message.Amount, message.Currency);

            try
            {
                aggregate.ApplyPayment(message.PaymentId, amount, paymentMethod);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    "Business rule violation for payment {PaymentId} on credit line {CreditLineId}: {Error}",
                    message.PaymentId, message.CreditLineId, ex.Message);

                await HandlePaymentRejectedAsync(message, "BUSINESS_RULE_VIOLATION",
                    ex.Message, cancellationToken);
                return;
            }

            // Get payment event details
            var paymentEvent = aggregate.UncommittedEvents
                .OfType<RevolvingPaymentApplied>()
                .FirstOrDefault();

            if (paymentEvent == null)
            {
                await HandlePaymentFailedAsync(message, "EVENT_NOT_GENERATED",
                    "Payment event was not generated", cancellationToken);
                return;
            }

            // Persist events
            var events = aggregate.UncommittedEvents.ToList();
            await _repository.SaveAsync(aggregate, cancellationToken);

            // Project events to read models
            try
            {
                await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to project events for credit line {CreditLineId}. Read models can be rebuilt.",
                    message.CreditLineId);
            }

            // Update tracking as completed
            await UpdateRevolvingPaymentTrackingCompletedAsync(
                message.PaymentId,
                paymentEvent.PrincipalPaid.Amount,
                paymentEvent.InterestPaid.Amount,
                paymentEvent.FeesPaid.Amount,
                paymentEvent.NewBalance.Amount,
                paymentEvent.AvailableCredit.Amount,
                cancellationToken);

            // Publish success event
            await _publishEndpoint.Publish<RevolvingPaymentProcessed>(new RevolvingPaymentProcessedMessage
            {
                PaymentId = message.PaymentId,
                CreditLineId = message.CreditLineId,
                CustomerId = message.CustomerId,
                TotalApplied = paymentEvent.TotalAmount.Amount,
                Currency = message.Currency,
                PrincipalPaid = paymentEvent.PrincipalPaid.Amount,
                InterestPaid = paymentEvent.InterestPaid.Amount,
                FeesPaid = paymentEvent.FeesPaid.Amount,
                NewAvailableCredit = paymentEvent.AvailableCredit.Amount,
                NewUsedCredit = paymentEvent.NewBalance.Amount,
                PaymentMethod = message.PaymentMethod,
                ProcessedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation(
                "Revolving payment {PaymentId} successfully processed for credit line {CreditLineId}. " +
                "Principal: {Principal}, Interest: {Interest}, Fees: {Fees}, NewUsedCredit: {UsedCredit}",
                message.PaymentId, message.CreditLineId,
                paymentEvent.PrincipalPaid.Amount,
                paymentEvent.InterestPaid.Amount,
                paymentEvent.FeesPaid.Amount,
                paymentEvent.NewBalance.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing payment {PaymentId} for credit line {CreditLineId}",
                message.PaymentId, message.CreditLineId);

            await HandlePaymentFailedAsync(message, "INTERNAL_ERROR",
                "An unexpected error occurred during payment processing", cancellationToken);

            throw; // Re-throw to trigger retry mechanism
        }
    }

    private async Task HandlePaymentRejectedAsync(
        ProcessRevolvingPaymentMessage message,
        string rejectionCode,
        string rejectionReason,
        CancellationToken cancellationToken)
    {
        await UpdatePaymentTrackingFailedAsync(
            message.PaymentId,
            PaymentTrackingStatus.Rejected,
            rejectionCode,
            rejectionReason,
            cancellationToken);

        await _publishEndpoint.Publish<PaymentRejected>(new PaymentRejectedMessage
        {
            PaymentId = message.PaymentId,
            LoanId = null,
            CreditLineId = message.CreditLineId,
            CustomerId = message.CustomerId,
            RejectionCode = rejectionCode,
            RejectionReason = rejectionReason,
            RejectedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task HandlePaymentFailedAsync(
        ProcessRevolvingPaymentMessage message,
        string errorCode,
        string errorReason,
        CancellationToken cancellationToken)
    {
        await UpdatePaymentTrackingFailedAsync(
            message.PaymentId,
            PaymentTrackingStatus.Failed,
            errorCode,
            errorReason,
            cancellationToken);

        await _publishEndpoint.Publish<PaymentFailed>(new PaymentFailedMessage
        {
            PaymentId = message.PaymentId,
            LoanId = null,
            CreditLineId = message.CreditLineId,
            CustomerId = message.CustomerId,
            ErrorCode = errorCode,
            FailureReason = errorReason,
            FailedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task UpdatePaymentTrackingStatusAsync(
        Guid paymentId,
        PaymentTrackingStatus status,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = @Status, updated_at = @UpdatedAt
            WHERE payment_id = @PaymentId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            PaymentId = paymentId,
            Status = status.ToString().ToUpperInvariant(),
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));
    }

    private async Task UpdateRevolvingPaymentTrackingCompletedAsync(
        Guid paymentId,
        decimal principalPaid,
        decimal interestPaid,
        decimal feesPaid,
        decimal newUsedCredit,
        decimal newAvailableCredit,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = 'COMPLETED',
                principal_paid = @PrincipalPaid,
                interest_paid = @InterestPaid,
                fees_paid = @FeesPaid,
                new_balance = @NewUsedCredit,
                new_available_credit = @NewAvailableCredit,
                updated_at = @UpdatedAt,
                processed_at = @ProcessedAt
            WHERE payment_id = @PaymentId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            PaymentId = paymentId,
            PrincipalPaid = principalPaid,
            InterestPaid = interestPaid,
            FeesPaid = feesPaid,
            NewUsedCredit = newUsedCredit,
            NewAvailableCredit = newAvailableCredit,
            UpdatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));
    }

    private async Task UpdatePaymentTrackingFailedAsync(
        Guid paymentId,
        PaymentTrackingStatus status,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = @Status,
                error_code = @ErrorCode,
                error_message = @ErrorMessage,
                updated_at = @UpdatedAt,
                processed_at = @ProcessedAt
            WHERE payment_id = @PaymentId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            PaymentId = paymentId,
            Status = status.ToString().ToUpperInvariant(),
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            UpdatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));
    }
}
