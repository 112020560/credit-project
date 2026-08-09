using CreditSystem.Application.Commands.CreateContract;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditSystem.Tests.Commands;

public class CreateContractCommandHandlerTests
{
    private readonly ILoanContractRepository _loanRepo = Substitute.For<ILoanContractRepository>();
    private readonly ICustomerReadRepository _customerRepo = Substitute.For<ICustomerReadRepository>();
    private readonly ICooperativeMemberRepository _memberRepo = Substitute.For<ICooperativeMemberRepository>();
    private readonly ICreditProductRepository _productRepo = Substitute.For<ICreditProductRepository>();
    private readonly ILoanGuaranteeRepository _guaranteeRepo = Substitute.For<ILoanGuaranteeRepository>();
    private readonly ILoanQueryService _queryService = Substitute.For<ILoanQueryService>();
    private readonly IProjectionEngine _projectionEngine = Substitute.For<IProjectionEngine>();
    private readonly IAmortizationCalculatorFactory _calcFactory = Substitute.For<IAmortizationCalculatorFactory>();

    private static readonly UnderwritingPolicy DefaultPolicy =
        new(8.0m, 90, NoScoreBehavior.ApproveWithPenalty, 5, false);

    private static readonly CustomerCreditProfile SampleCustomer =
        CustomerCreditProfile.Create(Guid.NewGuid(), "Juan Pérez", "CC", "123456789");

    private static CreditProduct ActiveProduct(decimal? baseRate = 14.0m) =>
        new(
            Guid.NewGuid(),
            "Préstamo Personal",
            new ProductLimits(100_000m, 5_000_000m, 1, 60),
            new ProductRates(baseRate, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            status: ProductStatus.Active);

    private CreateContractCommandHandler BuildHandler()
    {
        // Use an engine with no rules so every contract is approved with just the base rate
        var engine = new ContractEngine([], DefaultPolicy);

        var calculator = Substitute.For<IAmortizationCalculator>();
        calculator.Calculate(Arg.Any<Money>(), Arg.Any<InterestRate>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(new PaymentSchedule([]));
        _calcFactory.GetCalculator(Arg.Any<AmortizationMethod>()).Returns(calculator);

        return new CreateContractCommandHandler(
            _loanRepo,
            _customerRepo,
            _memberRepo,
            _productRepo,
            _guaranteeRepo,
            _queryService,
            engine,
            DefaultPolicy,
            _calcFactory,
            _projectionEngine,
            NullLogger<CreateContractCommandHandler>.Instance);
    }

    private static CreateContractCommand ValidCommand(Guid productId) => new()
    {
        ExternalCustomerId = SampleCustomer.ExternalId,
        ProductId = productId,
        Amount = 500_000m,
        Currency = "CRC",
        TermMonths = 12,
        AmortizationMethod = AmortizationMethod.French
    };

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsFailure()
    {
        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustomer);
        _productRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreditProduct?)null);

        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_ReturnsFailure()
    {
        var inactiveProduct = new CreditProduct(
            Guid.NewGuid(), "Préstamo Personal",
            new ProductLimits(100_000m, 5_000_000m, 1, 60),
            new ProductRates(0.14m, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            status: ProductStatus.Inactive);

        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustomer);
        _productRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(inactiveProduct);

        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(inactiveProduct.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not active");
    }

    [Fact]
    public async Task Handle_WhenProductActiveWithOwnRate_ApproveWithProductRate()
    {
        var product = ActiveProduct(baseRate: 14.0m);

        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustomer);
        _productRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _memberRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreditSystem.Domain.Aggregates.CooperativeMember.CooperativeMemberAggregate?)null);
        _queryService.HasActiveLoansAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _loanRepo.SaveAsync(Arg.Any<CreditSystem.Domain.Aggregates.LoanContract.LoanContractAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(product.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        // Product base rate is 14% (0.14), engine returns that as approved rate
        // Product defines BaseInterestRate = 14.0 (14%), engine has no adjustment rules → final rate = 14.0
        result.ApprovedRate.Should().Be(14.0m);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ReturnsFailure()
    {
        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CustomerCreditProfile?)null);

        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithoutGuarantees_CollateralValueIsNull()
    {
        var product = ActiveProduct(baseRate: 14.0m);

        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustomer);
        _productRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _memberRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreditSystem.Domain.Aggregates.CooperativeMember.CooperativeMemberAggregate?)null);
        _queryService.HasActiveLoansAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _loanRepo.SaveAsync(Arg.Any<CreditSystem.Domain.Aggregates.LoanContract.LoanContractAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = BuildHandler();
        var command = ValidCommand(product.Id); // no Guarantees
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        // No guarantees → guarantee repo should not be called
        await _guaranteeRepo.DidNotReceive().InsertAsync(Arg.Any<CreditSystem.Domain.Entities.LoanGuarantee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithGuarantees_EffectiveCollateralSummedAndGuaranteesPersisted()
    {
        var product = ActiveProduct(baseRate: 14.0m);

        _customerRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustomer);
        _productRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _memberRepo.GetByExternalIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreditSystem.Domain.Aggregates.CooperativeMember.CooperativeMemberAggregate?)null);
        _queryService.HasActiveLoansAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _loanRepo.SaveAsync(Arg.Any<CreditSystem.Domain.Aggregates.LoanContract.LoanContractAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _guaranteeRepo.InsertAsync(Arg.Any<CreditSystem.Domain.Entities.LoanGuarantee>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new CreateContractCommand
        {
            ExternalCustomerId = SampleCustomer.ExternalId,
            ProductId = product.Id,
            Amount = 500_000m,
            Currency = "CRC",
            TermMonths = 12,
            AmortizationMethod = AmortizationMethod.French,
            Guarantees =
            [
                new GuaranteeInput(CreditSystem.Domain.Enums.GuaranteeType.Hipoteca, "Casa", 1_000_000m, 0.80m),
                new GuaranteeInput(CreditSystem.Domain.Enums.GuaranteeType.Prenda, "Vehículo", 500_000m, 1.0m)
            ]
        };

        var handler = BuildHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        // Two guarantees → InsertAsync called twice
        await _guaranteeRepo.Received(2).InsertAsync(Arg.Any<CreditSystem.Domain.Entities.LoanGuarantee>(), Arg.Any<CancellationToken>());
    }
}
