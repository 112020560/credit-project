using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using PaymentWaterfall = CreditSystem.Domain.ValueObjects.PaymentWaterfall;

namespace CreditSystem.Domain.Aggregates.LoanContract;

public class LoanContractAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public Guid Id { get; private set; }
    public LoanContractState State { get; private set; }
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    // Private constructor for factory method
    private LoanContractAggregate()
    {
        State = LoanContractState.Initial;
    }

    #region Factory Methods

    public static LoanContractAggregate Create(
        Guid customerId,
        Money principal,
        InterestRate rate,
        int termMonths,
        AmortizationMethod amortizationMethod,
        IAmortizationCalculator calculator,
        Dictionary<string, object> evaluationMetadata,
        Money? originationFee = null,
        Guid? productId = null)
    {
        var aggregate = new LoanContractAggregate();
        var id = Guid.NewGuid();

        aggregate.Id = id;

        var schedule = calculator.Calculate(principal, rate, termMonths, DateTime.UtcNow);
        var fee = originationFee ?? Money.Zero(principal.Currency);

        aggregate.Apply(new ContractCreated
        {
            AggregateId = id,
            CustomerId = customerId,
            ProductId = productId,
            Principal = principal,
            InterestRate = rate,
            TermMonths = termMonths,
            AmortizationMethod = amortizationMethod,
            Schedule = schedule,
            OriginationFee = fee,
            EvaluationMetadata = evaluationMetadata,
            RateType = rate.RateType,
            Spread = rate.Spread,
            ReferenceRateId = rate.ReferenceRateId
        }, isNew: true);

        aggregate.Apply(new ContractApproved
        {
            AggregateId = id,
            CustomerId = customerId,
            ApprovedRate = rate,
            ApprovedPrincipal = principal,
            EvaluationMetadata = evaluationMetadata
        }, isNew: true);

        return aggregate;
    }

    #endregion

    #region Commands

    public void Disburse(string method, string destinationAccount)
    {
        EnsureStatus(ContractStatus.Approved, "Cannot disburse");

        Apply(new LoanDisbursed
        {
            AggregateId = Id,
            Amount = State.Principal,
            DisbursementMethod = method,
            DestinationAccount = destinationAccount,
            DisbursedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void ConfirmDisbursement(string confirmedBy)
    {
        EnsureStatus(ContractStatus.Disbursing, "Cannot confirm disbursement");

        Apply(new DisbursementConfirmed
        {
            AggregateId = Id,
            ConfirmedBy = confirmedBy,
            DisbursedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void FailDisbursement(string reason)
    {
        EnsureStatus(ContractStatus.Disbursing, "Cannot fail disbursement");

        Apply(new DisbursementFailed
        {
            AggregateId = Id,
            Reason = reason,
            FailedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void AccrueInterest(DateTime periodStart, DateTime periodEnd)
    {
        EnsureStatus(ContractStatus.Active, "Cannot accrue interest");

        var interest = State.InterestRate.CalculateDailyInterest(State.CurrentBalance);
        var days = (periodEnd - periodStart).Days;
        var totalInterest = new Money(interest.Amount * days);

        Apply(new InterestAccrued
        {
            AggregateId = Id,
            Amount = totalInterest,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PrincipalBalance = State.CurrentBalance,
            RateApplied = State.InterestRate
        }, isNew: true);
    }

    public void ApplyPayment(
        Guid paymentId,
        Money amount,
        PaymentMethod method,
        PaymentWaterfall? waterfall = null,
        Money? socialCapitalContributed = null,
        SocialCapitalCollectionMode collectionMode = SocialCapitalCollectionMode.SeparateCollection)
    {
        if (State.Status != ContractStatus.Active && State.Status != ContractStatus.Delinquent)
            throw new DomainException("Contract not in payable status");

        if (amount.Currency != State.Principal.Currency)
            throw new DomainException($"Currency mismatch: expected {State.Principal.Currency}, got {amount.Currency}");

        var effectiveWaterfall = waterfall ?? PaymentWaterfall.Default;
        var socialCapital = socialCapitalContributed ?? Money.Zero(amount.Currency);

        // If social capital is included in the payment, deduct it first so the loan gets the remainder.
        var amountForLoan = collectionMode == SocialCapitalCollectionMode.IncludedInPayment && socialCapital.Amount > 0
            ? new Money(Math.Max(0m, amount.Amount - socialCapital.Amount), amount.Currency)
            : amount;

        var context = new LoanPaymentContext(
            TotalFees:      State.TotalFees,
            PenaltyInterest: State.AccruedPenaltyInterest,
            AccruedInterest: State.AccruedInterest,
            Principal:      State.CurrentBalance,
            Currency:       amount.Currency);

        var dist = effectiveWaterfall.Apply(amountForLoan, context);

        var newBalance = State.CurrentBalance - dist.PrincipalPaid;

        Apply(new PaymentApplied
        {
            AggregateId = Id,
            PaymentId = paymentId,
            TotalAmount = amount,
            PrincipalPaid = dist.PrincipalPaid,
            InterestPaid = dist.InterestPaid,
            PenaltyInterestPaid = dist.PenaltyInterestPaid,
            FeePaid = dist.FeesPaid,
            NewBalance = newBalance,
            PaymentNumber = State.PaymentsMade + 1,
            Method = method,
            SocialCapitalContributed = socialCapital
        }, isNew: true);

        if (newBalance.Amount == 0)
        {
            Apply(new ContractPaidOff
            {
                AggregateId = Id,
                FinalPayment = amount,
                TotalPrincipalPaid = State.Principal,
                TotalInterestPaid = State.AccruedInterest + dist.InterestPaid,
                TotalFeesPaid = State.TotalFees,
                PaidOffAt = DateTime.UtcNow,
                EarlyPayoff = State.PaymentsMade < State.TermMonths
            }, isNew: true);
        }
    }

    public void RecordMissedPayment(int paymentNumber, DateTime dueDate, Money lateFee, int autoDefaultThresholdDays = 90, Money? penaltyInterest = null)
    {
        EnsureStatus(ContractStatus.Active, ContractStatus.Delinquent);

        if (State.Status != ContractStatus.Active && State.Status != ContractStatus.Delinquent)
            throw new DomainException($"Cannot record missed payment: loan status is {State.Status}");

        if (paymentNumber <= State.PaymentsMissed + State.PaymentsMade)
            throw new DomainException($"Payment {paymentNumber} already processed");

        var scheduledPayment = State.Schedule.Entries
            .FirstOrDefault(e => e.PaymentNumber == paymentNumber)
            ?? throw new DomainException($"Payment {paymentNumber} not found in schedule");

        var amountDue = scheduledPayment?.TotalPayment ?? Money.Zero(State.Principal.Currency);
        var daysOverdue = (int)(DateTime.UtcNow.Date - dueDate.Date).TotalDays;
        var penalty = penaltyInterest ?? Money.Zero(State.Principal.Currency);

        Apply(new PaymentMissed
        {
            AggregateId = Id,
            PaymentNumber = paymentNumber,
            DueDate = dueDate,
            AmountDue = amountDue,
            DaysOverdue = daysOverdue,
            LateFeeApplied = lateFee,
            PenaltyInterestAccrued = penalty
        }, isNew: true);

        if (daysOverdue >= autoDefaultThresholdDays)
        {
            MarkAsDefault($"Payment {daysOverdue} days overdue");
        }
    }

    public void MarkAsDefault(string reason)
    {
        if (State.Status == ContractStatus.Default)
            return;

        Apply(new ContractDefaulted
        {
            AggregateId = Id,
            Reason = reason,
            DaysDelinquent = CalculateDaysDelinquent(), // Aproximado
            OutstandingBalance = State.CurrentBalance,
            AccruedInterest = State.AccruedInterest,
            TotalOwed = State.TotalOwed
        }, isNew: true);
    }
    
    private int CalculateDaysDelinquent()
    {
        if (State.NextPaymentDue == null)
            return 0;
    
        var days = (int)(DateTime.UtcNow.Date - State.NextPaymentDue.Value.Date).TotalDays;
        return Math.Max(0, days);
    }

    public void Restructure(InterestRate newRate, int newTermMonths, Money forgiveAmount, string reason)
    {
        if (State.Status != ContractStatus.Delinquent && State.Status != ContractStatus.Default)
            throw new DomainException("Can only restructure delinquent or defaulted contracts");

        var newPrincipal = State.CurrentBalance - forgiveAmount;
        if (newPrincipal.Amount <= 0)
        {
            throw new DomainException("New balance after forgiveness must be greater than zero");
        }
        var newSchedule = PaymentSchedule.Calculate(newPrincipal, newRate, newTermMonths, DateTime.UtcNow);

        Apply(new ContractRestructured
        {
            AggregateId = Id,
            NewRate = newRate,
            NewTermMonths = newTermMonths,
            NewSchedule = newSchedule,
            ForgiveAmount = forgiveAmount,
            RestructureReason = reason
        }, isNew: true);
    }

    public void AdjustRate(decimal newReferenceRateValue, DateTime adjustedAt)
    {
        if (State.RateType == RateType.Fixed)
            throw new DomainException("Cannot adjust rate on a fixed-rate loan");

        EnsureStatus(ContractStatus.Active, "Cannot adjust rate");

        var newEffectiveRate = newReferenceRateValue + State.Spread;
        if (Math.Abs(newEffectiveRate - State.InterestRate.AnnualRate) < 0.0001m)
            return;

        var remainingEntries = State.Schedule.Entries
            .Where(e => e.DueDate >= adjustedAt.Date)
            .ToList();

        if (remainingEntries.Count <= 1)
            return;

        var newRate = new InterestRate(newEffectiveRate, RateType.Variable, State.Spread, State.ReferenceRateId);
        var newSchedule = PaymentSchedule.Calculate(State.CurrentBalance, newRate, remainingEntries.Count, adjustedAt);

        Apply(new RateAdjusted
        {
            AggregateId = Id,
            OldRate = State.InterestRate.AnnualRate,
            NewRate = newEffectiveRate,
            Spread = State.Spread,
            ReferenceRateId = State.ReferenceRateId!,
            ReferenceRateValue = newReferenceRateValue,
            AdjustedAt = adjustedAt,
            NewSchedule = newSchedule
        }, isNew: true);
    }

    #endregion

    #region Event Application

    private void Apply(IDomainEvent @event, bool isNew)
    {
        if (Id == Guid.Empty)
        {
            Id = @event.AggregateId;
        }
        
        State = ApplyEvent(State, @event);
        
        if (isNew)
        {
            if (@event is DomainEvent domainEvent)
            {
                _uncommittedEvents.Add(domainEvent with { Version = State.Version });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Event {@event.GetType().Name} must inherit from DomainEvent");
            }
        }
    }

    private static LoanContractState ApplyEvent(LoanContractState state, IDomainEvent @event)
    {
        return @event switch
        {
            ContractCreated e => state with
            {
                Id = e.AggregateId,
                CustomerId = e.CustomerId,
                ProductId = e.ProductId,
                Principal = e.Principal,
                CurrentBalance = e.Principal,
                InterestRate = e.InterestRate,
                RateType = e.RateType,
                Spread = e.Spread,
                ReferenceRateId = e.ReferenceRateId,
                TermMonths = e.TermMonths,
                AmortizationMethod = e.AmortizationMethod,
                Schedule = e.Schedule,
                TotalFees = e.OriginationFee,
                OriginationFee = e.OriginationFee,
                AccruedInterest = Money.Zero(e.Principal.Currency),
                AccruedPenaltyInterest = Money.Zero(e.Principal.Currency),
                TotalSocialCapitalContributed = Money.Zero(e.Principal.Currency),
                Status = ContractStatus.Approved,
                NextPaymentDue = e.Schedule.Entries.FirstOrDefault()?.DueDate,
                Version = state.Version + 1
            },

            ContractApproved => state with { Version = state.Version + 1 },

            LoanDisbursed e => state with
            {
                Status = ContractStatus.Disbursing,
                DisbursedAt = e.DisbursedAt,
                Version = state.Version + 1
            },

            DisbursementConfirmed e => state with
            {
                Status = ContractStatus.Active,
                Version = state.Version + 1
            },

            DisbursementFailed => state with
            {
                Status = ContractStatus.Approved,
                DisbursedAt = null,
                Version = state.Version + 1
            },

            InterestAccrued e => state with
            {
                AccruedInterest = state.AccruedInterest + e.Amount,
                LastInterestAccrualDate = e.PeriodEnd,
                Version = state.Version + 1
            },

            PaymentApplied e => state with
            {
                CurrentBalance = e.NewBalance,
                AccruedInterest = state.AccruedInterest - e.InterestPaid,
                AccruedPenaltyInterest = state.AccruedPenaltyInterest - e.PenaltyInterestPaid,
                TotalFees = state.TotalFees - e.FeePaid,
                TotalSocialCapitalContributed = state.TotalSocialCapitalContributed + e.SocialCapitalContributed,
                PaymentsMade = state.PaymentsMade + 1,
                LastPaymentDate = DateTime.UtcNow,
                NextPaymentDue = state.Schedule.Entries
                    .FirstOrDefault(x => x.PaymentNumber == e.PaymentNumber + 1)?.DueDate,
                Status = state.PaymentsMissed > 0 && e.NewBalance.Amount > 0
                    ? ContractStatus.Delinquent
                    : ContractStatus.Active,
                Version = state.Version + 1
            },

            PaymentMissed e => state with
            {
                PaymentsMissed = state.PaymentsMissed + 1,
                TotalFees = state.TotalFees + e.LateFeeApplied,
                AccruedPenaltyInterest = state.AccruedPenaltyInterest + e.PenaltyInterestAccrued,
                Status = ContractStatus.Delinquent,
                Version = state.Version + 1
            },

            ContractDefaulted e => state with
            {
                Status = ContractStatus.Default,
                DefaultedAt = DateTime.UtcNow,
                Version = state.Version + 1
            },

            ContractRestructured e => state with
            {
                InterestRate = e.NewRate,
                TermMonths = e.NewTermMonths,
                Schedule = e.NewSchedule,
                CurrentBalance = state.CurrentBalance - e.ForgiveAmount,
                Status = ContractStatus.Active,
                PaymentsMissed = 0,
                AccruedPenaltyInterest = Money.Zero(state.Principal.Currency),
                NextPaymentDue = e.NewSchedule.Entries.FirstOrDefault()?.DueDate,
                Version = state.Version + 1
            },

            ContractPaidOff e => state with
            {
                Status = ContractStatus.PaidOff,
                CurrentBalance = Money.Zero(),
                AccruedInterest = Money.Zero(),
                AccruedPenaltyInterest = Money.Zero(),
                PaidOffAt = e.PaidOffAt,
                Version = state.Version + 1
            },

            RateAdjusted e => state with
            {
                InterestRate = new InterestRate(e.NewRate, state.RateType, state.Spread, state.ReferenceRateId),
                Schedule = e.NewSchedule,
                NextPaymentDue = e.NewSchedule.Entries.FirstOrDefault()?.DueDate,
                Version = state.Version + 1
            },

            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };
    }

    #endregion

    #region Helpers

    private void EnsureStatus(ContractStatus expected, string message)
    {
        if (State.Status != expected)
            throw new DomainException($"{message}: expected {expected}, got {State.Status}");
    }

    private void EnsureStatus(params ContractStatus[] allowed)
    {
        if (!allowed.Contains(State.Status))
            throw new DomainException($"Invalid status: {State.Status}");
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    #endregion
    
    public LoanContractAggregate(LoanContractState? snapshot, IEnumerable<IDomainEvent> events)
    {
        State = snapshot ?? LoanContractState.Initial;
        Id = State.Id;
        
        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }
}