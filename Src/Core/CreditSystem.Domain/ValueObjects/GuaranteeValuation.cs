namespace CreditSystem.Domain.ValueObjects;

public record GuaranteeValuation
{
    public decimal AppraisalValue { get; init; }
    public decimal CoverageRate { get; init; }
    public decimal EffectiveCoverage => AppraisalValue * CoverageRate;

    public GuaranteeValuation(decimal appraisalValue, decimal coverageRate)
    {
        if (appraisalValue <= 0)
            throw new ArgumentException("Appraisal value must be greater than zero.", nameof(appraisalValue));
        if (coverageRate <= 0 || coverageRate > 1)
            throw new ArgumentException("Coverage rate must be between 0 (exclusive) and 1 (inclusive).", nameof(coverageRate));

        AppraisalValue = appraisalValue;
        CoverageRate = coverageRate;
    }
}
