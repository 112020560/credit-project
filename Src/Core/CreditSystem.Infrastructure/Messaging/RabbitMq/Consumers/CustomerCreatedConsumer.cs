using CreditSystem.Application.Commands.SyncCustomerFromCrm;
using MassTransit;
using MediatR;
using SharedKernel.Contracts.Crm.Customers;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;

public class CustomerCreatedConsumer : IConsumer<CustomerCreated>
{
    private readonly IMediator _mediator;

    public CustomerCreatedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<CustomerCreated> context)
    {
        var message = context.Message;

        var command = new SyncCustomerFromCrmCommand
        {
            ExternalId = message.CustomerId,
            FullName = message.FullName,
            Email = message.Email,
            Phone = message.Phone,
            DocumentType = message.IdentificationType,
            DocumentNumber = message.IdentificationNumber,
            CreditScore = ExtractInt(message.Metadata, "CreditScore"),
            MonthlyIncome = ExtractDecimal(message.Metadata, "MonthlyIncome"),
            MonthlyDebt = ExtractDecimal(message.Metadata, "MonthlyDebt")
        };

        await _mediator.Send(command, context.CancellationToken);
    }

    private static int? ExtractInt(IDictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.TryGetValue(key, out var value)) return null;
        return value is null ? null : Convert.ToInt32(value);
    }

    private static decimal? ExtractDecimal(IDictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.TryGetValue(key, out var value)) return null;
        return value is null ? null : Convert.ToDecimal(value);
    }
}
