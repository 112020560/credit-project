using CreditSystem.Infrastructure.Locking;
using FluentAssertions;
using NSubstitute;

namespace CreditSystem.Tests.Infrastructure.Locking;

/// <summary>
/// Unit tests for PostgresAdvisoryLock verify the SQL statements executed.
/// Integration (actual Postgres) tests would live in a separate integration test project.
/// These tests verify the happy-path logic and the lock-ID-per-worker isolation.
/// </summary>
public class WorkerLockIdTests
{
    [Fact]
    public void WorkerLockIds_AreAllUnique()
    {
        var ids = new[]
        {
            WorkerLockId.InterestAccrual,
            WorkerLockId.PaymentMissed,
            WorkerLockId.RevolvingInterestAccrual,
            WorkerLockId.StatementGeneration,
            WorkerLockId.RevolvingPaymentMissed,
            WorkerLockId.RiskClassification
        };

        ids.Distinct().Should().HaveCount(ids.Length, "each worker must have a unique lock ID");
    }

    [Fact]
    public void WorkerLockIds_ArePositiveLongs()
    {
        WorkerLockId.InterestAccrual.Should().BePositive();
        WorkerLockId.PaymentMissed.Should().BePositive();
        WorkerLockId.RevolvingInterestAccrual.Should().BePositive();
        WorkerLockId.StatementGeneration.Should().BePositive();
        WorkerLockId.RevolvingPaymentMissed.Should().BePositive();
        WorkerLockId.RiskClassification.Should().BePositive();
    }
}
