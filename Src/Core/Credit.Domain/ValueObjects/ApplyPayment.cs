using System;

namespace Credit.Domain.ValueObjects;

public sealed record ApplyPayment(
    Money Amount,
    DateTime PaymentDate
);
