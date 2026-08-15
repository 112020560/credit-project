using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.DisburseLoan;

public class DisburseLoanCommandHandler : IRequestHandler<DisburseLoanCommand, DisburseLoanResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly ILogger<DisburseLoanCommandHandler> _logger;

    public DisburseLoanCommandHandler(
        ILoanContractRepository repository,
        ILogger<DisburseLoanCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DisburseLoanResponse> Handle(
        DisburseLoanCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return DisburseLoanResponse.Failed($"Loan {request.LoanId} not found");
        }

        try
        {
            aggregate.Disburse(
                request.DisbursementMethod.ToUpperInvariant(),
                request.DestinationAccount);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot disburse loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return DisburseLoanResponse.Failed(ex.Message);
        }

        await _repository.SaveAsync(aggregate, cancellationToken);

        _logger.LogInformation(
            "Loan {LoanId} disbursed via {Method} to {Account}",
            request.LoanId,
            request.DisbursementMethod,
            request.DestinationAccount);

        return DisburseLoanResponse.Disbursed(
            aggregate.Id,
            aggregate.State.Principal.Amount,
            aggregate.State.DisbursedAt!.Value);
    }
}