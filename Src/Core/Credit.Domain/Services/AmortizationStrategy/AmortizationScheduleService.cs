using System;
using Credit.Domain.Enums;
using Credit.Domain.Services.Dtos;

namespace Credit.Domain.Services.AmortizationStrategy;

public class AmortizationScheduleService
{
    private readonly Dictionary<AmortizationMethod, IAmortizationStrategy> _strategies;
    public AmortizationScheduleService(IEnumerable<IAmortizationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            s => s.Method,
            s => s
        );
    }

    public IEnumerable<InstallmentDto> GenerateSchedule(
        decimal principal,
        decimal annualRate,
        int months,
        AmortizationMethod method)
    {
        if (!_strategies.TryGetValue(method, out var strategy))
            throw new InvalidOperationException($"Amortization method '{method}' not supported.");

        return strategy.GenerateSchedule(principal, annualRate, months);
    }

//     private IEnumerable<InstallmentDto> GenerateFrenchSchedule(
//         decimal principal,
//         decimal annualRate,
//         int months)
//     {
//         var installments = new List<InstallmentDto>();

//         var monthlyRate = annualRate / 12 / 100;
//         var payment = principal * monthlyRate / (1 - (decimal)Math.Pow(1 + (double)monthlyRate, -months));

//         decimal balance = principal;

//         for (int i = 1; i <= months; i++)
//         {
//             var interest = balance * monthlyRate;
//             var amortization = payment - interest;
//             balance -= amortization;

//             installments.Add(new InstallmentDto(
//                 Number: i,
//                 DueDate: DateTime.UtcNow.AddMonths(i),
//                 Principal: Math.Round(amortization, 2),
//                 Interest: Math.Round(interest, 2),
//                 TotalPayment: Math.Round(payment, 2),
//                 RemainingBalance: Math.Round(balance, 2)
//             ));
//         }

//         return installments;
//     }

//     private IEnumerable<InstallmentDto> GenerateGermanSchedule(
//     decimal principal,
//     decimal annualRate,
//     int months)
//     {
//         var installments = new List<InstallmentDto>();

//         var monthlyRate = annualRate / 12 / 100;
//         var amortization = principal / months;
//         decimal balance = principal;

//         for (int i = 1; i <= months; i++)
//         {
//             var interest = balance * monthlyRate;
//             var payment = amortization + interest;
//             balance -= amortization;

//             installments.Add(new InstallmentDto(
//                 Number: i,
//                 DueDate: DateTime.UtcNow.AddMonths(i),
//                 Principal: Math.Round(amortization, 2),
//                 Interest: Math.Round(interest, 2),
//                 TotalPayment: Math.Round(payment, 2),
//                 RemainingBalance: Math.Round(balance, 2)
//             ));
//         }

//         return installments;
//     }

//     private IEnumerable<InstallmentDto> GenerateAmericanSchedule(
//     decimal principal,
//     decimal annualRate,
//     int months)
// {
//     var installments = new List<InstallmentDto>();

//     var monthlyRate = annualRate / 12 / 100;
//     var interestPayment = principal * monthlyRate;

//     for (int i = 1; i <= months; i++)
//     {
//         decimal amort = (i == months) ? principal : 0;
//         decimal payment = interestPayment + amort;
//         decimal remaining = (i == months) ? 0 : principal;

//         installments.Add(new InstallmentDto(
//             Number: i,
//             DueDate: DateTime.UtcNow.AddMonths(i),
//             Principal: Math.Round(amort, 2),
//             Interest: Math.Round(interestPayment, 2),
//             TotalPayment: Math.Round(payment, 2),
//             RemainingBalance: Math.Round(remaining, 2)
//         ));
//     }

//     return installments;
// }
}
