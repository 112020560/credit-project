

using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using Crm.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.Customers;

public record CreateCustomerCommand(CreateCustomerDto Dto): ICommand<CustomerSummaryDto>;

internal sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, CustomerSummaryDto>
{
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outbox;
    public CreateCustomerCommandHandler(ILogger<CreateCustomerCommandHandler> logger, IUnitOfWork unitOfWork, IOutboxWriter outbox)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
    }
    public async Task<Result<CustomerSummaryDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var customer = MapToDto(dto);
        await _unitOfWork.CustomersRepository.AddCustomerAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var contract = new CreateCustomerContract
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            DisplayName = customer.DisplayName ?? string.Empty,
            IdentificationType = customer.IdentificationType,
            IdentificationNumber = customer.IdentificationNumber ?? string.Empty,
            TaxId = dto.TaxId,
            Email = dto.Contacts?.FirstOrDefault(x => x.Type == "Email")?.Value,
            Phone = dto.Contacts?.FirstOrDefault(x => x.Type == "Phone")?.Value,
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 1,
            Metadata = BuildMetadata(customer)
        };
        var payload = JsonSerializer.Serialize(contract);

        // Broadcast event to all subscribers
        await _outbox.AppendAsync(new OutboxEvent
        {
            ServiceName     = "crm",
            AggregateId     = customer.Id,
            AggregateType   = "Customer",
            EventType       = "CustomerCreated",
            DeduplicationKey = $"CustomerCreated:{customer.Id}",
            Payload         = payload
        }, cancellationToken);

        // Point-to-point command to credit service
        await _outbox.AppendAsync(new OutboxEvent
        {
            ServiceName     = "crm",
            AggregateId     = customer.Id,
            AggregateType   = "Customer",
            EventType       = "CustomerCreatedCommand",
            DeduplicationKey = $"CustomerCreatedCommand:{customer.Id}",
            Payload         = payload
        }, cancellationToken);

        return new CustomerSummaryDto(customer.Id, customer.FullName, customer.DisplayName ?? string.Empty, customer.IdentificationNumber ?? string.Empty);

    }

    private static IDictionary<string, object>? BuildMetadata(Customer customer)
    {
        if (customer.CreditScore is null && customer.MonthlyIncome is null && customer.MonthlyDebt is null)
            return null;

        var metadata = new Dictionary<string, object>();
        if (customer.CreditScore is not null) metadata["CreditScore"] = customer.CreditScore;
        if (customer.MonthlyIncome is not null) metadata["MonthlyIncome"] = customer.MonthlyIncome;
        if (customer.MonthlyDebt is not null) metadata["MonthlyDebt"] = customer.MonthlyDebt;
        return metadata;
    }

    private static Customer MapToDto(CreateCustomerDto dto)
    {
        return new Customer
        {
            Id = Guid.CreateVersion7(),
            IdentificationType = dto.IdentificationType,
            IdentificationNumber = dto.IdentificationNumber,
            FullName = dto.FullName,
            DisplayName = dto.DisplayName,
            BirthDate = dto.BirthDate,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreditScore = dto.CreditScore,
            MonthlyIncome = dto.MonthlyIncome,
            MonthlyDebt = dto.MonthlyDebt,
            CustomerAddresses = [.. (dto.Addresses ?? []).Select(x => new Address(x.Type, x.Street, x.City, x.State, x.Country, x.PostalCode, x.IsPrimary))],
            CustomerPhones = [.. (dto.Contacts ?? []).Where(x => x.Type == "Phone").Select(x => new PhoneContact(x.Value, x.Type, null, x.IsPrimary, false))],
            CustomerEmails = [.. (dto.Contacts ?? []).Where(x => x.Type == "Email").Select(x => new EmailContact(x.Value, x.IsPrimary, false))],
            CustomerWorkInfos = [.. (dto.WorkInfos ?? []).Select(x => new WorkInfo(x.Occupation, x.EmployerName, x.Salary,
                WorkAddress: JsonSerializer.Serialize((dto.Addresses ?? []).Where(a => a.Type == "Work").Select(a => new { a.Street, a.City, a.State, a.Country, a.PostalCode }).FirstOrDefault())))]
        };
    }
}