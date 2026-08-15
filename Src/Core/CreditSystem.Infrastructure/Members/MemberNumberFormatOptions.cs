namespace CreditSystem.Infrastructure.Members;

public class MemberNumberFormatOptions
{
    public const string SectionName = "MemberNumberFormat";

    public string Prefix { get; set; } = "CM";
    public int SequentialDigits { get; set; } = 5;
}
