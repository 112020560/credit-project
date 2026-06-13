using System;
using System.Text.Json;
using Credit.Domain.Entities;
using Credit.Domain.Models;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class PaymentsRepository
{
    private readonly CreditDbContext _context;
    public PaymentsRepository(CreditDbContext context)
    {
        _context = context;
    }

    //Get payments by credit id
    public async Task<PaymentScheduleModel[]> GetPaymentsByCreditIdAsync(Guid creditId, CancellationToken cancellationToken)
    {
        var creditline = await _context.CreditLines.FindAsync(creditId, cancellationToken);
        if (creditline == null)
        {
            return [];
        }

        if (string.IsNullOrEmpty(creditline.AmortizationSchedule))
        {
            return [];
        }
        var payments = JsonSerializer.Deserialize<PaymentScheduleModel[]>(creditline.AmortizationSchedule);

        return payments ?? [];
    }

    //Creaete payment on entity Installment
    public async Task CreatePaymentAsync(Installment installment, CancellationToken cancellationToken)
    {
       await _context.Installments.AddAsync(installment, cancellationToken);
    }
}
