using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.ApplyPayment;

public class ApplyPaymentCommandHandler : IRequestHandler<ApplyPaymentCommand, ApplyPaymentResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<ApplyPaymentCommandHandler> _logger;

    public ApplyPaymentCommandHandler(
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<ApplyPaymentCommandHandler> logger)
    {
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<ApplyPaymentResponse> Handle(
        ApplyPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return ApplyPaymentResponse.Failed($"Loan {request.LoanId} not found");
        }

        // 2. Parsear método de pago
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return ApplyPaymentResponse.Failed($"Invalid payment method: {request.PaymentMethod}");
        }

        // 3. Ejecutar comando en el aggregate
        var paymentId = Guid.NewGuid();
        var amount = new Money(request.Amount, request.Currency);

        try
        {
            aggregate.ApplyPayment(paymentId, amount, paymentMethod);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot apply payment to loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return ApplyPaymentResponse.Failed(ex.Message);
        }

        // 4. Obtener el evento de pago para extraer detalles
        var paymentEvent = aggregate.UncommittedEvents
            .OfType<PaymentApplied>()
            .FirstOrDefault();

        if (paymentEvent == null)
        {
            return ApplyPaymentResponse.Failed("Payment event not generated");
        }

        // Guardar eventos ANTES de persistir
        var events = aggregate.UncommittedEvents.ToList();

        // 5. Persistir eventos (fuente de verdad)
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 6. Proyectar eventos a read models
        try
        {
            await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            // Los eventos ya están persistidos - la proyección puede reconstruirse
            // Loguear como warning y continuar para no perder la respuesta al cliente
            _logger.LogWarning(ex,
                "Failed to project events for loan {LoanId}. Events are persisted and read models can be rebuilt.",
                request.LoanId);
        }

        var isPaidOff = aggregate.State.Status == ContractStatus.PaidOff;

        _logger.LogInformation(
            "Payment {PaymentId} of {Amount} applied to loan {LoanId}. New balance: {Balance}. PaidOff: {IsPaidOff}",
            paymentId,
            request.Amount,
            request.LoanId,
            paymentEvent.NewBalance.Amount,
            isPaidOff);

        return ApplyPaymentResponse.Applied(
            paymentId: paymentId,
            loanId: aggregate.Id,
            totalApplied: paymentEvent.TotalAmount.Amount,
            principalPaid: paymentEvent.PrincipalPaid.Amount,
            interestPaid: paymentEvent.InterestPaid.Amount,
            feesPaid: paymentEvent.FeePaid.Amount,
            newBalance: paymentEvent.NewBalance.Amount,
            isPaidOff: isPaidOff);
    }
}