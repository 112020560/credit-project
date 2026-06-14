namespace Crm.Application.Customers.Dtos;

public record CustomerWorkInfoDto(
    string Occupation,
    string EmployerName,
    decimal Salary,
    string MetaData
);