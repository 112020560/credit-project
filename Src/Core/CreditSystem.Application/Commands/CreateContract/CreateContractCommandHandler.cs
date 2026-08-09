using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Commands.CreateContract;

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, CreateContractResponse>
{
    private readonly ILoanContractRepository _repository;
    private readonly ICustomerReadRepository _customerService;
    private readonly ICooperativeMemberRepository _memberRepository;
    private readonly ICreditProductRepository _productRepository;
    private readonly ILoanQueryService _queryService;
    private readonly ContractEngine _contractEngine;
    private readonly UnderwritingPolicy _policy;
    private readonly IAmortizationCalculatorFactory _calculatorFactory;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<CreateContractCommandHandler> _logger;

    public CreateContractCommandHandler(
        ILoanContractRepository repository,
        ICustomerReadRepository customerService,
        ICooperativeMemberRepository memberRepository,
        ICreditProductRepository productRepository,
        ILoanQueryService queryService,
        ContractEngine contractEngine,
        UnderwritingPolicy policy,
        IAmortizationCalculatorFactory calculatorFactory,
        IProjectionEngine projectionEngine,
        ILogger<CreateContractCommandHandler> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _memberRepository = memberRepository;
        _productRepository = productRepository;
        _queryService = queryService;
        _contractEngine = contractEngine;
        _policy = policy;
        _calculatorFactory = calculatorFactory;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task<CreateContractResponse> Handle(
        CreateContractCommand request, 
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByExternalIdAsync(
            request.ExternalCustomerId,
            cancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer not found for external ID {ExternalId}", request.ExternalCustomerId);
            return CreateContractResponse.Failed($"Customer with external ID {request.ExternalCustomerId} not found");
        }

        // Resolver producto de crédito
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Credit product not found: {ProductId}", request.ProductId);
            return CreateContractResponse.Failed("Credit product not found");
        }

        if (product.Status != ProductStatus.Active)
        {
            _logger.LogWarning("Credit product {ProductId} is not active", request.ProductId);
            return CreateContractResponse.Failed("Credit product is not active");
        }

        // Resolver membresía cooperativa
        var member = await _memberRepository.GetByExternalIdAsync(request.ExternalCustomerId, cancellationToken);

        if (member == null && _policy.RequireActiveMembership)
        {
            _logger.LogWarning("Applicant {ExternalId} is not a registered cooperative member", request.ExternalCustomerId);
            return CreateContractResponse.Failed("Applicant is not a registered cooperative member");
        }

        var hasActiveLoans = await _queryService.HasActiveLoansAsync(customer.Id, cancellationToken);

        // 2. Evaluar reglas del motor
        var evaluationContext = new ContractEvaluationContext
        {
            Customer = customer,
            RequestedAmount = new Money(request.Amount, request.Currency),
            TermMonths = request.TermMonths,
            CollateralValue = request.CollateralValue.HasValue ? new Money(request.CollateralValue.Value, request.Currency) : null,
            CreditScore = customer.CreditScore,
            MonthlyIncome = customer.MonthlyIncome.HasValue ? new Money(customer.MonthlyIncome.Value, request.Currency) : null,
            MonthlyDebt = customer.MonthlyDebt.HasValue ? new Money(customer.MonthlyDebt.Value, request.Currency) : null,
            HasActiveLoans = hasActiveLoans,
            MemberSharesAmount = member?.State.Shares.TotalAmount,
            IsActiveMember = member != null ? member.State.Status == MemberStatus.Active : null,
            Product = product
        };

        var effectiveBaseRate = product.Rates.BaseInterestRate ?? _policy.BaseInterestRate;
        var evaluation = await _contractEngine.EvaluateAsync(evaluationContext, effectiveBaseRate, cancellationToken);

        foreach (var result in evaluation.Results)
        {
            _logger.LogInformation(
                "Rule {Rule} evaluated: Passed={Passed}, Message={Message}",
                result.RuleName, result.Passed, result.Message);
        }

        if (!evaluation.Approved)
        {
            _logger.LogInformation(
                "Contract rejected for customer {CustomerId}. Reasons: {Reasons}",
                customer.Id,
                string.Join(", ", evaluation.Results.Where(r => !r.Passed).Select(r => r.Message)));

            return CreateContractResponse.Rejected(evaluation.Results);
        }

        _logger.LogInformation(
            "Contract approved for customer {CustomerId}. Final rate: {Rate}%, Rules evaluated: {Count}",
            customer.Id, evaluation.InterestRate, evaluation.Results.Count);

        var calculator = _calculatorFactory.GetCalculator(request.AmortizationMethod);

        // 3. Crear el Aggregate
        var principal = new Money(request.Amount, request.Currency);
        var interestRate = new InterestRate(evaluation.InterestRate);

        var aggregate = LoanContractAggregate.Create(
            customerId: customer.Id,  // ID local, no el del CRM
            principal: principal,
            rate: interestRate,
            termMonths: request.TermMonths,
            amortizationMethod: request.AmortizationMethod,
            calculator: calculator,
            evaluationMetadata: new Dictionary<string, object>
            {
                ["ExternalCustomerId"] = request.ExternalCustomerId,
                ["CollateralValue"] = request.CollateralValue ?? 0,
                ["EvaluationResults"] = evaluation.Results
            }
        );

        var events = aggregate.UncommittedEvents.ToList();

        await _repository.SaveAsync(aggregate, cancellationToken);

        // 5. Proyectar eventos a Read Models
        try
        {
            await _projectionEngine.ProjectEventsAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to project events for contract {ContractId}. Read models can be rebuilt.",
                aggregate.Id);
        }

        _logger.LogInformation(
            "Contract {ContractId} created for customer {CustomerId} with rate {Rate}%",
            aggregate.Id, customer.Id, evaluation.InterestRate);

        return CreateContractResponse.Approved(
            aggregate.Id, 
            evaluation.InterestRate, 
            evaluation.Results);
    }
}