using System;
using Crm.Application.Abstractions.Mq;
using MassTransit;

namespace Crm.Infrastructure.Adapters.Outbound.Messaging.RabbitMq;

public class MqRequestResponse<T>(IRequestClient<T> requestClient): IMqRequestResponse<T> where T : class
{
    public async Task<TResponse> ExecuteAsync<TResponse>(T transferData, CancellationToken cancellationToken) where TResponse : class
    {
        var request = requestClient.Create(transferData, cancellationToken );
        var response = await request.GetResponse<TResponse>();

        return response.Message;
    }
}
