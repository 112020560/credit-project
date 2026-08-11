using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Entities;

public class CreditProduct
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public ProductLimits Limits { get; private set; }
    public ProductRates Rates { get; private set; }
    public AmortizationMethod DefaultAmortizationMethod { get; private set; }
    public bool RequiresCollateral { get; private set; }
    public ProductStatus Status { get; private set; }
    public decimal? PenaltyRate { get; private set; }
    public decimal? OriginationFeeRate { get; private set; }
    public PaymentWaterfall Waterfall { get; private set; }
    public SocialCapitalConfig? SocialCapitalConfig { get; private set; }

    public CreditProduct(
        Guid id,
        string name,
        ProductLimits limits,
        ProductRates rates,
        AmortizationMethod defaultAmortizationMethod,
        bool requiresCollateral,
        ProductStatus status = ProductStatus.Active,
        decimal? penaltyRate = null,
        decimal? originationFeeRate = null,
        PaymentWaterfall? waterfall = null,
        SocialCapitalConfig? socialCapitalConfig = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        Id = id;
        Name = name;
        Limits = limits ?? throw new ArgumentNullException(nameof(limits));
        Rates = rates ?? throw new ArgumentNullException(nameof(rates));
        DefaultAmortizationMethod = defaultAmortizationMethod;
        RequiresCollateral = requiresCollateral;
        Status = status;
        PenaltyRate = penaltyRate;
        OriginationFeeRate = originationFeeRate;
        Waterfall = waterfall ?? PaymentWaterfall.Default;
        SocialCapitalConfig = socialCapitalConfig;
    }
}
