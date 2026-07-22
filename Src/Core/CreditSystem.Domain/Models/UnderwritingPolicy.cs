using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Models;

public record UnderwritingPolicy(
    decimal BaseInterestRate,
    int AutoDefaultThresholdDays,
    NoScoreBehavior NoScoreBehavior);
