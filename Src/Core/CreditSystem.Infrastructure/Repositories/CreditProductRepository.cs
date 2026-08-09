using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class CreditProductRepository : ICreditProductRepository
{
    private readonly string _connectionString;

    public CreditProductRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CreditProduct?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, name, min_amount, max_amount, min_term_months, max_term_months,
                   base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status,
                   penalty_rate, origination_fee_rate
            FROM credit_products
            WHERE id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Id = id });

        return row == null ? null : MapToEntity(row);
    }

    public async Task<IEnumerable<CreditProduct>> GetAllActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, name, min_amount, max_amount, min_term_months, max_term_months,
                   base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status,
                   penalty_rate, origination_fee_rate
            FROM credit_products
            WHERE status = 'Active'
            ORDER BY name
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(sql);
        return rows.Select<dynamic, CreditProduct>(r => MapToEntity(r));
    }

    public async Task<IEnumerable<CreditProduct>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, name, min_amount, max_amount, min_term_months, max_term_months,
                   base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status,
                   penalty_rate, origination_fee_rate
            FROM credit_products
            ORDER BY name
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(sql);
        return rows.Select<dynamic, CreditProduct>(r => MapToEntity(r));
    }

    public async Task InsertAsync(CreditProduct product, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO credit_products
                (id, name, min_amount, max_amount, min_term_months, max_term_months,
                 base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status,
                 penalty_rate, origination_fee_rate, created_at)
            VALUES
                (@Id, @Name, @MinAmount, @MaxAmount, @MinTermMonths, @MaxTermMonths,
                 @BaseInterestRate, @MaxLtv, @DefaultAmortizationMethod, @RequiresCollateral, @Status,
                 @PenaltyRate, @OriginationFeeRate, NOW())
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            product.Id,
            product.Name,
            MinAmount = product.Limits.MinAmount,
            MaxAmount = product.Limits.MaxAmount,
            MinTermMonths = product.Limits.MinTermMonths,
            MaxTermMonths = product.Limits.MaxTermMonths,
            BaseInterestRate = product.Rates.BaseInterestRate,
            MaxLtv = product.Rates.MaxLtv,
            DefaultAmortizationMethod = product.DefaultAmortizationMethod.ToString(),
            product.RequiresCollateral,
            Status = product.Status.ToString(),
            product.PenaltyRate,
            product.OriginationFeeRate
        }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(Guid id, ProductStatus status, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE credit_products
            SET status = @Status
            WHERE id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            Status = status.ToString()
        }, cancellationToken: ct));
    }

    private static CreditProduct MapToEntity(dynamic row)
    {
        var limits = new ProductLimits(
            (decimal)row.min_amount,
            (decimal)row.max_amount,
            (int)row.min_term_months,
            (int)row.max_term_months);

        var rates = new ProductRates(
            (decimal?)row.base_interest_rate,
            (decimal?)row.max_ltv);

        var method = Enum.TryParse<AmortizationMethod>((string)row.default_amortization_method, out var m)
            ? m
            : AmortizationMethod.French;

        var status = Enum.TryParse<ProductStatus>((string)row.status, out var s)
            ? s
            : ProductStatus.Active;

        return new CreditProduct(
            (Guid)row.id,
            (string)row.name,
            limits,
            rates,
            method,
            (bool)row.requires_collateral,
            status,
            (decimal?)row.penalty_rate,
            (decimal?)row.origination_fee_rate);
    }
}
