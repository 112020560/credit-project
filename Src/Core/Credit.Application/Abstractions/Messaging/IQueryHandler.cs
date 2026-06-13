using MediatR;
using SharedKernel;

namespace Credit.Application.Abstractions.Messaging;


public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;