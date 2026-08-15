using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.FailDisbursement;

public class FailDisbursementCommandHandler : IRequestHandler<FailDisbursementCommand, FailDisbursementResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly ILogger<FailDisbursementCommandHandler> _logger;

    public FailDisbursementCommandHandler(
        ILoanContractRepository repository,
        ILogger<FailDisbursementCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FailDisbursementResponse> Handle(
        FailDisbursementCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return FailDisbursementResponse.Error($"Loan {request.LoanId} not found");
        }

        var failedAt = DateTime.UtcNow;

        try
        {
            aggregate.FailDisbursement(request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot fail disbursement for loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return FailDisbursementResponse.Error(ex.Message);
        }

        await _repository.SaveAsync(aggregate, cancellationToken);

        _logger.LogInformation(
            "Disbursement failed for loan {LoanId}, reason: {Reason}",
            request.LoanId,
            request.Reason);

        return FailDisbursementResponse.Failed(aggregate.Id, request.Reason, failedAt);
    }
}
