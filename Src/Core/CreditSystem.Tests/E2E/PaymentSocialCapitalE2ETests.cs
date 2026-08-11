using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using CreditSystem.Infrastructure.Projectors;
using Dapper;
using FluentAssertions;
using Npgsql;

namespace CreditSystem.Tests.E2E;

/// <summary>
/// E2E tests: full payment flow hitting the real database.
/// Verifies that social capital is stored in rm_payment_history
/// and that the cooperative member's balance is updated correctly.
/// </summary>
public class PaymentSocialCapitalE2ETests : IAsyncLifetime
{
    private readonly E2EFixture _fx = new();
    private readonly List<Guid> _loanIds = new();
    private readonly List<Guid> _memberIds = new();
    private readonly List<Guid> _customerProfileIds = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _fx.CleanupAsync(
        loanIds: _loanIds,
        memberIds: _memberIds,
        customerProfileIds: _customerProfileIds);

    // ── helpers ────────────────────────────────────────────────────────────

    private static LoanContractAggregate CreateAndDisburseContract(Guid customerId)
    {
        var calculator = new FrenchAmortizationCalculator();
        var rate = new InterestRate(14m);
        var aggregate = LoanContractAggregate.Create(
            customerId: customerId,
            principal: new Money(500_000m, "CRC"),
            rate: rate,
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: calculator,
            evaluationMetadata: new Dictionary<string, object>());

        aggregate.ClearUncommittedEvents();
        aggregate.Disburse("WIRE", "CR21015201001026284066");
        return aggregate;
    }

    /// <summary>
    /// Inserts a minimal cooperative member and a matching customer_credit_profile
    /// so that the SocialCapitalProjector's JOIN can resolve correctly.
    /// Returns (memberId, customerProfileId, sharedExternalId).
    /// </summary>
    private async Task<(Guid memberId, Guid profileId, Guid externalId)> InsertTestMemberAndProfileAsync()
    {
        var externalId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);

        await conn.ExecuteAsync("""
            INSERT INTO cooperative_members
                (id, external_id, member_number, status, joined_at,
                 total_shares_amount, shares_currency, number_of_contributions,
                 social_capital_balance, created_at, updated_at)
            VALUES
                (@Id, @ExternalId, @MemberNumber, 'Active', NOW(),
                 0, 'CRC', 0, 0, NOW(), NOW())
            """,
            new { Id = memberId, ExternalId = externalId, MemberNumber = $"E2E-{memberId:N}"[..20] });

        await conn.ExecuteAsync("""
            INSERT INTO customer_credit_profiles
                (id, external_id, full_name, document_type, document_number,
                 credit_score, monthly_income, created_at, updated_at)
            VALUES
                (@Id, @ExternalId, 'E2E Test Member', 'CEDULA', @DocNumber,
                 750, 1500000, NOW(), NOW())
            """,
            new
            {
                Id = profileId,
                ExternalId = externalId,
                DocNumber = $"E2E{profileId:N}"[..12]
            });

        _memberIds.Add(memberId);
        _customerProfileIds.Add(profileId);
        return (memberId, profileId, externalId);
    }

    /// <summary>Inserts the loan summary row needed by SocialCapitalProjector.</summary>
    private async Task InsertLoanSummaryAsync(Guid loanId, Guid customerProfileId, Money principal)
    {
        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO rm_loan_summaries
                (loan_id, customer_id, customer_name, principal, current_balance,
                 accrued_interest, total_fees, interest_rate, term_months,
                 status, payments_made, payments_missed, version, created_at, updated_at)
            VALUES
                (@LoanId, @CustomerId, 'E2E Test', @Principal, @Principal,
                 0, 0, 14, 12,
                 'Active', 0, 0, 1, NOW(), NOW())
            ON CONFLICT (loan_id) DO NOTHING
            """,
            new { LoanId = loanId, CustomerId = customerProfileId, Principal = principal.Amount });
    }

    // ── tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyPayment_WithSocialCapital_StoresContributionInPaymentHistory()
    {
        // Arrange
        var (_, profileId, _) = await InsertTestMemberAndProfileAsync();
        var repo = _fx.BuildLoanRepository();
        var projectionStore = _fx.BuildProjectionStore();
        var projector = new PaymentHistoryProjector(projectionStore);

        var aggregate = CreateAndDisburseContract(customerId: profileId);
        _loanIds.Add(aggregate.Id);

        // Save disburse event
        await repo.SaveAsync(aggregate);
        aggregate.ClearUncommittedEvents();

        // Apply payment with fixed social capital of CRC 750
        var socialCapital = new Money(750m, "CRC");
        aggregate.ApplyPayment(
            Guid.NewGuid(),
            new Money(30_000m, "CRC"),
            PaymentMethod.Wire,
            socialCapitalContributed: socialCapital,
            collectionMode: SocialCapitalCollectionMode.SeparateCollection);

        var paymentEvent = aggregate.UncommittedEvents.OfType<PaymentApplied>().First();

        // Act: save + project
        await repo.SaveAsync(aggregate);
        await projector.ProjectAsync(paymentEvent);

        // Assert: rm_payment_history has the social capital amount
        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);
        var row = await conn.QuerySingleOrDefaultAsync<(decimal socialCapital, decimal totalAmount)>(
            "SELECT social_capital_contributed, total_amount FROM rm_payment_history WHERE id = @Id",
            new { Id = paymentEvent.PaymentId });

        row.socialCapital.Should().Be(750m);
        row.totalAmount.Should().Be(30_000m);
    }

    [Fact]
    public async Task ApplyPayment_WithIncludedInPayment_SocialCapital_DeductsFromLoanAmount()
    {
        // Arrange
        var (_, profileId, _) = await InsertTestMemberAndProfileAsync();
        var repo = _fx.BuildLoanRepository();
        var projectionStore = _fx.BuildProjectionStore();
        var projector = new PaymentHistoryProjector(projectionStore);

        var aggregate = CreateAndDisburseContract(customerId: profileId);
        _loanIds.Add(aggregate.Id);
        var balanceBefore = aggregate.State.CurrentBalance.Amount;

        await repo.SaveAsync(aggregate);
        aggregate.ClearUncommittedEvents();

        // IncludedInPayment: CRC 750 of the CRC 30 000 goes to social capital → loan gets CRC 29 250
        var socialCapital = new Money(750m, "CRC");
        aggregate.ApplyPayment(
            Guid.NewGuid(),
            new Money(30_000m, "CRC"),
            PaymentMethod.Wire,
            socialCapitalContributed: socialCapital,
            collectionMode: SocialCapitalCollectionMode.IncludedInPayment);

        var paymentEvent = aggregate.UncommittedEvents.OfType<PaymentApplied>().First();

        // Act
        await repo.SaveAsync(aggregate);
        await projector.ProjectAsync(paymentEvent);

        // Assert: principal reduction should be based on 29 250, not 30 000
        var principalPaid = paymentEvent.PrincipalPaid.Amount;
        var totalDistributed = paymentEvent.PrincipalPaid.Amount
            + paymentEvent.InterestPaid.Amount
            + paymentEvent.FeePaid.Amount
            + paymentEvent.PenaltyInterestPaid.Amount;

        (totalDistributed <= 29_250m).Should().BeTrue(
            "loan receives payment minus social capital when IncludedInPayment");
        paymentEvent.SocialCapitalContributed.Amount.Should().Be(750m);

        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);
        var storedSC = await conn.QuerySingleAsync<decimal>(
            "SELECT social_capital_contributed FROM rm_payment_history WHERE id = @Id",
            new { Id = paymentEvent.PaymentId });
        storedSC.Should().Be(750m);
    }

    [Fact]
    public async Task ApplyPayment_UpdatesCooperativeMemberSocialCapitalBalance()
    {
        // Arrange
        var (memberId, profileId, _) = await InsertTestMemberAndProfileAsync();
        var repo = _fx.BuildLoanRepository();
        var projectionStore = _fx.BuildProjectionStore();
        var projector = new SocialCapitalProjector(projectionStore);

        var aggregate = CreateAndDisburseContract(customerId: profileId);
        _loanIds.Add(aggregate.Id);

        await repo.SaveAsync(aggregate);

        // Pre-insert rm_loan_summaries so the projector can resolve member via the JOIN
        await InsertLoanSummaryAsync(aggregate.Id, profileId, aggregate.State.Principal);

        aggregate.ClearUncommittedEvents();

        // Apply two payments of CRC 500 social capital each
        var socialCapital = new Money(500m, "CRC");
        aggregate.ApplyPayment(Guid.NewGuid(), new Money(25_000m, "CRC"), PaymentMethod.Cash,
            socialCapitalContributed: socialCapital);
        aggregate.ApplyPayment(Guid.NewGuid(), new Money(25_000m, "CRC"), PaymentMethod.Cash,
            socialCapitalContributed: socialCapital);

        // Act: project both events
        await repo.SaveAsync(aggregate);
        foreach (var evt in aggregate.UncommittedEvents.OfType<PaymentApplied>())
            await projector.ProjectAsync(evt);

        // Assert: member balance should be 1 000 (2 × 500)
        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);
        var balance = await conn.QuerySingleAsync<decimal>(
            "SELECT social_capital_balance FROM cooperative_members WHERE id = @Id",
            new { Id = memberId });

        balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task ApplyPayment_WithZeroSocialCapital_DoesNotChangeBalance()
    {
        // Arrange
        var (memberId, profileId, _) = await InsertTestMemberAndProfileAsync();
        var repo = _fx.BuildLoanRepository();
        var projectionStore = _fx.BuildProjectionStore();
        var projector = new SocialCapitalProjector(projectionStore);

        var aggregate = CreateAndDisburseContract(customerId: profileId);
        _loanIds.Add(aggregate.Id);

        await repo.SaveAsync(aggregate);
        await InsertLoanSummaryAsync(aggregate.Id, profileId, aggregate.State.Principal);
        aggregate.ClearUncommittedEvents();

        // Payment with NO social capital
        aggregate.ApplyPayment(Guid.NewGuid(), new Money(25_000m, "CRC"), PaymentMethod.Wire);

        await repo.SaveAsync(aggregate);
        foreach (var evt in aggregate.UncommittedEvents.OfType<PaymentApplied>())
            await projector.ProjectAsync(evt);

        // Assert: balance stays at 0
        await using var conn = new NpgsqlConnection(E2EFixture.ConnectionString);
        var balance = await conn.QuerySingleAsync<decimal>(
            "SELECT social_capital_balance FROM cooperative_members WHERE id = @Id",
            new { Id = memberId });

        balance.Should().Be(0m);
    }

    [Fact]
    public async Task Aggregate_RehydratesFromEventStore_WithSocialCapitalAccumulated()
    {
        // Arrange
        var (_, profileId, _) = await InsertTestMemberAndProfileAsync();
        var repo = _fx.BuildLoanRepository();

        var aggregate = CreateAndDisburseContract(customerId: profileId);
        _loanIds.Add(aggregate.Id);
        await repo.SaveAsync(aggregate);
        aggregate.ClearUncommittedEvents();

        // Apply two payments
        var sc = new Money(300m, "CRC");
        aggregate.ApplyPayment(Guid.NewGuid(), new Money(20_000m, "CRC"), PaymentMethod.Wire, socialCapitalContributed: sc);
        aggregate.ApplyPayment(Guid.NewGuid(), new Money(20_000m, "CRC"), PaymentMethod.Wire, socialCapitalContributed: sc);
        await repo.SaveAsync(aggregate);

        // Act: load fresh from event store
        var rehydrated = await repo.GetByIdAsync(aggregate.Id);

        // Assert
        rehydrated.Should().NotBeNull();
        rehydrated!.State.TotalSocialCapitalContributed.Amount.Should().Be(600m,
            "two payments of CRC 300 each should accumulate to CRC 600");
        rehydrated.State.PaymentsMade.Should().Be(2);
    }
}
