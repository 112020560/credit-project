using System;
using System.Collections.Generic;
using System.Linq;
using Credit.Domain.Enums;
using Credit.Domain.Events;
using SmartCore.Credit.Domain;

namespace Credit.Domain;

public sealed class CreditAgreement
{
    public Guid Id { get; private set; }
    public CreditTerms Terms { get; private set; } = default!;
    public CreditState State { get; private set; }
    public AmortizationSchedule? Schedule { get; private set; }
    public decimal FundedAmount { get; private set; }
    public List<DomainEvent> UncommittedEvents { get; } = new();

    // Factory: create new credit (not yet approved)
    public static CreditAgreement Create(Guid id, CreditTerms terms, AmortizationSchedule schedule)
    {
        var agg = new CreditAgreement();
        agg.Apply(new CreditCreated(id, terms));
        agg.Terms = terms;
        agg.Schedule = schedule;
        agg.Id = id;
        agg.State = CreditState.Created;
        return agg;
    }

    // For Event Sourcing rehydration: apply event without adding to uncommitted
    public void ApplyEventFromHistory(DomainEvent evt) => When(evt);

    private void Apply(DomainEvent evt)
    {
        When(evt);
        UncommittedEvents.Add(evt);
    }

    private void When(DomainEvent evt)
    {
        switch (evt)
        {
            case CreditCreated e:
                Id = e.CreditId;
                Terms = e.Terms;
                State = CreditState.Created;
                break;
            case CreditApproved:
                State = CreditState.Approved;
                break;
            case CreditFunded e:
                FundedAmount = e.FundedAmount;
                State = CreditState.Funded;
                break;
            case PaymentRegistered e:
                // handled in RegisterPayment flow
                break;
            case CreditMarkedLate:
                State = CreditState.Late;
                break;
            case CreditClosed:
                State = CreditState.Closed;
                break;
            default:
                break;
        }
    }

    // Domain operations:

    public void Approve()
    {
        if (State != CreditState.Created)
            throw new DomainException("Only created credits can be approved.");

        Apply(new CreditApproved(Id));
    }

    public void Fund(decimal amount, DateTime fundedOn)
    {
        if (State != CreditState.Approved)
            throw new DomainException("Only approved credits can be funded.");

        if (amount <= 0) throw new DomainException("Fund amount must be > 0.");
        if (amount != Terms.Principal) throw new DomainException("Funded amount must match principal.");

        Apply(new CreditFunded(Id, amount, fundedOn));
        // After funding we set it Active
        State = CreditState.Active;
    }

    public void RegisterPayment(Payment payment)
    {
        if (State != CreditState.Active && State != CreditState.Late)
            throw new DomainException("Payments can only be registered when credit is Active or Late.");

        if (Schedule == null) throw new DomainException("Schedule is not set.");

        var remaining = payment.Amount;
        var next = Schedule.Installments.FirstOrDefault(i => !i.IsPaid);
        if (next == null)
            throw new DomainException("All installments already paid.");

        // consume payment across installments sequentially
        foreach (var inst in Schedule.Installments.Where(i => !i.IsPaid))
        {
            if (remaining <= 0) break;
            var (principalApplied, interestApplied) = inst.ApplyPayment(remaining);
            remaining -= (principalApplied + interestApplied);

            // Emit event for each application
            Apply(new PaymentAppliedToInstallment(Id, payment.Id, inst.Number, principalApplied, interestApplied));
        }

        // register payment overall
        Apply(new PaymentRegistered(Id, payment.Id, payment.Amount, payment.PaidOn));

        // If fully paid then close
        if (Schedule.Installments.All(i => i.IsPaid))
            Apply(new CreditClosed(Id, DateTime.UtcNow));
    }

    public void MarkAsLate(DateTime when)
    {
        if (State != CreditState.Active)
            throw new DomainException("Only active credits can be marked late.");

        Apply(new CreditMarkedLate(Id, when));
    }

    public void Close(DateTime when)
    {
        if (State == CreditState.Closed)
            throw new DomainException("Already closed.");

        Apply(new CreditClosed(Id, when));
    }

    // Clear events after persistence
    public void ClearUncommittedEvents() => UncommittedEvents.Clear();
}
