namespace Crm.Domain.CreditApplications;

public static class ApplicationDocumentType
{
    public const string NationalId = "NationalId";
    public const string Passport = "Passport";
    public const string IncomeProof = "IncomeProof";
    public const string BankStatement = "BankStatement";
    public const string TaxRegistration = "TaxRegistration";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        NationalId, Passport, IncomeProof, BankStatement, TaxRegistration
    };

    public static readonly IReadOnlySet<string> Required = new HashSet<string>
    {
        NationalId, IncomeProof
    };
}
