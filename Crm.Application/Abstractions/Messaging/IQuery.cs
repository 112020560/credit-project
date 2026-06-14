using MediatR;
using SharedKernel;

namespace Crm.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;