using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
