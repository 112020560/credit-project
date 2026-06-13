using System;
using Credit.Domain.Enums;
using Credit.Domain.Services.Dtos;

namespace Credit.Domain.Services.AmortizationStrategy;

public class FrenchAmortizationStrategy : IAmortizationStrategy
{
    public AmortizationMethod Method => AmortizationMethod.French;

    public IEnumerable<InstallmentDto> GenerateSchedule(decimal principal, decimal annualRate, int months)
    {
        var list = new List<InstallmentDto>();

        var monthlyRate = annualRate / 12 / 100;
        var payment = principal * monthlyRate /
                      (1 - (decimal)Math.Pow(1 + (double)monthlyRate, -months));

        decimal balance = principal;

        for (int i = 1; i <= months; i++)
        {
            var interest = balance * monthlyRate;
            var amort = payment - interest;
            balance -= amort;

            list.Add(new InstallmentDto(
                Number: i,
                DueDate: DateTime.UtcNow.AddMonths(i),
                Principal: Math.Round(amort, 2),
                Interest: Math.Round(interest, 2),
                TotalPayment: Math.Round(payment, 2),
                RemainingBalance: Math.Round(balance, 2)
            ));
        }

        return list;
    }
}
