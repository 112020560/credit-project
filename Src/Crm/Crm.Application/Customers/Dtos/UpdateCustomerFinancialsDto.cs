namespace Crm.Application.Customers.Dtos;

public record UpdateCustomerFinancialsDto(
    decimal? CreditScore,
    decimal? MonthlyIncome,
    decimal? MonthlyDebt);
