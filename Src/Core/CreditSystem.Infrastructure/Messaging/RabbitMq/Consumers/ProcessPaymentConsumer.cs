using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
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

public class ProcessPaymentConsumer : IConsumer<ProcessPaymentMessage>
{
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _connectionString;
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<ProcessPaymentConsumer> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _publishEndpoint = publishEndpoint;
        _connectionString = configuration.GetConnectionString("CreditDb")!;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPaymentMessage> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation(
            "Processing payment {PaymentId} for loan {LoanId}. Amount: {Amount} {Currency}",
            message.PaymentId, message.LoanId, message.Amount, message.Currency);

        try
        {
            // Update status to Processing
            await UpdatePaymentTrackingStatusAsync(
                message.PaymentId,
                PaymentTrackingStatus.Processing,
                cancellationToken);

            // Load aggregate
            var aggregate = await _repository.GetByIdAsync(message.LoanId, cancellationToken);

            if (aggregate == null)
            {
                _logger.LogWarning("Loan {LoanId} not found for payment {PaymentId}",
                    message.LoanId, message.PaymentId);

                await HandlePaymentRejectedAsync(message, "LOAN_NOT_FOUND",
                    $"Loan {message.LoanId} not found", cancellationToken);
                return;
            }

            // Validate loan status
            if (aggregate.State.Status != ContractStatus.Active)
            {
                _logger.LogWarning("Loan {LoanId} is not active (status: {Status}) for payment {PaymentId}",
                    message.LoanId, aggregate.State.Status, message.PaymentId);

                await HandlePaymentRejectedAsync(message, "LOAN_NOT_ACTIVE",
                    $"Loan is not active. Current status: {aggregate.State.Status}", cancellationToken);
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
                    "Business rule violation for payment {PaymentId} on loan {LoanId}: {Error}",
                    message.PaymentId, message.LoanId, ex.Message);

                await HandlePaymentRejectedAsync(message, "BUSINESS_RULE_VIOLATION",
                    ex.Message, cancellationToken);
                return;
            }

            // Get payment event details
            var paymentEvent = aggregate.UncommittedEvents
                .OfType<PaymentApplied>()
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
                    "Failed to project events for loan {LoanId}. Read models can be rebuilt.",
                    message.LoanId);
            }

            var isPaidOff = aggregate.State.Status == ContractStatus.PaidOff;

            // Update tracking as completed
            await UpdatePaymentTrackingCompletedAsync(
                message.PaymentId,
                paymentEvent.PrincipalPaid.Amount,
                paymentEvent.InterestPaid.Amount,
                paymentEvent.FeePaid.Amount,
                paymentEvent.NewBalance.Amount,
                isPaidOff,
                cancellationToken);

            // Publish success event
            await _publishEndpoint.Publish<PaymentProcessed>(new PaymentProcessedMessage
            {
                PaymentId = message.PaymentId,
                LoanId = message.LoanId,
                CustomerId = message.CustomerId,
                TotalApplied = paymentEvent.TotalAmount.Amount,
                Currency = message.Currency,
                PrincipalPaid = paymentEvent.PrincipalPaid.Amount,
                InterestPaid = paymentEvent.InterestPaid.Amount,
                FeesPaid = paymentEvent.FeePaid.Amount,
                NewBalance = paymentEvent.NewBalance.Amount,
                IsPaidOff = isPaidOff,
                PaymentMethod = message.PaymentMethod,
                ProcessedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} successfully processed for loan {LoanId}. " +
                "Principal: {Principal}, Interest: {Interest}, Fees: {Fees}, NewBalance: {Balance}, PaidOff: {PaidOff}",
                message.PaymentId, message.LoanId,
                paymentEvent.PrincipalPaid.Amount,
                paymentEvent.InterestPaid.Amount,
                paymentEvent.FeePaid.Amount,
                paymentEvent.NewBalance.Amount,
                isPaidOff);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing payment {PaymentId} for loan {LoanId}",
                message.PaymentId, message.LoanId);

            await HandlePaymentFailedAsync(message, "INTERNAL_ERROR",
                "An unexpected error occurred during payment processing", cancellationToken);

            throw; // Re-throw to trigger retry mechanism
        }
    }

    private async Task HandlePaymentRejectedAsync(
        ProcessPaymentMessage message,
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
            LoanId = message.LoanId,
            CreditLineId = null,
            CustomerId = message.CustomerId,
            RejectionCode = rejectionCode,
            RejectionReason = rejectionReason,
            RejectedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task HandlePaymentFailedAsync(
        ProcessPaymentMessage message,
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
            LoanId = message.LoanId,
            CreditLineId = null,
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

    private async Task UpdatePaymentTrackingCompletedAsync(
        Guid paymentId,
        decimal principalPaid,
        decimal interestPaid,
        decimal feesPaid,
        decimal newBalance,
        bool isPaidOff,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = 'COMPLETED',
                principal_paid = @PrincipalPaid,
                interest_paid = @InterestPaid,
                fees_paid = @FeesPaid,
                new_balance = @NewBalance,
                is_paid_off = @IsPaidOff,
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
            NewBalance = newBalance,
            IsPaidOff = isPaidOff,
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
