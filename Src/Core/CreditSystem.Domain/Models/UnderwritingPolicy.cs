using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Models;

public record UnderwritingPolicy(
    decimal BaseInterestRate,
    int AutoDefaultThresholdDays,
    NoScoreBehavior NoScoreBehavior,
    int SharesMultiplierLimit = 5,
    bool RequireActiveMembership = false,
    int GracePeriodDays = 5,
    decimal PenaltyRate = 0m,
    decimal OriginationFeeRate = 0m);
