using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit;

public class RevolvingCreditAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public RevolvingCreditState State { get; private set; } = null!;
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    public RevolvingCreditAggregate()
    {
        State = RevolvingCreditState.Initial;
    }

    public RevolvingCreditAggregate(RevolvingCreditState? snapshot, IEnumerable<IDomainEvent> events)
    {
        State = snapshot ?? RevolvingCreditState.Initial;

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    #region Factory

    public static RevolvingCreditAggregate Create(
        Guid customerId,
        Money creditLimit,
        InterestRate rate,
        decimal minimumPaymentPercentage,
        Money minimumPaymentAmount,
        int billingCycleDay,
        int gracePeriodDays)
    {
        if (billingCycleDay < 1 || billingCycleDay > 28)
            throw new DomainException("Billing cycle day must be between 1 and 28");

        var aggregate = new RevolvingCreditAggregate();
        var id = Guid.NewGuid();

        aggregate.Id = id;

        aggregate.Apply(new CreditLineCreated
        {
            AggregateId = id,
            CustomerId = customerId,
            CreditLimit = creditLimit,
            InterestRate = rate,
            MinimumPaymentPercentage = minimumPaymentPercentage,
            MinimumPaymentAmount = minimumPaymentAmount,
            BillingCycleDay = billingCycleDay,
            GracePeriodDays = gracePeriodDays
        }, isNew: true);

        return aggregate;
    }

    #endregion

    #region Commands

    public void Activate()
    {
        if (State.Status != RevolvingCreditStatus.Pending)
            throw new DomainException($"Cannot activate: status is {State.Status}");

        Apply(new CreditLineActivated
        {
            AggregateId = Id,
            ActivatedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void DrawFunds(Money amount, string description)
    {
        if (State.Status != RevolvingCreditStatus.Active)
            throw new DomainException($"Cannot draw funds: credit line is {State.Status}");

        if (amount.Amount <= 0)
            throw new DomainException("Draw amount must be greater than zero");

        if (amount.Amount > State.AvailableCredit.Amount)
            throw new DomainException(
                $"Insufficient credit. Available: {State.AvailableCredit.Amount}, Requested: {amount.Amount}");

        var newBalance = State.CurrentBalance + amount;
        var availableCredit = State.CreditLimit - newBalance;

        Apply(new FundsDrawn
        {
            AggregateId = Id,
            DrawId = Guid.NewGuid(),
            Amount = amount,
            Description = description,
            NewBalance = newBalance,
            AvailableCredit = availableCredit,
            DrawnAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void ApplyPayment(Guid paymentId, Money amount, PaymentMethod method)
    {
        if (State.Status == RevolvingCreditStatus.Closed)
            throw new DomainException("Cannot apply payment: credit line is closed");

        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero");

        var remaining = amount;
        var feesPaid = Money.Zero(amount.Currency);
        var interestPaid = Money.Zero(amount.Currency);
        var principalPaid = Money.Zero(amount.Currency);

        // 1. Pagar fees
        if (State.PendingFees.Amount > 0 && remaining.Amount > 0)
        {
            feesPaid = remaining.Amount >= State.PendingFees.Amount
                ? State.PendingFees
                : remaining;
            remaining = new Money(remaining.Amount - feesPaid.Amount, remaining.Currency);
        }

        // 2. Pagar interés
        if (State.AccruedInterest.Amount > 0 && remaining.Amount > 0)
        {
            interestPaid = remaining.Amount >= State.AccruedInterest.Amount
                ? State.AccruedInterest
                : remaining;
            remaining = new Money(remaining.Amount - interestPaid.Amount, remaining.Currency);
        }

        // 3. Pagar principal
        if (State.CurrentBalance.Amount > 0 && remaining.Amount > 0)
        {
            principalPaid = remaining.Amount >= State.CurrentBalance.Amount
                ? State.CurrentBalance
                : remaining;
        }

        var newBalance = new Money(State.CurrentBalance.Amount - principalPaid.Amount, State.CurrentBalance.Currency);
        var availableCredit = new Money(State.CreditLimit.Amount - newBalance.Amount, State.CreditLimit.Currency);

        Apply(new RevolvingPaymentApplied
        {
            AggregateId = Id,
            PaymentId = paymentId,
            TotalAmount = amount,
            InterestPaid = interestPaid,
            FeesPaid = feesPaid,
            PrincipalPaid = principalPaid,
            NewBalance = newBalance,
            AvailableCredit = availableCredit,
            Method = method
        }, isNew: true);

        // Descongelar si estaba congelado y pagó el mínimo
        if (State.Status == RevolvingCreditStatus.Frozen && 
            State.CurrentMinimumPayment != null &&
            amount.Amount >= State.CurrentMinimumPayment.Amount)
        {
            Apply(new CreditLineUnfrozen
            {
                AggregateId = Id,
                Reason = "Minimum payment received",
                UnfrozenAt = DateTime.UtcNow
            }, isNew: true);
        }
    }

    public void AccrueInterest(DateTime periodStart, DateTime periodEnd)
    {
        if (State.CurrentBalance.Amount <= 0)
            return;

        if (periodStart >= periodEnd)
            return;

        var days = (periodEnd - periodStart).Days;
        if (days <= 0) return;

        var dailyInterest = State.InterestRate.CalculateDailyInterest(State.CurrentBalance);
        var totalInterest = new Money(dailyInterest.Amount * days, State.CurrentBalance.Currency);

        Apply(new RevolvingInterestAccrued
        {
            AggregateId = Id,
            Amount = totalInterest,
            AverageBalance = State.CurrentBalance,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        }, isNew: true);
    }

    public void GenerateStatement()
    {
        if (State.Status != RevolvingCreditStatus.Active && State.Status != RevolvingCreditStatus.Frozen)
            throw new DomainException($"Cannot generate statement: status is {State.Status}");

        var statementDate = DateTime.UtcNow.Date;
        var dueDate = statementDate.AddDays(State.GracePeriodDays);

        // Calcular pago mínimo
        var percentagePayment = new Money(
            State.CurrentBalance.Amount * State.MinimumPaymentPercentage / 100,
            State.CurrentBalance.Currency);

        var minimumPayment = percentagePayment.Amount > State.MinimumPaymentAmount.Amount
            ? percentagePayment
            : State.MinimumPaymentAmount;

        // Si el balance es menor al mínimo, el mínimo es el balance total
        if (State.TotalOwed.Amount < minimumPayment.Amount)
        {
            minimumPayment = State.TotalOwed;
        }

        // Si no hay deuda, el mínimo es 0
        if (State.TotalOwed.Amount <= 0)
        {
            minimumPayment = Money.Zero(State.CreditLimit.Currency);
        }

        Apply(new StatementGenerated
        {
            AggregateId = Id,
            StatementId = Guid.NewGuid(),
            StatementDate = statementDate,
            DueDate = dueDate,
            PreviousBalance = State.CurrentBalance,
            Purchases = Money.Zero(State.CreditLimit.Currency),
            Payments = Money.Zero(State.CreditLimit.Currency),
            InterestCharged = State.AccruedInterest,
            FeesCharged = State.PendingFees,
            NewBalance = State.TotalOwed,
            MinimumPayment = minimumPayment
        }, isNew: true);
    }

    public void Freeze(string reason)
    {
        if (State.Status != RevolvingCreditStatus.Active)
            throw new DomainException($"Cannot freeze: status is {State.Status}");

        Apply(new CreditLineFrozen
        {
            AggregateId = Id,
            Reason = reason,
            FrozenAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void Unfreeze(string reason)
    {
        if (State.Status != RevolvingCreditStatus.Frozen)
            throw new DomainException($"Cannot unfreeze: status is {State.Status}");

        Apply(new CreditLineUnfrozen
        {
            AggregateId = Id,
            Reason = reason,
            UnfrozenAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void ChangeCreditLimit(Money newLimit, string reason)
    {
        if (State.Status == RevolvingCreditStatus.Closed)
            throw new DomainException("Cannot change limit: credit line is closed");

        if (newLimit.Amount < State.CurrentBalance.Amount)
            throw new DomainException(
                $"New limit ({newLimit.Amount}) cannot be less than current balance ({State.CurrentBalance.Amount})");

        Apply(new CreditLimitChanged
        {
            AggregateId = Id,
            PreviousLimit = State.CreditLimit,
            NewLimit = newLimit,
            Reason = reason
        }, isNew: true);
    }

    public void Close(string reason)
    {
        if (State.Status == RevolvingCreditStatus.Closed)
            return;

        if (State.CurrentBalance.Amount > 0)
            throw new DomainException("Cannot close: balance must be zero");

        Apply(new CreditLineClosed
        {
            AggregateId = Id,
            Reason = reason,
            FinalBalance = State.CurrentBalance,
            ClosedAt = DateTime.UtcNow
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
        }
    }

    private static RevolvingCreditState ApplyEvent(RevolvingCreditState state, IDomainEvent @event)
    {
        return @event switch
        {
            CreditLineCreated e => state with
            {
                Id = e.AggregateId,
                CustomerId = e.CustomerId,
                CreditLimit = e.CreditLimit,
                AvailableCredit = e.CreditLimit,
                CurrentBalance = Money.Zero(e.CreditLimit.Currency),
                AccruedInterest = Money.Zero(e.CreditLimit.Currency),
                PendingFees = Money.Zero(e.CreditLimit.Currency),
                InterestRate = e.InterestRate,
                MinimumPaymentPercentage = e.MinimumPaymentPercentage,
                MinimumPaymentAmount = e.MinimumPaymentAmount,
                BillingCycleDay = e.BillingCycleDay,
                GracePeriodDays = e.GracePeriodDays,
                Status = RevolvingCreditStatus.Pending,
                Version = state.Version + 1
            },

            CreditLineActivated e => state with
            {
                Status = RevolvingCreditStatus.Active,
                ActivatedAt = e.ActivatedAt,
                NextStatementDate = CalculateNextStatementDate(e.ActivatedAt, state.BillingCycleDay),
                Version = state.Version + 1
            },

            FundsDrawn e => state with
            {
                CurrentBalance = e.NewBalance,
                AvailableCredit = e.AvailableCredit,
                Version = state.Version + 1
            },

            RevolvingPaymentApplied e => state with
            {
                CurrentBalance = e.NewBalance,
                AvailableCredit = e.AvailableCredit,
                AccruedInterest = new Money(state.AccruedInterest.Amount - e.InterestPaid.Amount, state.AccruedInterest.Currency),
                PendingFees = new Money(state.PendingFees.Amount - e.FeesPaid.Amount, state.PendingFees.Currency),
                ConsecutiveMissedPayments = 0,
                Version = state.Version + 1
            },

            RevolvingInterestAccrued e => state with
            {
                AccruedInterest = new Money(state.AccruedInterest.Amount + e.Amount.Amount, state.AccruedInterest.Currency),
                LastInterestAccrualDate = e.PeriodEnd,
                Version = state.Version + 1
            },

            StatementGenerated e => state with
            {
                LastStatementDate = e.StatementDate,
                NextStatementDate = e.StatementDate.AddMonths(1),
                PaymentDueDate = e.DueDate,
                CurrentMinimumPayment = e.MinimumPayment,
                Version = state.Version + 1
            },

            CreditLineFrozen e => state with
            {
                Status = RevolvingCreditStatus.Frozen,
                FrozenAt = e.FrozenAt,
                Version = state.Version + 1
            },

            CreditLineUnfrozen e => state with
            {
                Status = RevolvingCreditStatus.Active,
                FrozenAt = null,
                ConsecutiveMissedPayments = 0,
                Version = state.Version + 1
            },

            CreditLimitChanged e => state with
            {
                CreditLimit = e.NewLimit,
                AvailableCredit = new Money(e.NewLimit.Amount - state.CurrentBalance.Amount, e.NewLimit.Currency),
                Version = state.Version + 1
            },

            CreditLineClosed e => state with
            {
                Status = RevolvingCreditStatus.Closed,
                AvailableCredit = Money.Zero(state.CreditLimit.Currency),
                ClosedAt = e.ClosedAt,
                Version = state.Version + 1
            },

            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };
    }

    private static DateTime CalculateNextStatementDate(DateTime from, int billingCycleDay)
    {
        var daysInMonth = DateTime.DaysInMonth(from.Year, from.Month);
        var day = Math.Min(billingCycleDay, daysInMonth);
        var next = new DateTime(from.Year, from.Month, day);
        
        if (next <= from)
        {
            next = next.AddMonths(1);
            daysInMonth = DateTime.DaysInMonth(next.Year, next.Month);
            day = Math.Min(billingCycleDay, daysInMonth);
            next = new DateTime(next.Year, next.Month, day);
        }
        
        return next;
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    #endregion
}