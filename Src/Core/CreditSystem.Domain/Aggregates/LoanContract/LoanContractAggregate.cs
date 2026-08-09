using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;

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
        Money? originationFee = null)
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
            Principal = principal,
            InterestRate = rate,
            TermMonths = termMonths,
            AmortizationMethod = amortizationMethod,
            Schedule = schedule,
            OriginationFee = fee,
            EvaluationMetadata = evaluationMetadata
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

    public void ApplyPayment(Guid paymentId, Money amount, PaymentMethod method)
    {
        if (State.Status != ContractStatus.Active && State.Status != ContractStatus.Delinquent)
            throw new DomainException("Contract not in payable status");
        
        if (amount.Currency != State.Principal.Currency)
            throw new DomainException($"Currency mismatch: expected {State.Principal.Currency}, got {amount.Currency}");

        // Apply in order: fees -> penalty interest -> regular interest -> principal
        var remainingAmount = amount;
        var feePaid = Money.Zero();
        var penaltyInterestPaid = Money.Zero();
        var interestPaid = Money.Zero();
        var principalPaid = Money.Zero();

        if (State.TotalFees.Amount > 0)
        {
            feePaid = remainingAmount > State.TotalFees ? State.TotalFees : remainingAmount;
            remainingAmount = remainingAmount - feePaid;
        }

        if (remainingAmount.Amount > 0 && State.AccruedPenaltyInterest.Amount > 0)
        {
            penaltyInterestPaid = remainingAmount > State.AccruedPenaltyInterest ? State.AccruedPenaltyInterest : remainingAmount;
            remainingAmount = remainingAmount - penaltyInterestPaid;
        }

        if (remainingAmount.Amount > 0 && State.AccruedInterest.Amount > 0)
        {
            interestPaid = remainingAmount > State.AccruedInterest ? State.AccruedInterest : remainingAmount;
            remainingAmount = remainingAmount - interestPaid;
        }

        if (remainingAmount.Amount > 0)
        {
            principalPaid = remainingAmount > State.CurrentBalance ? State.CurrentBalance : remainingAmount;
        }

        var newBalance = State.CurrentBalance - principalPaid;

        Apply(new PaymentApplied
        {
            AggregateId = Id,
            PaymentId = paymentId,
            TotalAmount = amount,
            PrincipalPaid = principalPaid,
            InterestPaid = interestPaid,
            PenaltyInterestPaid = penaltyInterestPaid,
            FeePaid = feePaid,
            NewBalance = newBalance,
            PaymentNumber = State.PaymentsMade + 1,
            Method = method
        }, isNew: true);

        if (newBalance.Amount == 0)
        {
            Apply(new ContractPaidOff
            {
                AggregateId = Id,
                FinalPayment = amount,
                TotalPrincipalPaid = State.Principal,
                TotalInterestPaid = State.AccruedInterest + interestPaid,
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
                Principal = e.Principal,
                CurrentBalance = e.Principal,
                InterestRate = e.InterestRate,
                TermMonths = e.TermMonths,
                AmortizationMethod = e.AmortizationMethod,
                Schedule = e.Schedule,
                TotalFees = e.OriginationFee,
                OriginationFee = e.OriginationFee,
                AccruedPenaltyInterest = Money.Zero(e.Principal.Currency),
                Status = ContractStatus.Approved,
                NextPaymentDue = e.Schedule.Entries.FirstOrDefault()?.DueDate,
                Version = state.Version + 1
            },

            ContractApproved => state,

            LoanDisbursed e => state with
            {
                Status = ContractStatus.Active,
                DisbursedAt = e.DisbursedAt,
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