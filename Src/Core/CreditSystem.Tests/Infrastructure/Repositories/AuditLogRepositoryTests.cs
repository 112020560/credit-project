using CreditSystem.Domain.Models;
using CreditSystem.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditSystem.Tests.Infrastructure.Repositories;

public class AuditLogRepositoryTests
{
    [Fact]
    public async Task LogAsync_DatabaseException_IsAbsorbedWithoutThrowing()
    {
        // Use an invalid connection string to force a DB exception
        var repo = new AuditLogRepository("Host=invalid_host_xyz;Database=none", NullLogger<AuditLogRepository>.Instance);

        var entry = new AuditEntry
        {
            Action = "payment.applied",
            EntityType = "LoanContract",
            EntityId = Guid.NewGuid(),
            UserId = "usr-123",
            Details = new { amount = 1000m }
        };

        // Should NOT throw — exception must be absorbed internally
        var act = async () => await repo.LogAsync(entry);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsync_NullDetails_DoesNotThrow()
    {
        var repo = new AuditLogRepository("Host=invalid_host_xyz;Database=none", NullLogger<AuditLogRepository>.Instance);

        var entry = new AuditEntry
        {
            Action = "contract.created",
            EntityType = "LoanContract",
            EntityId = Guid.NewGuid(),
            Details = null
        };

        var act = async () => await repo.LogAsync(entry);
        await act.Should().NotThrowAsync();
    }
}
