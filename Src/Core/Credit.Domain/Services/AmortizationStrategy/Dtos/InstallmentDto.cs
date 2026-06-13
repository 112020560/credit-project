using System;

namespace Credit.Domain.Services.Dtos;

public record InstallmentDto
(
    int Number,
    DateTime DueDate,
    decimal Principal,
    decimal Interest,
    decimal TotalPayment,
    decimal RemainingBalance
);
