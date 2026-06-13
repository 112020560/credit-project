using System;
using Credit.Domain.Aggregates;
using Credit.Domain.Events;
using Credit.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Credit.Domain.Test;

public class CreditContractTests
{
    [Fact]
    public void Payment_should_pay_interest_then_principal()
    {
        var id = new CreditId("CR-1");

        var history = new IDomainEvent[]
        {
        new LoanDisbursed(
            id,
            new Money(1000, "CRC"),
            DateTime.UtcNow.AddDays(-30)
        ),
        new InterestAccrued(
            id,
            new DateRange(
                new DateOnly(2025, 2, 1),
                new DateOnly(2025, 2, 28)
            ),
            new Money(100, "CRC"),
            DateTime.UtcNow.AddDays(-1)
        )
        };

        var credit = new CreditContract(history);

        credit.ApplyPayment(new Money(200, "CRC"), DateTime.UtcNow);

        var payment = credit.UncommittedEvents
            .OfType<PaymentApplied>()
            .Single();

        payment.Breakdown.Interest.Amount.Should().Be(100);
        payment.Breakdown.Principal.Amount.Should().Be(100);
    }


}
