

using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.Customers;

public record CreateCustomerCommand(CreateCustomerDto Dto): ICommand<CustomerSummaryDto>;

internal sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, CustomerSummaryDto>
{
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMqProducerService _mqProducerService;
    public CreateCustomerCommandHandler(ILogger<CreateCustomerCommandHandler> logger, IUnitOfWork unitOfWork, IMqProducerService mqProducerService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _mqProducerService = mqProducerService;
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
            Metadata = null
        };

        // Send to credit service queue (point-to-point)
        await _mqProducerService.SendCommand<CustomerCreated>(contract, "credit-service-customer-events", Guid.NewGuid().ToString("N"), cancellationToken);

        // Broadcast publish so other services (Retail, etc.) can subscribe
        await _mqProducerService.PublishEvent(contract, Guid.NewGuid().ToString("N"), cancellationToken);

        return new CustomerSummaryDto(customer.Id, customer.FullName, customer.DisplayName ?? string.Empty, customer.IdentificationNumber ?? string.Empty);

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
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CustomerAddresses = [.. (dto.Addresses ?? []).Select(x => new CustomerAddress
            {
                Id = Guid.CreateVersion7(),
                Type = x.Type,
                Street = x.Street,
                City = x.City,
                State = x.State,
                Country = x.Country,
                PostalCode = x.PostalCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPrimary = x.IsPrimary,
            })],
            CustomerPhones = [.. (dto.Contacts ?? []).Where(x => x.Type == "Phone").Select(x => new CustomerPhone
            {
                Id = Guid.CreateVersion7(),
                Type = x.Type,
                Number = x.Value,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = x.IsPrimary,
                Verified = false,
            })],
            CustomerEmails = [.. (dto.Contacts ?? []).Where(x => x.Type == "Email").Select(x => new CustomerEmail
            {
                Id = Guid.CreateVersion7(),
                Email = x.Value,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = x.IsPrimary,
                Verified = false,
            })],
            CustomerWorkInfos = [.. (dto.WorkInfos ?? []).Select(x => new CustomerWorkInfo
            {
                Id = Guid.CreateVersion7(),
                Occupation = x.Occupation,
                EmployerName = x.EmployerName,
                Salary = x.Salary,
                WorkAddress = JsonSerializer.Serialize((dto.Addresses ?? []).Where(a => a.Type == "Work").Select(a => new
                {
                    a.Street,
                    a.City,
                    a.State,
                    a.Country,
                    a.PostalCode,
                }).FirstOrDefault()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            })]
        };
    }
}