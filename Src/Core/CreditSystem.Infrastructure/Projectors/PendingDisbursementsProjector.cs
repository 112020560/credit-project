using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projectors;

public class PendingDisbursementsProjector : IProjection
{
    private readonly IProjectionStore _store;
    private readonly ICustomerReadRepository _customerService;
    private readonly ILogger<PendingDisbursementsProjector> _logger;

    public string ProjectionName => "PendingDisbursements";

    public PendingDisbursementsProjector(
        IProjectionStore store,
        ICustomerReadRepository customerService,
        ILogger<PendingDisbursementsProjector> logger)
    {
        _store = store;
        _customerService = customerService;
        _logger = logger;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        switch (@event)
        {
            case ContractCreated e:
                await HandleContractCreated(e, ct);
                break;
            case LoanDisbursed e:
                await HandleLoanDisbursed(e, ct);
                break;
            case DisbursementConfirmed e:
                await HandleDisbursementConfirmed(e, ct);
                break;
            case DisbursementFailed e:
                await HandleDisbursementFailed(e, ct);
                break;
            case ContractDefaulted e:
                await HandleRemove(e.AggregateId, ct);
                break;
            case ContractPaidOff e:
                await HandleRemove(e.AggregateId, ct);
                break;
        }
    }

    private async Task HandleContractCreated(ContractCreated e, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(e.CustomerId, ct);

        var model = new PendingDisbursementReadModel
        {
            LoanId = e.AggregateId,
            CustomerId = e.CustomerId,
            CustomerName = customer?.FullName,
            Principal = e.Principal?.Amount ?? 0,
            Currency = e.Principal?.Currency ?? "USD",
            DisbursementMethod = null,
            DestinationAccount = null,
            ApprovedAt = e.OccurredAt,
            DisbursementInstructedAt = null,
            Status = "Approved",
            UpdatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_pending_disbursements", model, "loan_id", ct);
        _logger.LogDebug("PendingDisbursements: added loan {LoanId} (Approved)", e.AggregateId);
    }

    private async Task HandleLoanDisbursed(LoanDisbursed e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_pending_disbursements
            SET status = 'Disbursing',
                disbursement_method = @Method,
                destination_account = @Account,
                disbursement_instructed_at = @InstructedAt,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            Method = e.DisbursementMethod,
            Account = e.DestinationAccount,
            InstructedAt = e.DisbursedAt,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("PendingDisbursements: loan {LoanId} → Disbursing", e.AggregateId);
    }

    private async Task HandleDisbursementConfirmed(DisbursementConfirmed e, CancellationToken ct)
    {
        const string sql = "DELETE FROM rm_pending_disbursements WHERE loan_id = @LoanId";
        await _store.ExecuteAsync(sql, new { LoanId = e.AggregateId }, ct);
        _logger.LogDebug("PendingDisbursements: loan {LoanId} confirmed — removed", e.AggregateId);
    }

    private async Task HandleDisbursementFailed(DisbursementFailed e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_pending_disbursements
            SET status = 'Approved',
                disbursement_method = NULL,
                destination_account = NULL,
                disbursement_instructed_at = NULL,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new { LoanId = e.AggregateId, Now = DateTime.UtcNow }, ct);
        _logger.LogDebug("PendingDisbursements: loan {LoanId} disbursement failed — back to Approved", e.AggregateId);
    }

    private async Task HandleRemove(Guid loanId, CancellationToken ct)
    {
        const string sql = "DELETE FROM rm_pending_disbursements WHERE loan_id = @LoanId";
        await _store.ExecuteAsync(sql, new { LoanId = loanId }, ct);
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        await _store.ExecuteAsync("TRUNCATE TABLE rm_pending_disbursements", ct: ct);

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
        }
    }
}
