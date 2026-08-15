using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Dapper;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class LoanGuaranteeRepository : ILoanGuaranteeRepository
{
    private readonly string _connectionString;

    public LoanGuaranteeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<LoanGuarantee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, loan_contract_id, type, description, appraisal_value, coverage_rate, status, expiration_date
            FROM loan_guarantees
            WHERE id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Id = id });
        return row == null ? null : MapToEntity(row);
    }

    public async Task<IEnumerable<LoanGuarantee>> GetByContractAsync(Guid loanContractId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, loan_contract_id, type, description, appraisal_value, coverage_rate, status, expiration_date
            FROM loan_guarantees
            WHERE loan_contract_id = @LoanContractId
            ORDER BY created_at
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(sql, new { LoanContractId = loanContractId });
        return rows.Select<dynamic, LoanGuarantee>(r => MapToEntity(r));
    }

    public async Task InsertAsync(LoanGuarantee guarantee, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO loan_guarantees
                (id, loan_contract_id, type, description, appraisal_value, coverage_rate, status, expiration_date, created_at)
            VALUES
                (@Id, @LoanContractId, @Type, @Description, @AppraisalValue, @CoverageRate, @Status, @ExpirationDate, NOW())
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            guarantee.Id,
            guarantee.LoanContractId,
            Type = guarantee.Type.ToString(),
            guarantee.Description,
            AppraisalValue = guarantee.Valuation.AppraisalValue,
            CoverageRate = guarantee.Valuation.CoverageRate,
            Status = guarantee.Status.ToString(),
            ExpirationDate = guarantee.ExpirationDate.HasValue ? (DateTime?)guarantee.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue) : null
        }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(Guid id, GuaranteeStatus status, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE loan_guarantees
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

    private static LoanGuarantee MapToEntity(dynamic row)
    {
        var valuation = new GuaranteeValuation(
            (decimal)row.appraisal_value,
            (decimal)row.coverage_rate);

        var type = Enum.TryParse<GuaranteeType>((string)row.type, out var t)
            ? t : GuaranteeType.Hipoteca;

        var status = Enum.TryParse<GuaranteeStatus>((string)row.status, out var s)
            ? s : GuaranteeStatus.Vigente;

        DateOnly? expirationDate = row.expiration_date != null
            ? DateOnly.FromDateTime((DateTime)row.expiration_date)
            : null;

        return new LoanGuarantee(
            (Guid)row.id,
            (Guid)row.loan_contract_id,
            type,
            (string)row.description,
            valuation,
            status,
            expirationDate);
    }
}
