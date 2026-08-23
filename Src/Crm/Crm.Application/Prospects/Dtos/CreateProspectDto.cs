namespace Crm.Application.Prospects.Dtos;

public record CreateProspectDto(
    string IdentificationType,
    string IdentificationNumber,
    string FullName,
    string? DisplayName,
    DateOnly? BirthDate,
    IEnumerable<ProspectContactDto>? Contacts);
