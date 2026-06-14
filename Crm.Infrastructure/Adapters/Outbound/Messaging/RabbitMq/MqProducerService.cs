using System;
using System.Text.Json;
using Crm.Application.Abstractions.Mq;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Adapters.Outbound.Messaging.RabbitMq;

public class MqProducerService : IMqProducerService
{
    private readonly IBus _bus;
    private readonly ILogger<MqProducerService> _logger;
    public MqProducerService(ILogger<MqProducerService> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task SendCommand<TBody>(TBody payload, string command, string traceId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Send message {message} to queue: {command}", JsonSerializer.Serialize(payload), command);
        if (payload is null) throw new Exception("The SendCommand payload is null");
        var url = new Uri($"queue:{command}");
        var endpoint = await _bus.GetSendEndpoint(url);
        await endpoint.Send(payload, sendContext =>
        {
            sendContext.Headers.Set("X-Trace-Id", traceId);
        }, cancellationToken);
    }

    public async Task PublishEvent<TBody>(TBody payload, string traceId, CancellationToken cancellationToken)
    {
        if (payload is null) throw new Exception("The PublishEvent payload is null");
        await _bus.Publish(payload, sendContext =>
        {
            sendContext.Headers.Set("X-Trace-Id", traceId);
        }, cancellationToken);
    }
}
