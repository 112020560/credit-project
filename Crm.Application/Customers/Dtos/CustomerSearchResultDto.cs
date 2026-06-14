namespace Crm.Application.Customers.Dtos;

public record CustomerSearchResultDto(
    Guid Id,
    string FullName,
    string? DisplayName,
    string? IdentificationType,
    string? IdentificationNumber,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string Status);

public record CustomerSearchPagedResponse(
    IReadOnlyList<CustomerSearchResultDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
