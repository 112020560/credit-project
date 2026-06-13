using System;
using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;

namespace Credit.Application.UseCases.Payments;

public record GetLineInstallmentsByIdQuery(Guid CreditLineId): IQuery<InstallmentModel>;
