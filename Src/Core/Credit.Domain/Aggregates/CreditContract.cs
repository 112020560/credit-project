
using Credit.Domain.Abstractions;
using Credit.Domain.Events;
using Credit.Domain.ValueObjects;

namespace Credit.Domain.Aggregates;

#nullable disable
public sealed class CreditContract : AggregateRoot
{
    public CreditId Id { get; private set; }

    private string _currency;
    private decimal _principalOutstanding;
    private decimal _interestOutstanding;
    private bool _isCreated;

    private readonly List<IDomainEvent> _uncommittedEvents = [];

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    public CreditContract()
    {

    }
    public CreditContract(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
            Apply(e);
    }

    public void ApplyPayment(Money amount, DateTime when)
    {
        if (amount.Amount <= 0)
            throw new InvalidOperationException("Payment must be positive");

        if (amount.Currency != _currency)
            throw new InvalidOperationException("Currency mismatch");

        var remaining = amount.Amount;

        // Penalidad (no existe aún)
        var penaltyPaid = 0m;

        // Interés
        var interestPaid = Math.Min(remaining, _interestOutstanding);
        _interestOutstanding -= interestPaid;
        remaining -= interestPaid;

        // Principal
        var principalPaid = remaining;
        _principalOutstanding -= principalPaid;

        var breakdown = new PaymentBreakdown(
            new Money(penaltyPaid, _currency),
            new Money(interestPaid, _currency),
            new Money(principalPaid, _currency)
        );

        Raise(new PaymentApplied(
            Id,
            amount,
            breakdown,
            when
        ));
    }

    public static CreditContract Create(
        CreditId id,
        string currency,
        DateTime when)
    {
        var contract = new CreditContract();
        contract.Raise(new CreditContractCreated(id, currency, when));
        return contract;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CreditContractCreated f:
                Id = f.CreditId;
                _currency = f.Currency;
                _principalOutstanding = 0;
                _interestOutstanding = 0;
                _isCreated = true;
                break;

            case LoanDisbursed d:
                Id = d.CreditId;
                _currency = d.Amount.Currency;
                _principalOutstanding = d.Amount.Amount;
                _interestOutstanding = 0;
                break;

            case InterestAccrued i:
                _interestOutstanding += i.Amount.Amount;
                break;

            case PaymentApplied p:
                // ya aplicado en el comando
                break;
        }
    }

    // protected override void Apply(IDomainEvent @event)
    // {
    //     throw new NotImplementedException();
    // }
}


