namespace CreditSystem.Application.Commands.EnrollMember;

public record EnrollMemberResponse
{
    public bool Success { get; init; }
    public Guid? MemberId { get; init; }
    public string? MemberNumber { get; init; }
    public Guid? ExternalCustomerId { get; init; }
    public DateTime? JoinedAt { get; init; }
    public string? Error { get; init; }

    public static EnrollMemberResponse Enrolled(Guid memberId, string memberNumber, Guid externalCustomerId, DateTime joinedAt) => new()
    {
        Success = true,
        MemberId = memberId,
        MemberNumber = memberNumber,
        ExternalCustomerId = externalCustomerId,
        JoinedAt = joinedAt
    };

    public static EnrollMemberResponse Failed(string error) => new()
    {
        Success = false,
        Error = error
    };
}
