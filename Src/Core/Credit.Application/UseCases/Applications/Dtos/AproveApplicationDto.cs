using System;

namespace Credit.Application.UseCases.Applications.Dtos;

public class ApproveApplicationDto
{
    public decimal approved_amount { get; set; }
    public int approved_term_months { get; set; }
    public decimal approved_rate { get; set; }
}
