using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Prospects;
using Crm.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.Prospects;

public record CreateProspectCommand(CreateProspectDto Dto) : ICommand<ProspectSummaryDto>;

internal sealed class CreateProspectCommandHandler(
    ILogger<CreateProspectCommandHandler> logger,
    IUnitOfWork unitOfWork,
    IOutboxWriter outbox)
    : ICommandHandler<CreateProspectCommand, ProspectSummaryDto>
{
    public async Task<Result<ProspectSummaryDto>> Handle(CreateProspectCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var exists = await unitOfWork.ProspectsRepository.ExistsByIdentificationAsync(
            dto.IdentificationType, dto.IdentificationNumber, cancellationToken);

        if (exists)
            return Result.Failure<ProspectSummaryDto>(ProspectError.DuplicateIdentification(dto.IdentificationNumber));

        var prospect = new Prospect
        {
            Id = Guid.CreateVersion7(),
            IdentificationType = dto.IdentificationType,
            IdentificationNumber = dto.IdentificationNumber,
            FullName = dto.FullName,
            DisplayName = dto.DisplayName,
            BirthDate = dto.BirthDate,
            Status = ProspectStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Phones = [.. (dto.Contacts ?? []).Where(c => c.Type == "Phone").Select(c => new PhoneContact(c.Value, c.Type, null, c.IsPrimary, false))],
            Emails = [.. (dto.Contacts ?? []).Where(c => c.Type == "Email").Select(c => new EmailContact(c.Value, c.IsPrimary, false))],
        };

        await unitOfWork.ProspectsRepository.AddAsync(prospect, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await outbox.AppendAsync(new OutboxEvent
        {
            ServiceName      = "crm",
            AggregateId      = prospect.Id,
            AggregateType    = "Prospect",
            EventType        = "ProspectCreated",
            DeduplicationKey = $"ProspectCreated:{prospect.Id}",
            Payload          = JsonSerializer.Serialize(new ProspectCreatedEvent(prospect.Id, prospect.FullName, prospect.Status.ToString()))
        }, cancellationToken);

        logger.LogInformation("Prospect {ProspectId} created for {IdentificationNumber}", prospect.Id, prospect.IdentificationNumber);

        return new ProspectSummaryDto(prospect.Id, prospect.FullName, prospect.DisplayName, prospect.IdentificationNumber, prospect.Status);
    }
}

public record ProspectCreatedEvent(Guid ProspectId, string FullName, string Status);

internal sealed class CreateProspectCommandValidator : AbstractValidator<CreateProspectCommand>
{
    public CreateProspectCommandValidator()
    {
        RuleFor(x => x.Dto.IdentificationType).NotEmpty();
        RuleFor(x => x.Dto.IdentificationNumber).NotEmpty();
        RuleFor(x => x.Dto.FullName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Dto.Contacts)
            .Must(c => c != null && c.Any())
            .WithMessage("At least one contact (email or phone) is required");
    }
}
