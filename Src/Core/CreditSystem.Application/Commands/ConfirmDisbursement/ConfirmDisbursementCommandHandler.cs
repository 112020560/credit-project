using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.ConfirmDisbursement;

public class ConfirmDisbursementCommandHandler : IRequestHandler<ConfirmDisbursementCommand, ConfirmDisbursementResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly ILogger<ConfirmDisbursementCommandHandler> _logger;

    public ConfirmDisbursementCommandHandler(
        ILoanContractRepository repository,
        ILogger<ConfirmDisbursementCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ConfirmDisbursementResponse> Handle(
        ConfirmDisbursementCommand request,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return ConfirmDisbursementResponse.Failed($"Loan {request.LoanId} not found");
        }

        var confirmedAt = DateTime.UtcNow;

        try
        {
            aggregate.ConfirmDisbursement(request.ConfirmedBy);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot confirm disbursement for loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return ConfirmDisbursementResponse.Failed(ex.Message);
        }

        await _repository.SaveAsync(aggregate, cancellationToken);

        _logger.LogInformation(
            "Disbursement confirmed for loan {LoanId} by {ConfirmedBy}",
            request.LoanId,
            request.ConfirmedBy);

        return ConfirmDisbursementResponse.Confirmed(aggregate.Id, request.ConfirmedBy, confirmedAt);
    }
}
