using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Enums;
using Dapper;
using Npgsql;

namespace CreditSystem.Api.EndPoints;

public static class RiskEndpoints
{
    public static void MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loans")
            .WithTags("Risk Classification")
            .WithOpenApi();

        group.MapGet("/risk-summary", GetRiskSummary)
            .WithName("GetRiskSummary")
            .WithSummary("Portfolio risk summary by SUGEF 1-05 category")
            .Produces<RiskSummaryResponse>(StatusCodes.Status200OK);

        group.MapGet("/risk-summary/{category}", GetRiskDetail)
            .WithName("GetRiskCategoryDetail")
            .WithSummary("List loans classified in a specific risk category")
            .Produces<IEnumerable<LoanRiskDetailRow>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{loanId:guid}/risk-category", ManualReclassify)
            .WithName("ManualReclassifyLoan")
            .WithSummary("Manually downgrade a loan's risk category (degradation only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GetRiskSummary(
        IConfiguration configuration,
        CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("CreditDb")!;
        const string sql = @"
            SELECT
                risk_category       AS Category,
                COUNT(*)::INT       AS LoanCount,
                SUM(current_balance) AS TotalBalance,
                SUM(estimated_provision) AS TotalProvision
            FROM rm_loan_summaries
            WHERE status IN ('Active', 'Delinquent', 'Default')
            AND risk_category IS NOT NULL
            GROUP BY risk_category
            ORDER BY risk_category";

        const string totalSql = @"
            SELECT
                COUNT(*)::INT        AS TotalLoans,
                COALESCE(SUM(current_balance), 0)       AS TotalPortfolioBalance,
                COALESCE(SUM(estimated_provision), 0)   AS TotalRequiredProvision
            FROM rm_loan_summaries
            WHERE status IN ('Active', 'Delinquent', 'Default')";

        await using var conn = new NpgsqlConnection(connectionString);

        var rows = (await conn.QueryAsync<RiskCategoryRow>(sql)).ToList();
        var totals = await conn.QuerySingleAsync<(int TotalLoans, decimal TotalPortfolioBalance, decimal TotalRequiredProvision)>(totalSql);

        // Enrich with provision percentage
        foreach (var row in rows)
        {
            if (Enum.TryParse<LoanRiskCategory>(row.Category, out var cat))
                row.ProvisionPercentage = Domain.Services.RiskCategoryTable.GetProvisionRate(cat) * 100m;
        }

        return Results.Ok(new RiskSummaryResponse
        {
            Categories = rows,
            TotalLoans = totals.TotalLoans,
            TotalPortfolioBalance = totals.TotalPortfolioBalance,
            TotalRequiredProvision = totals.TotalRequiredProvision
        });
    }

    private static async Task<IResult> GetRiskDetail(
        string category,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!Enum.TryParse<LoanRiskCategory>(category, ignoreCase: true, out _))
            return Results.BadRequest($"Invalid risk category '{category}'. Valid values: A1, A2, B1, B2, C1, C2, D, E");

        var connectionString = configuration.GetConnectionString("CreditDb")!;
        const string sql = @"
            SELECT
                loan_id             AS LoanId,
                customer_id         AS CustomerId,
                customer_name       AS CustomerName,
                current_balance     AS CurrentBalance,
                estimated_provision AS EstimatedProvision,
                GREATEST(0, EXTRACT(DAY FROM NOW() - next_payment_date)::INT) AS DaysOverdue,
                risk_category       AS RiskCategory
            FROM rm_loan_summaries
            WHERE risk_category = @Category
            AND status IN ('Active', 'Delinquent', 'Default')
            ORDER BY current_balance DESC";

        await using var conn = new NpgsqlConnection(connectionString);
        var results = await conn.QueryAsync<LoanRiskDetailRow>(sql, new { Category = category.ToUpperInvariant() });
        return Results.Ok(results);
    }

    private static async Task<IResult> ManualReclassify(
        Guid loanId,
        ManualReclassifyRequest request,
        ILoanQueryService queryService,
        IRiskClassificationRepository riskRepository,
        CancellationToken ct)
    {
        if (!Enum.TryParse<LoanRiskCategory>(request.Category, ignoreCase: true, out var newCategory))
            return Results.BadRequest($"Invalid risk category '{request.Category}'. Valid values: A1, A2, B1, B2, C1, C2, D, E");

        var loan = await queryService.GetLoanSummaryAsync(loanId, ct);
        if (loan == null)
            return Results.NotFound($"Loan {loanId} not found");

        // Validate degradation-only rule
        if (loan.RiskCategory != null &&
            Enum.TryParse<LoanRiskCategory>(loan.RiskCategory, out var currentCategory) &&
            Domain.Services.RiskCategoryTable.Severity(newCategory) < Domain.Services.RiskCategoryTable.Severity(currentCategory))
        {
            return Results.UnprocessableEntity(
                $"Cannot improve risk category manually. Current: {loan.RiskCategory}, Requested: {request.Category}. Improvement is automatic only.");
        }

        var provision = loan.CurrentBalance * Domain.Services.RiskCategoryTable.GetProvisionRate(newCategory);
        await riskRepository.UpdateRiskCategoryAsync(loanId, newCategory.ToString(), provision, ct);

        return Results.NoContent();
    }
}

// DTOs
public record RiskSummaryResponse
{
    public List<RiskCategoryRow> Categories { get; init; } = new();
    public int TotalLoans { get; init; }
    public decimal TotalPortfolioBalance { get; init; }
    public decimal TotalRequiredProvision { get; init; }
}

public class RiskCategoryRow
{
    public string Category { get; set; } = null!;
    public decimal ProvisionPercentage { get; set; }
    public int LoanCount { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalProvision { get; set; }
}

public record LoanRiskDetailRow
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal EstimatedProvision { get; init; }
    public int DaysOverdue { get; init; }
    public string RiskCategory { get; init; } = null!;
}

public record ManualReclassifyRequest(string Category);
