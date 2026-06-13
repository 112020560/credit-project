using System;
using Credit.Domain.ValueObjects;
using MediatR;

namespace Credit.Application.Commands;

public sealed record CreateCreditContract(
    CreditId CreditId,
    string Currency,
    DateTime When
) : IRequest;
