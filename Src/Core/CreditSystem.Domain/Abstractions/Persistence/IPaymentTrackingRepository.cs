using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Domain.Abstractions.Persistence;

/// <summary>
/// Repository for payment tracking read models.
/// </summary>
public interface IPaymentTrackingRepository
{
    Task<PaymentTrackingReadModel?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTrackingReadModel>> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTrackingReadModel>> GetByCreditLineIdAsync(Guid creditLineId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTrackingReadModel>> GetByCustomerIdAsync(
        Guid customerId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task CreateAsync(PaymentTrackingReadModel tracking, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid paymentId,
        PaymentTrackingStatus status,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
