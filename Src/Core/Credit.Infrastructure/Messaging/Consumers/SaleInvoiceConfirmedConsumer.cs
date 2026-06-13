using System.Text.Json;
using Credit.Application.Abstractions.Persistence;
using Credit.Domain.Entities;
using Credit.Domain.Enums;
using Credit.Domain.Models;
using Credit.Domain.Services.AmortizationStrategy;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Contracts.Sales;

namespace Credit.Infrastructure.Messaging.Consumers;

public sealed class SaleInvoiceConfirmedConsumer(
    ICreditCustomerRepository customerRepository,
    ICreditLineRepository creditLineRepository,
    IUnitOfWork unitOfWork,
    CreditDbContext dbContext,
    AmortizationScheduleService amortizationService,
    ILogger<SaleInvoiceConfirmedConsumer> logger)
    : IConsumer<SaleInvoiceConfirmedEvent>
{
    public async Task Consume(ConsumeContext<SaleInvoiceConfirmedEvent> context)
    {
        var evt = context.Message;

        if (evt.CreditAmount <= 0 || evt.CustomerId is null)
            return;

        // Idempotency: skip if a credit line already exists for this invoice
        if (await creditLineRepository.ExistsByInvoiceNumberAsync(evt.InvoiceNumber, context.CancellationToken))
        {
            logger.LogInformation(
                "Credit line already created for invoice {InvoiceNumber} — skipping",
                evt.InvoiceNumber);
            return;
        }

        // Find the credit customer by the Retail customer GUID
        var customer = await customerRepository.GetByExternalIdAsync(
            evt.CustomerId.Value, context.CancellationToken);

        if (customer is null)
        {
            logger.LogWarning(
                "Customer {CustomerId} not found in credit system for invoice {InvoiceNumber}",
                evt.CustomerId, evt.InvoiceNumber);
            return;
        }

        // Load credit product
        CreditProduct? product = null;

        if (evt.CreditProductId.HasValue)
            product = await dbContext.CreditProducts
                .FirstOrDefaultAsync(p => p.Id == evt.CreditProductId.Value, context.CancellationToken);

        product ??= await dbContext.CreditProducts
            .FirstOrDefaultAsync(context.CancellationToken);

        if (product is null)
        {
            logger.LogError(
                "No credit product found. Cannot create credit line for invoice {InvoiceNumber}",
                evt.InvoiceNumber);
            return;
        }

        // Parse amortization method (default French if unknown)
        var method = Enum.TryParse<AmortizationMethod>(product.AmortizationMethod, ignoreCase: true, out var parsed)
            ? parsed
            : AmortizationMethod.French;

        // Calculate amortization schedule
        var installments = amortizationService
            .GenerateSchedule(evt.CreditAmount, product.InterestRate, product.TermMonths, method)
            .ToList();

        var scheduleModels = installments.Select(i => new PaymentScheduleModel
        {
            QuotaNumber = i.Number,
            PaymentDate = i.DueDate,
            CapitalAmmount = i.Principal,
            InterestAmount = i.Interest,
            Balance = i.RemainingBalance,
            Status = "Pending"
        }).ToArray();

        var scheduleJson = JsonSerializer.Serialize(scheduleModels);
        var metadataJson = JsonSerializer.Serialize(new
        {
            invoiceNumber = evt.InvoiceNumber,
            invoiceId = evt.InvoiceId
        });

        var today = DateOnly.FromDateTime(evt.ConfirmedAt.UtcDateTime);

        var creditLine = new CreditLine
        {
            Id = Guid.NewGuid(),
            ApplicationId = null,
            CustomerId = customer.Id,
            ProductId = product.Id,
            Principal = evt.CreditAmount,
            Outstanding = evt.CreditAmount,
            Currency = evt.Currency,
            StartDate = today,
            EndDate = today.AddMonths(product.TermMonths),
            Status = "Active",
            AmortizationSchedule = scheduleJson,
            Metadata = metadataJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await creditLineRepository.AddAsync(creditLine, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Credit line {CreditLineId} created for customer {CreditCustomerId}, invoice {InvoiceNumber}, amount {Amount}",
            creditLine.Id, customer.Id, evt.InvoiceNumber, evt.CreditAmount);
    }
}
