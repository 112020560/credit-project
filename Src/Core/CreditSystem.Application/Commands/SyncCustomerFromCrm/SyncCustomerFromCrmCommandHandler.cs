using CreditSystem.Domain.Abstractions.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.SyncCustomerFromCrm;

public class SyncCustomerFromCrmCommandHandler : IRequestHandler<SyncCustomerFromCrmCommand>
{
    private readonly ICustomerCreditProfileRepository _repository;
    private readonly ILogger<SyncCustomerFromCrmCommandHandler> _logger;

    public SyncCustomerFromCrmCommandHandler(
        ICustomerCreditProfileRepository repository,
        ILogger<SyncCustomerFromCrmCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(SyncCustomerFromCrmCommand command, CancellationToken cancellationToken)
    {
        await _repository.UpsertAsync(
            externalId: command.ExternalId,
            fullName: command.FullName,
            email: command.Email,
            phone: command.Phone,
            documentType: command.DocumentType,
            documentNumber: command.DocumentNumber,
            creditScore: command.CreditScore,
            monthlyIncome: command.MonthlyIncome,
            monthlyDebt: command.MonthlyDebt,
            ct: cancellationToken);

        _logger.LogInformation(
            "Customer reference synced from CRM for external ID {ExternalId}",
            command.ExternalId);
    }
}
