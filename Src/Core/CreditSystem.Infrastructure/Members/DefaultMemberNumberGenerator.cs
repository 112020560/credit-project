using CreditSystem.Domain.Abstractions;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CreditSystem.Infrastructure.Members;

public class DefaultMemberNumberGenerator : IMemberNumberGenerator
{
    private readonly string _connectionString;
    private readonly MemberNumberFormatOptions _options;

    public DefaultMemberNumberGenerator(string connectionString, IOptions<MemberNumberFormatOptions> options)
    {
        _connectionString = connectionString;
        _options = options.Value;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        const string sql = """
            INSERT INTO member_number_sequences (year, last_value)
            VALUES (@Year, 1)
            ON CONFLICT (year) DO UPDATE
                SET last_value = member_number_sequences.last_value + 1
            RETURNING last_value
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var sequence = await conn.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { Year = year }, cancellationToken: ct));

        var format = $"D{_options.SequentialDigits}";
        return $"{_options.Prefix}-{year}-{sequence.ToString(format)}";
    }
}
