using System;

namespace Crm.Application.Abstractions.Mq;

public interface IMqRequestResponse<in T>  where T : class
{
    Task<TResponse> ExecuteAsync<TResponse>(T transferData, CancellationToken cancellationToken)
        where TResponse : class;    
}
