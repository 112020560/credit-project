using System;
using Credit.Domain.Enums;
using Credit.Domain.Services.Dtos;

namespace Credit.Domain.Services.AmortizationStrategy;

public class AmericanAmortizationStrategy : IAmortizationStrategy
{
    public AmortizationMethod Method => AmortizationMethod.American;

    public IEnumerable<InstallmentDto> GenerateSchedule(
        decimal principal, decimal annualRate, int months)
    {
        var list = new List<InstallmentDto>();

        var monthlyRate = annualRate / 12 / 100;
        var interestPayment = principal * monthlyRate;

        for (int i = 1; i <= months; i++)
        {
            decimal amort = (i == months) ? principal : 0;
            decimal payment = interestPayment + amort;
            decimal remaining = (i == months) ? 0 : principal;

            list.Add(new InstallmentDto(
                Number: i,
                DueDate: DateTime.UtcNow.AddMonths(i),
                Principal: Math.Round(amort, 2),
                Interest: Math.Round(interestPayment, 2),
                TotalPayment: Math.Round(payment, 2),
                RemainingBalance: Math.Round(remaining, 2)
            ));
        }

        return list;
    }
}
