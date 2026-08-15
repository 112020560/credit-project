using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
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
    private readonly ICreditProductRepository _productRepository;
    private readonly ILogger<ApplyPaymentCommandHandler> _logger;

    public ApplyPaymentCommandHandler(
        ILoanContractRepository repository,
        ICreditProductRepository productRepository,
        ILogger<ApplyPaymentCommandHandler> logger)
    {
        _repository = repository;
        _productRepository = productRepository;
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

        // 2. Parse payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return ApplyPaymentResponse.Failed($"Invalid payment method: {request.PaymentMethod}");
        }

        var paymentId = Guid.NewGuid();
        var amount = new Money(request.Amount, request.Currency);

        // 3. Load product waterfall and social capital config (if product is assigned)
        PaymentWaterfall? waterfall = null;
        Money socialCapital = Money.Zero(request.Currency);
        SocialCapitalCollectionMode collectionMode = SocialCapitalCollectionMode.SeparateCollection;

        if (aggregate.State.ProductId.HasValue)
        {
            var product = await _productRepository.GetByIdAsync(aggregate.State.ProductId.Value, cancellationToken);
            if (product != null)
            {
                waterfall = product.Waterfall;

                if (product.SocialCapitalConfig != null)
                {
                    socialCapital = product.SocialCapitalConfig.Calculate(
                        paymentAmount: amount,
                        principalPaid: Money.Zero(request.Currency),
                        originalAmount: aggregate.State.Principal,
                        currency: request.Currency);
                    collectionMode = product.SocialCapitalConfig.CollectionMode;
                }
            }
        }

        try
        {
            aggregate.ApplyPayment(paymentId, amount, paymentMethod, waterfall, socialCapital, collectionMode);
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

        await _repository.SaveAsync(aggregate, cancellationToken);

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
            isPaidOff: isPaidOff,
            socialCapitalContributed: paymentEvent.SocialCapitalContributed.Amount);
    }
}