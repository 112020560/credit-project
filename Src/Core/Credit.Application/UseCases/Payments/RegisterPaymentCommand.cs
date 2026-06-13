

using Credit.Application.Abstractions.Messaging;
using Credit.Application.UseCases.Payments.Dtos;
using Credit.Domain.Models;

namespace Credit.Application.Commands;

public record RegisterPaymentCommand(
    Guid CreditLineId,
    CreatePaymentsDto PaymentDto
): ICommand<PaymentScheduleModel>;