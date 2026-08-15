using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Domain.Abstractions.Services;

public interface IRevolvingCreditQueryService
{
    Task<RevolvingCreditSummaryReadModel?> GetSummaryAsync(Guid creditLineId, CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetActiveForInterestAccrualAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingCreditSummaryReadModel>> GetDueForStatementAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingTransactionReadModel>> GetTransactionsAsync(Guid creditLineId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingStatementReadModel>> GetStatementsAsync(Guid creditLineId, CancellationToken ct = default);
    Task<RevolvingStatementReadModel?> GetLatestStatementAsync(Guid creditLineId, CancellationToken ct = default);
    Task<IReadOnlyList<RevolvingStatementReadModel>> GetUnpaidStatementsAsync(CancellationToken ct = default);
}