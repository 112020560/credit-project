namespace SharedKernel.Contracts.Crm.Members;

public interface MemberSynced
{
    Guid ExternalId { get; }
    string MemberNumber { get; }
    string Status { get; }
    DateTime JoinedAt { get; }
    decimal TotalSharesAmount { get; }
    string SharesCurrency { get; }
    int NumberOfContributions { get; }
    DateTime? LastContributionDate { get; }
}
