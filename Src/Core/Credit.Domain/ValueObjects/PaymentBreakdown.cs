using System;

namespace Credit.Domain.ValueObjects;

public sealed record PaymentBreakdown(
    Money Penalty,
    Money Interest,
    Money Principal
);