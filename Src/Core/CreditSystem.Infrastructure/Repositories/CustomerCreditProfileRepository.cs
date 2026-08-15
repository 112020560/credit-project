using CreditSystem.Domain.Abstractions.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditSystem.Infrastructure.Repositories;

public class CustomerCreditProfileRepository : ICustomerCreditProfileRepository
{
    private readonly string _connectionString;

    public CustomerCreditProfileRepository(IConfiguration configuration)
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
            INSERT INTO customer_credit_profiles
                (id, external_id, full_name, email, phone, document_type, document_number,
                 credit_score, monthly_income, monthly_debt, created_at, updated_at)
            VALUES
                (@Id, @ExternalId, @FullName, @Email, @Phone, @DocumentType, @DocumentNumber,
                 @CreditScore, @MonthlyIncome, @MonthlyDebt, @CreatedAt, @UpdatedAt)
            ON CONFLICT (external_id) DO UPDATE SET
                full_name       = COALESCE(@FullName,       customer_credit_profiles.full_name),
                email           = COALESCE(@Email,          customer_credit_profiles.email),
                phone           = COALESCE(@Phone,          customer_credit_profiles.phone),
                document_type   = COALESCE(@DocumentType,   customer_credit_profiles.document_type),
                document_number = COALESCE(@DocumentNumber, customer_credit_profiles.document_number),
                credit_score    = COALESCE(@CreditScore,    customer_credit_profiles.credit_score),
                monthly_income  = COALESCE(@MonthlyIncome,  customer_credit_profiles.monthly_income),
                monthly_debt    = COALESCE(@MonthlyDebt,    customer_credit_profiles.monthly_debt),
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
