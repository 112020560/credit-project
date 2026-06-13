using System;

namespace Credit.Application.UseCases.Payments.Dtos;

public class CreatePaymentsDto
{
    public int QuotaNumber {get; set;}
    public decimal Amount {get; set;}
    public string Method {get; set;} = "CASH";
    public string Currency {get; set;} = "USD";
    public DateTime PaymentDate {get; set;}
}
