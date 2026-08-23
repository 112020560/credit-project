using System;

namespace Crm.Application.Abstractions.Mq;

public interface IMqProducerService
{
    Task SendCommand<TBody>(TBody payload, string command, string traceId, CancellationToken cancellationToken);
    Task PublishEvent<TBody>(TBody payload, string traceId, CancellationToken cancellationToken);
}
