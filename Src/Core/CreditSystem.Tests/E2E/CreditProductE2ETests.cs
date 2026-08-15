using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Dapper;
using FluentAssertions;
using Npgsql;

namespace CreditSystem.Tests.E2E;

/// <summary>
/// E2E tests: CreditProduct JSONB serialization roundtrip.
/// Verifies that waterfall_config and social_capital_config survive an INSERT → SELECT cycle.
/// </summary>
public class CreditProductE2ETests : IAsyncLifetime
{
    private readonly E2EFixture _fx = new();
    private readonly List<Guid> _productIds = new();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _fx.CleanupAsync(productIds: _productIds);

    [Fact]
    public async Task CreateProduct_WithCustomWaterfall_RoundtripsCorrectlyFromDatabase()
    {
        // Arrange
        var waterfall = new PaymentWaterfall([
            new PaymentWaterfallStep(1, PaymentComponent.Fees),
            new PaymentWaterfallStep(2, PaymentComponent.PenaltyInterest),
            new PaymentWaterfallStep(3, PaymentComponent.SocialCapital),
            new PaymentWaterfallStep(4, PaymentComponent.RegularInterest),
            new PaymentWaterfallStep(5, PaymentComponent.Principal)
        ]);

        var product = new CreditProduct(
            Guid.NewGuid(),
            $"E2E-Test-Waterfall-{Guid.NewGuid():N}",
            new ProductLimits(100_000m, 5_000_000m, 6, 60),
            new ProductRates(14.5m, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            waterfall: waterfall);

        _productIds.Add(product.Id);
        var repo = _fx.BuildProductRepository();

        // Act
        await repo.InsertAsync(product);
        var loaded = await repo.GetByIdAsync(product.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Waterfall.Steps.Should().HaveCount(5);
        loaded.Waterfall.Steps.Select(s => s.Component).Should().ContainInOrder(
            PaymentComponent.Fees,
            PaymentComponent.PenaltyInterest,
            PaymentComponent.SocialCapital,
            PaymentComponent.RegularInterest,
            PaymentComponent.Principal);
        loaded.SocialCapitalConfig.Should().BeNull();
    }

    [Fact]
    public async Task CreateProduct_WithSocialCapitalConfig_RoundtripsCorrectlyFromDatabase()
    {
        // Arrange
        var socialConfig = new SocialCapitalConfig
        {
            CalculationType = SocialCapitalCalculationType.FixedAmount,
            Value = 750m,
            CollectionMode = SocialCapitalCollectionMode.IncludedInPayment
        };

        var product = new CreditProduct(
            Guid.NewGuid(),
            $"E2E-Test-SocialCapital-{Guid.NewGuid():N}",
            new ProductLimits(50_000m, 2_000_000m, 3, 36),
            new ProductRates(12m, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            socialCapitalConfig: socialConfig);

        _productIds.Add(product.Id);
        var repo = _fx.BuildProductRepository();

        // Act
        await repo.InsertAsync(product);
        var loaded = await repo.GetByIdAsync(product.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.SocialCapitalConfig.Should().NotBeNull();
        loaded.SocialCapitalConfig!.CalculationType.Should().Be(SocialCapitalCalculationType.FixedAmount);
        loaded.SocialCapitalConfig.Value.Should().Be(750m);
        loaded.SocialCapitalConfig.CollectionMode.Should().Be(SocialCapitalCollectionMode.IncludedInPayment);

        // Default waterfall should be used when none specified
        loaded.Waterfall.Steps.Should().HaveCount(4);
        loaded.Waterfall.Steps[0].Component.Should().Be(PaymentComponent.Fees);
    }

    [Fact]
    public async Task CreateProduct_WithPercentageOfPayment_SocialCapital_RoundtripsCorrectly()
    {
        // Arrange
        var socialConfig = new SocialCapitalConfig
        {
            CalculationType = SocialCapitalCalculationType.PercentageOfPayment,
            Value = 2.5m,  // 2.5%
            CollectionMode = SocialCapitalCollectionMode.SeparateCollection
        };

        var product = new CreditProduct(
            Guid.NewGuid(),
            $"E2E-Test-Percentage-{Guid.NewGuid():N}",
            new ProductLimits(100_000m, 10_000_000m, 12, 120),
            new ProductRates(15m, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            socialCapitalConfig: socialConfig);

        _productIds.Add(product.Id);
        var repo = _fx.BuildProductRepository();

        // Act
        await repo.InsertAsync(product);
        var loaded = await repo.GetByIdAsync(product.Id);

        // Assert
        loaded!.SocialCapitalConfig!.CalculationType.Should().Be(SocialCapitalCalculationType.PercentageOfPayment);
        loaded.SocialCapitalConfig.Value.Should().Be(2.5m);
        loaded.SocialCapitalConfig.CollectionMode.Should().Be(SocialCapitalCollectionMode.SeparateCollection);
    }

    [Fact]
    public async Task GetAllActiveProducts_ReturnsProductsWithWaterfallAndSocialCapital()
    {
        // Arrange: insert a product with both waterfall and social capital
        var waterfall = new PaymentWaterfall([
            new PaymentWaterfallStep(1, PaymentComponent.Fees),
            new PaymentWaterfallStep(2, PaymentComponent.RegularInterest),
            new PaymentWaterfallStep(3, PaymentComponent.Principal)
        ]);
        var socialConfig = new SocialCapitalConfig
        {
            CalculationType = SocialCapitalCalculationType.FixedAmount,
            Value = 500m,
            CollectionMode = SocialCapitalCollectionMode.IncludedInPayment
        };

        var product = new CreditProduct(
            Guid.NewGuid(),
            $"E2E-Test-GetAll-{Guid.NewGuid():N}",
            new ProductLimits(100_000m, 3_000_000m, 6, 48),
            new ProductRates(13m, null),
            AmortizationMethod.French,
            requiresCollateral: false,
            waterfall: waterfall,
            socialCapitalConfig: socialConfig);

        _productIds.Add(product.Id);
        var repo = _fx.BuildProductRepository();
        await repo.InsertAsync(product);

        // Act
        var all = (await repo.GetAllActiveAsync()).ToList();
        var loaded = all.FirstOrDefault(p => p.Id == product.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Waterfall.Steps.Should().HaveCount(3);
        loaded.SocialCapitalConfig!.Value.Should().Be(500m);
    }
}
