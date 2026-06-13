using System;
using Credit.Domain.Enums;
using Credit.Domain.Services.Dtos;

namespace Credit.Domain.Services.AmortizationStrategy;

public interface IAmortizationStrategy
{
    public AmortizationMethod Method { get; }
    IEnumerable<InstallmentDto> GenerateSchedule(
        decimal principal,
        decimal annualRate,
        int months);
}
