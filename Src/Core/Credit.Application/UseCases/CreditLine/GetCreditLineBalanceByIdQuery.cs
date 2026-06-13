using System;
using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;

namespace Credit.Application.UseCases.CreditLine;

public record GetCreditLineBalanceByIdQuery(Guid CreditLineId): IQuery<CreditLineBalanceModel>;
