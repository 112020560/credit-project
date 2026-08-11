using CreditSystem.Application.Commands.EnrollMember;
using CreditSystem.Domain.Abstractions.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members")
            .WithTags("Cooperative Members")
            .WithOpenApi();

        group.MapGet("/{externalId:guid}", GetMemberProfile)
            .WithName("GetMemberProfile")
            .WithSummary("Get cooperative member profile")
            .Produces<MemberProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{externalId:guid}/shares", GetMemberShares)
            .WithName("GetMemberShares")
            .WithSummary("Get cooperative member shares (aportaciones)")
            .Produces<MemberSharesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{externalId:guid}/social-capital", GetSocialCapitalBalance)
            .WithName("GetSocialCapitalBalance")
            .WithSummary("Get accumulated social capital balance from loan payments")
            .Produces<SocialCapitalBalanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/enroll", EnrollMember)
            .WithName("EnrollMember")
            .WithSummary("Enroll an existing customer as a cooperative member")
            .Produces<EnrollMemberResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> EnrollMember(
        [FromBody] EnrollMemberRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new EnrollMemberCommand
        {
            ExternalCustomerId = request.ExternalCustomerId,
            JoinedAt = request.JoinedAt,
            InitialSharesAmount = request.InitialSharesAmount,
            SharesCurrency = request.SharesCurrency ?? "CRC"
        };

        var response = await mediator.Send(command, cancellationToken);

        if (!response.Success)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Enrollment Failed",
                Detail = response.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return Results.Created($"/api/members/{response.ExternalCustomerId}", response);
    }

    private static async Task<IResult> GetMemberProfile(
        Guid externalId,
        [FromServices] ICooperativeMemberRepository repository,
        CancellationToken cancellationToken)
    {
        var member = await repository.GetByExternalIdAsync(externalId, cancellationToken);
        if (member == null) return Results.NotFound();

        return Results.Ok(new MemberProfileResponse(
            ExternalId: member.State.ExternalId,
            MemberNumber: member.State.MemberNumber,
            Status: member.State.Status.ToString(),
            JoinedAt: member.State.JoinedAt,
            TotalSharesAmount: member.State.Shares.TotalAmount.Amount,
            SharesCurrency: member.State.Shares.TotalAmount.Currency,
            NumberOfContributions: member.State.Shares.NumberOfContributions,
            LastContributionDate: member.State.Shares.LastContributionDate));
    }

    private static async Task<IResult> GetSocialCapitalBalance(
        Guid externalId,
        [FromServices] ICooperativeMemberRepository repository,
        CancellationToken cancellationToken)
    {
        var balance = await repository.GetSocialCapitalBalanceAsync(externalId, cancellationToken);
        if (balance == null) return Results.NotFound();

        return Results.Ok(new SocialCapitalBalanceResponse(externalId, balance.Value));
    }

    private static async Task<IResult> GetMemberShares(
        Guid externalId,
        [FromServices] ICooperativeMemberRepository repository,
        CancellationToken cancellationToken)
    {
        var member = await repository.GetByExternalIdAsync(externalId, cancellationToken);
        if (member == null) return Results.NotFound();

        return Results.Ok(new MemberSharesResponse(
            ExternalId: member.State.ExternalId,
            TotalSharesAmount: member.State.Shares.TotalAmount.Amount,
            SharesCurrency: member.State.Shares.TotalAmount.Currency,
            NumberOfContributions: member.State.Shares.NumberOfContributions,
            LastContributionDate: member.State.Shares.LastContributionDate));
    }
}

public record MemberProfileResponse(
    Guid ExternalId,
    string MemberNumber,
    string Status,
    DateTime JoinedAt,
    decimal TotalSharesAmount,
    string SharesCurrency,
    int NumberOfContributions,
    DateTime? LastContributionDate);

public record MemberSharesResponse(
    Guid ExternalId,
    decimal TotalSharesAmount,
    string SharesCurrency,
    int NumberOfContributions,
    DateTime? LastContributionDate);

public record SocialCapitalBalanceResponse(
    Guid ExternalId,
    decimal SocialCapitalBalance);

public record EnrollMemberRequest(
    Guid ExternalCustomerId,
    DateTime JoinedAt,
    decimal InitialSharesAmount,
    string? SharesCurrency = "CRC");
