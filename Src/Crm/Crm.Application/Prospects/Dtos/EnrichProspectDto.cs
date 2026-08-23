namespace Crm.Application.Prospects.Dtos;

public record EnrichProspectAddressDto(string? Type, string? Street, string? City, string? State, string? Country, string? PostalCode, bool IsPrimary);

public record EnrichProspectWorkInfoDto(string? Occupation, string? EmployerName, decimal? Salary);

public record EnrichProspectFiscalInfoDto(string? TaxId, string? TaxRegime, string? EconomicActivity, string? Industry);

public record EnrichProspectDto(
    IEnumerable<ProspectContactDto>? Contacts,
    IEnumerable<EnrichProspectAddressDto>? Addresses,
    IEnumerable<EnrichProspectWorkInfoDto>? WorkInfos,
    IEnumerable<EnrichProspectFiscalInfoDto>? FiscalInfos);
