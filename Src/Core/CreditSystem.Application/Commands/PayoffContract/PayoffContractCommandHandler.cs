using CreditSystem.Application.Queries.GetPayoffAmount;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.PayoffContract;

public class PayoffContractCommandHandler : IRequestHandler<PayoffContractCommand, PayoffContractResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<PayoffContractCommandHandler> _logger;

    public PayoffContractCommandHandler(
        ILoanContractRepository repository,
        IMediator mediator,
        ILogger<PayoffContractCommandHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PayoffContractResponse> Handle(
        PayoffContractCommand request,
        CancellationToken cancellationToken)
    {
        var payoffQuery = new GetPayoffAmountQuery { LoanId = request.LoanId };
        var payoffInfo = await _mediator.Send(payoffQuery, cancellationToken);

        if (payoffInfo == null)
        {
            _logger.LogWarning("Loan {LoanId} not found", request.LoanId);
            return PayoffContractResponse.Failed($"Loan {request.LoanId} not found");
        }

        var aggregate = await _repository.GetByIdAsync(request.LoanId, cancellationToken);

        if (aggregate == null)
        {
            return PayoffContractResponse.Failed($"Loan {request.LoanId} not found");
        }

        if (aggregate.State.Status == ContractStatus.PaidOff)
        {
            return PayoffContractResponse.Failed("Loan is already paid off");
        }

        if (aggregate.State.Status != ContractStatus.Active &&
            aggregate.State.Status != ContractStatus.Delinquent &&
            aggregate.State.Status != ContractStatus.Restructured)
        {
            return PayoffContractResponse.Failed(
                $"Cannot payoff loan with status: {aggregate.State.Status}");
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return PayoffContractResponse.Failed($"Invalid payment method: {request.PaymentMethod}");
        }

        var paymentId = Guid.NewGuid();
        var payoffAmount = new Money(payoffInfo.TotalPayoffAmount, payoffInfo.Currency);

        try
        {
            aggregate.ApplyPayment(paymentId, payoffAmount, paymentMethod);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot payoff loan {LoanId}: {Error}",
                request.LoanId, ex.Message);
            return PayoffContractResponse.Failed(ex.Message);
        }

        var paymentEvent = aggregate.UncommittedEvents
            .OfType<PaymentApplied>()
            .FirstOrDefault();

        var paidOffEvent = aggregate.UncommittedEvents
            .OfType<ContractPaidOff>()
            .FirstOrDefault();

        if (paidOffEvent == null)
        {
            return PayoffContractResponse.Failed("Payoff did not complete - balance may not be zero");
        }

        await _repository.SaveAsync(aggregate, cancellationToken);

        _logger.LogInformation(
            "Loan {LoanId} paid off. Amount: {Amount}, Early: {Early}",
            request.LoanId,
            payoffInfo.TotalPayoffAmount,
            paidOffEvent.EarlyPayoff);

        return PayoffContractResponse.PaidOff(
            loanId: aggregate.Id,
            paymentId: paymentId,
            payoffAmount: payoffInfo.TotalPayoffAmount,
            principalPaid: paymentEvent?.PrincipalPaid.Amount ?? 0,
            interestPaid: paymentEvent?.InterestPaid.Amount ?? 0,
            feesPaid: paymentEvent?.FeePaid.Amount ?? 0,
            totalPrincipalPaid: paidOffEvent.TotalPrincipalPaid.Amount,
            totalInterestPaid: paidOffEvent.TotalInterestPaid.Amount,
            totalFeesPaid: paidOffEvent.TotalFeesPaid.Amount,
            earlyPayoff: paidOffEvent.EarlyPayoff,
            paidOffAt: paidOffEvent.PaidOffAt);
    }
}