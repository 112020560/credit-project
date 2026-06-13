using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Domain.Abstractions.Services;

public interface ILoanQueryService
{
    Task<LoanSummaryReadModel?> GetLoanSummaryAsync(Guid loanId, CancellationToken ct = default);
    Task<IReadOnlyList<LoanSummaryReadModel>> GetCustomerLoansAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<DelinquentLoanReadModel>> GetDelinquentLoansAsync(int? minDaysOverdue = null, string? collectionStatus = null,CancellationToken ct = default);
    Task<IReadOnlyList<PaymentHistoryReadModel>> GetPaymentHistoryAsync(Guid loanId, CancellationToken ct = default);
    Task<LoanPortfolioReadModel?> GetPortfolioSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UpcomingPaymentReadModel>> GetUpcomingPaymentsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<bool> HasActiveLoansAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<ActiveLoanForAccrual>> GetActiveLoansForAccrualAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OverdueLoanInfo>> GetLoansWithOverduePaymentsAsync(CancellationToken ct = default);
    
    Task<IReadOnlyList<DefaultedLoanReadModel>> GetDefaultedLoansAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<PaidOffLoanReadModel>> GetPaidOffLoansAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? earlyPayoffOnly = null,
        CancellationToken ct = default);
}
