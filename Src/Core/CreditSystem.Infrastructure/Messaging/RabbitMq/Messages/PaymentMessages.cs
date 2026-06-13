using SharedKernel.Contracts.Payments;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Messages;

/// <summary>
/// Concrete implementation of ProcessPaymentCommand for loan payments.
/// </summary>
public class ProcessPaymentMessage : ProcessPaymentCommand
{
    public Guid PaymentId { get; set; }
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}

/// <summary>
/// Concrete implementation of ProcessRevolvingPaymentCommand for revolving credit payments.
/// </summary>
public class ProcessRevolvingPaymentMessage : ProcessRevolvingPaymentCommand
{
    public Guid PaymentId { get; set; }
    public Guid CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}

/// <summary>
/// Concrete implementation of PaymentProcessed event for loan payments.
/// </summary>
public class PaymentProcessedMessage : PaymentProcessed
{
    public Guid PaymentId { get; set; }
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalApplied { get; set; }
    public string Currency { get; set; } = "MXN";
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal FeesPaid { get; set; }
    public decimal NewBalance { get; set; }
    public bool IsPaidOff { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}

/// <summary>
/// Concrete implementation of RevolvingPaymentProcessed event.
/// </summary>
public class RevolvingPaymentProcessedMessage : RevolvingPaymentProcessed
{
    public Guid PaymentId { get; set; }
    public Guid CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalApplied { get; set; }
    public string Currency { get; set; } = "MXN";
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal FeesPaid { get; set; }
    public decimal NewAvailableCredit { get; set; }
    public decimal NewUsedCredit { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}

/// <summary>
/// Concrete implementation of PaymentFailed event.
/// </summary>
public class PaymentFailedMessage : PaymentFailed
{
    public Guid PaymentId { get; set; }
    public Guid? LoanId { get; set; }
    public Guid? CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTimeOffset FailedAt { get; set; }
}

/// <summary>
/// Concrete implementation of PaymentRejected event.
/// </summary>
public class PaymentRejectedMessage : PaymentRejected
{
    public Guid PaymentId { get; set; }
    public Guid? LoanId { get; set; }
    public Guid? CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string RejectionCode { get; set; } = string.Empty;
    public DateTimeOffset RejectedAt { get; set; }
}
