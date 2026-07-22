using CreditSystem.Domain.Abstractions.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class CustomerReferenceRepository : ICustomerReferenceRepository
{
    private readonly string _connectionString;

    public CustomerReferenceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CreditDb")!;
    }

    public async Task UpsertAsync(
        Guid externalId,
        string? fullName,
        string? email,
        string? phone,
        string? documentType,
        string? documentNumber,
        int? creditScore,
        decimal? monthlyIncome,
        decimal? monthlyDebt,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO customer_references
                (id, external_id, full_name, email, phone, document_type, document_number,
                 credit_score, monthly_income, monthly_debt, created_at, updated_at)
            VALUES
                (@Id, @ExternalId, @FullName, @Email, @Phone, @DocumentType, @DocumentNumber,
                 @CreditScore, @MonthlyIncome, @MonthlyDebt, @CreatedAt, @UpdatedAt)
            ON CONFLICT (external_id) DO UPDATE SET
                full_name       = COALESCE(@FullName,       customer_references.full_name),
                email           = COALESCE(@Email,          customer_references.email),
                phone           = COALESCE(@Phone,          customer_references.phone),
                document_type   = COALESCE(@DocumentType,   customer_references.document_type),
                document_number = COALESCE(@DocumentNumber, customer_references.document_number),
                credit_score    = COALESCE(@CreditScore,    customer_references.credit_score),
                monthly_income  = COALESCE(@MonthlyIncome,  customer_references.monthly_income),
                monthly_debt    = COALESCE(@MonthlyDebt,    customer_references.monthly_debt),
                updated_at      = @UpdatedAt";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            CreditScore = creditScore,
            MonthlyIncome = monthlyIncome,
            MonthlyDebt = monthlyDebt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: ct));
    }
}
