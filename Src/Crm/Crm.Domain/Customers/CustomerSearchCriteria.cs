namespace Crm.Domain.Customers;

public record CustomerSearchCriteria(string? Query, int Page, int PageSize);
