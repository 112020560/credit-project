using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;

using CreditSystem.Domain.Aggregates.RevolvingCredit;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.RevolvingCredit.CreateCreditLine;

public class CreateCreditLineCommandHandler : IRequestHandler<CreateCreditLineCommand, CreateCreditLineResponse>
{
    private readonly IRevolvingCreditRepository _repository;
    private readonly ICustomerReadRepository _customerService;
    private readonly ContractEngine _contractEngine;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<CreateCreditLineCommandHandler> _logger;

    public CreateCreditLineCommandHandler(
        IRevolvingCreditRepository repository,
        ICustomerReadRepository customerService,
        ContractEngine contractEngine,
        IProjectionEngine projectionEngine,
        ILogger<CreateCreditLineCommandHandler> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _contractEngine = contractEngine;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<CreateCreditLineResponse> Handle(
        CreateCreditLineCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByExternalIdAsync(
            request.ExternalCustomerId, cancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer {ExternalId} not found", request.ExternalCustomerId);
            return CreateCreditLineResponse.Failed($"Customer {request.ExternalCustomerId} not found");
        }

        decimal interestRate;

        if (request.InterestRate.HasValue)
        {
            interestRate = request.InterestRate.Value;
        }
        else
        {
            // Use underwriting engine to calculate rate
            var context = new ContractEvaluationContext
            {
                Customer = customer,
                RequestedAmount = new Money(request.CreditLimit, request.Currency),
                TermMonths = 12, // Revolving credit has no fixed term; 12 months used for evaluation
                CreditScore = customer.CreditScore,
                MonthlyIncome = customer.MonthlyIncome.HasValue ? new Money(customer.MonthlyIncome.Value, request.Currency) : null,
                MonthlyDebt = customer.MonthlyDebt.HasValue ? new Money(customer.MonthlyDebt.Value, request.Currency) : null
            };

            var evaluation = await _contractEngine.EvaluateAsync(context, ct: cancellationToken);

            if (!evaluation.Approved)
            {
                _logger.LogWarning("Credit line denied for customer {CustomerId}", customer.Id);
                return CreateCreditLineResponse.Failed("Credit line application denied based on evaluation rules");
            }

            interestRate = evaluation.InterestRate;
        }

        // 3. Crear aggregate
        var aggregate = RevolvingCreditAggregate.Create(
            customerId: customer.Id,
            creditLimit: new Money(request.CreditLimit, request.Currency),
            rate: new InterestRate(interestRate),
            minimumPaymentPercentage: request.MinimumPaymentPercentage,
            minimumPaymentAmount: new Money(request.MinimumPaymentAmount, request.Currency),
            billingCycleDay: request.BillingCycleDay,
            gracePeriodDays: request.GracePeriodDays);

        var events = aggregate.UncommittedEvents.ToList();
        await _repository.SaveAsync(aggregate, cancellationToken);
        try
        {
            await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to project events for credit line {CreditLineId}. Read models can be rebuilt.",
                aggregate.Id);
        }

        _logger.LogInformation(
            "Credit line {CreditLineId} created for customer {CustomerId} with limit {Limit}",
            aggregate.Id, customer.Id, request.CreditLimit);

        return CreateCreditLineResponse.Created(
            aggregate.Id,
            request.CreditLimit,
            interestRate,
            request.BillingCycleDay);
    }
}