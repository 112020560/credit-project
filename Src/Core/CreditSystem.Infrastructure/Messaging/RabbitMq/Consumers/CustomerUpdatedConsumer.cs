using CreditSystem.Application.Commands.SyncCustomerFromCrm;
using MassTransit;
using MediatR;
using SharedKernel.Contracts.Crm.Customers;

namespace CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;

public class CustomerUpdatedConsumer : IConsumer<CustomerUpdated>
{
    private readonly IMediator _mediator;

    public CustomerUpdatedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<CustomerUpdated> context)
    {
        var message = context.Message;
        var changes = message.Changes;

        var command = new SyncCustomerFromCrmCommand
        {
            ExternalId = message.CustomerId,
            FullName = GetString(changes, "FullName"),
            Email = GetString(changes, "Email"),
            Phone = GetString(changes, "Phone"),
            DocumentType = GetString(changes, "IdentificationType"),
            DocumentNumber = GetString(changes, "IdentificationNumber"),
            CreditScore = GetInt(changes, "CreditScore"),
            MonthlyIncome = GetDecimal(changes, "MonthlyIncome"),
            MonthlyDebt = GetDecimal(changes, "MonthlyDebt")
        };

        await _mediator.Send(command, context.CancellationToken);
    }

    private static string? GetString(IDictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.TryGetValue(key, out var value)) return null;
        return value?.ToString();
    }

    private static int? GetInt(IDictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.TryGetValue(key, out var value)) return null;
        return value is null ? null : Convert.ToInt32(value);
    }

    private static decimal? GetDecimal(IDictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.TryGetValue(key, out var value)) return null;
        return value is null ? null : Convert.ToDecimal(value);
    }
}
