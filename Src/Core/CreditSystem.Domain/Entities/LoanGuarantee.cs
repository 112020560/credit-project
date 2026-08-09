using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Entities;

public class LoanGuarantee
{
    public Guid Id { get; private set; }
    public Guid LoanContractId { get; private set; }
    public GuaranteeType Type { get; private set; }
    public string Description { get; private set; }
    public GuaranteeValuation Valuation { get; private set; }
    public GuaranteeStatus Status { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }

    public LoanGuarantee(
        Guid id,
        Guid loanContractId,
        GuaranteeType type,
        string description,
        GuaranteeValuation valuation,
        GuaranteeStatus status = GuaranteeStatus.Vigente,
        DateOnly? expirationDate = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (valuation == null)
            throw new ArgumentNullException(nameof(valuation));

        Id = id;
        LoanContractId = loanContractId;
        Type = type;
        Description = description;
        Valuation = valuation;
        Status = status;
        ExpirationDate = expirationDate;
    }
}
