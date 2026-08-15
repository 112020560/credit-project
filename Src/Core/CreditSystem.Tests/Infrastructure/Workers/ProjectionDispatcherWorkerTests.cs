using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Infrastructure.EventStore;
using CreditSystem.Infrastructure.Projectors;
using CreditSystem.Infrastructure.Workers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CreditSystem.Tests.Infrastructure.Workers;

public class ProjectionDispatcherWorkerTests
{
    private const long Seq = 42L;

    private static StoredEventRecord FakeEvent(long sequence = Seq) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "SomeEvent", "{}", sequence, DateTime.UtcNow);

    private static IOptions<ProjectionDispatcherOptions> DefaultOptions =>
        Options.Create(new ProjectionDispatcherOptions { IntervalSeconds = 5, BatchSize = 100 });

    private static (
        IServiceScopeFactory ScopeFactory,
        IEventStore EventStore,
        IProjectionCheckpointRepository Checkpoints,
        IProjectionFailureRepository Failures,
        IProjection Projector,
        IEventSerializer Serializer
    ) BuildMocks(StoredEventRecord[]? events = null)
    {
        var projector = Substitute.For<IProjection>();
        projector.ProjectionName.Returns("TestProjector");

        var checkpoints = Substitute.For<IProjectionCheckpointRepository>();
        checkpoints.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(0L);

        var failures = Substitute.For<IProjectionFailureRepository>();

        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventsSinceSequenceAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(events ?? [FakeEvent()]);

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IEventStore)).Returns(eventStore);
        sp.GetService(typeof(IProjectionCheckpointRepository)).Returns(checkpoints);
        sp.GetService(typeof(IProjectionFailureRepository)).Returns(failures);
        sp.GetService(typeof(IEnumerable<IProjection>)).Returns(new[] { projector });

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(sp);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serializer = Substitute.For<IEventSerializer>();
        serializer.Deserialize(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Substitute.For<CreditSystem.Domain.Abstractions.Events.IDomainEvent>());

        return (scopeFactory, eventStore, checkpoints, failures, projector, serializer);
    }

    [Fact]
    public async Task SuccessfulEvent_AdvancesCheckpoint()
    {
        var m = BuildMocks();
        var worker = new ProjectionDispatcherWorker(m.ScopeFactory, m.Serializer, DefaultOptions,
            NullLogger<ProjectionDispatcherWorker>.Instance);

        // Invoke one tick via the internal method through a derived test class
        await worker.InvokeTickAsync(CancellationToken.None);

        await m.Checkpoints.Received(1).SaveCheckpointAsync("TestProjector", Seq, Arg.Any<CancellationToken>());
        await m.Failures.DidNotReceive().RecordFailureAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransientFailure_SucceedsOnRetry_DoesNotRecordFailure()
    {
        var m = BuildMocks();

        // First call throws, second call succeeds
        m.Projector.ProjectAsync(Arg.Any<CreditSystem.Domain.Abstractions.Events.IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(
                x => throw new Exception("transient"),
                x => Task.CompletedTask);

        var worker = new ProjectionDispatcherWorker(m.ScopeFactory, m.Serializer, DefaultOptions,
            NullLogger<ProjectionDispatcherWorker>.Instance);

        await worker.InvokeTickAsync(CancellationToken.None);

        await m.Failures.DidNotReceive().RecordFailureAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await m.Checkpoints.Received(1).SaveCheckpointAsync("TestProjector", Seq, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PermanentFailure_RecordsFailureAndAdvancesCheckpoint()
    {
        var m = BuildMocks();

        m.Projector.ProjectAsync(Arg.Any<CreditSystem.Domain.Abstractions.Events.IDomainEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("permanent error"));

        var worker = new ProjectionDispatcherWorker(m.ScopeFactory, m.Serializer, DefaultOptions,
            NullLogger<ProjectionDispatcherWorker>.Instance);

        await worker.InvokeTickAsync(CancellationToken.None);

        await m.Failures.Received(1).RecordFailureAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), "TestProjector",
            Arg.Any<string>(), 4, Arg.Any<CancellationToken>());
        // Checkpoint still advances
        await m.Checkpoints.Received(1).SaveCheckpointAsync("TestProjector", Seq, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailingProjector_DoesNotBlockNextEvent()
    {
        var event1 = FakeEvent(sequence: 10L);
        var event2 = FakeEvent(sequence: 20L);
        var m = BuildMocks([event1, event2]);

        // Fail on first event, succeed on second
        m.Projector.ProjectAsync(Arg.Any<CreditSystem.Domain.Abstractions.Events.IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(
                x => throw new Exception("fail first"),
                x => throw new Exception("fail first retry 1"),
                x => throw new Exception("fail first retry 2"),
                x => throw new Exception("fail first retry 3"),
                x => Task.CompletedTask);  // second event succeeds first try

        var worker = new ProjectionDispatcherWorker(m.ScopeFactory, m.Serializer, DefaultOptions,
            NullLogger<ProjectionDispatcherWorker>.Instance);

        await worker.InvokeTickAsync(CancellationToken.None);

        // Both events should have their checkpoints advanced
        await m.Checkpoints.Received(1).SaveCheckpointAsync("TestProjector", 10L, Arg.Any<CancellationToken>());
        await m.Checkpoints.Received(1).SaveCheckpointAsync("TestProjector", 20L, Arg.Any<CancellationToken>());
    }
}

/// <summary>Extension to expose internal tick method for testing without running the full timer loop.</summary>
internal static class ProjectionDispatcherWorkerTestExtensions
{
    public static Task InvokeTickAsync(this ProjectionDispatcherWorker worker, CancellationToken ct)
    {
        var method = typeof(ProjectionDispatcherWorker)
            .GetMethod("TickAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (Task)method.Invoke(worker, [ct])!;
    }
}
