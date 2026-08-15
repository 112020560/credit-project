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
    decimal OriginationFeeRate = 0m,
    bool EnforceSharesCapacityLimit = false,
    decimal MaxDtiRatio = 0.50m,
    string Id = "default");
