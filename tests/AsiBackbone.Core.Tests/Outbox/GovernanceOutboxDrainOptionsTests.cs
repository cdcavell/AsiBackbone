using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.Storage.InMemory.Outbox;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

public sealed class GovernanceOutboxDrainOptionsTests
{
    private static readonly DateTimeOffset DrainUtc = new(2026, 6, 29, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DrainAsyncUsesConfiguredRetryDelayForEmitterExceptions()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry queuedEntry = await outboxStore.EnqueueAsync(
            CreateEnvelope("exception"),
            TestContext.Current.CancellationToken);
        var options = Options.Create(new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = TimeSpan.FromMinutes(7),
            DeferredDelay = TimeSpan.FromMinutes(13)
        });
        var drain = new AsiBackboneGovernanceOutboxDrain(
            outboxStore,
            new ThrowingEmitter(new InvalidOperationException("provider unavailable")),
            outboxOptions: options);

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));
        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            queuedEntry.OutboxEntryId,
            TestContext.Current.CancellationToken);

        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, drained.Status);
        Assert.NotNull(storedEntry);
        Assert.Equal(DrainUtc.AddMinutes(7), drained.NextRetryUtc);
        Assert.Equal(DrainUtc.AddMinutes(7), storedEntry.NextRetryUtc);
    }

    [Fact]
    public async Task DrainAsyncUsesConfiguredDeferredDelayWhenDeferredResultHasNoRetryAfter()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        await outboxStore.EnqueueAsync(
            CreateEnvelope("deferred"),
            TestContext.Current.CancellationToken);
        var options = Options.Create(new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = TimeSpan.FromMinutes(7),
            DeferredDelay = TimeSpan.FromMinutes(13)
        });
        var drain = new AsiBackboneGovernanceOutboxDrain(
            outboxStore,
            new ResultEmitter(GovernanceEmissionResult.Deferred()),
            outboxOptions: options);

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.Deferred, drained.Status);
        Assert.Equal(DrainUtc.AddMinutes(13), drained.NextRetryUtc);
    }

    [Fact]
    public async Task DrainAsyncPreservesEmitterRetryAfterOverConfiguredDeferredDelay()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        await outboxStore.EnqueueAsync(
            CreateEnvelope("retry-after"),
            TestContext.Current.CancellationToken);
        DateTimeOffset retryAfterUtc = DrainUtc.AddMinutes(3);
        var options = Options.Create(new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = TimeSpan.FromMinutes(7),
            DeferredDelay = TimeSpan.FromMinutes(13)
        });
        var drain = new AsiBackboneGovernanceOutboxDrain(
            outboxStore,
            new ResultEmitter(GovernanceEmissionResult.Deferred(retryAfterUtc: retryAfterUtc)),
            outboxOptions: options);

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.Deferred, drained.Status);
        Assert.Equal(retryAfterUtc, drained.NextRetryUtc);
    }

    [Fact]
    public void ConstructorRejectsNegativeRetryDelayOptions()
    {
        var options = Options.Create(new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = TimeSpan.FromTicks(-1)
        });

        _ = Assert.Throws<InvalidOperationException>(() => new AsiBackboneGovernanceOutboxDrain(
            new InMemoryGovernanceOutboxStore(),
            NoOpGovernanceEmitter.Instance,
            outboxOptions: options));
    }

    [Fact]
    public void ConstructorRejectsNegativeDeferredDelayOptions()
    {
        var options = Options.Create(new AsiBackboneGovernanceOutboxOptions
        {
            DeferredDelay = TimeSpan.FromTicks(-1)
        });

        _ = Assert.Throws<InvalidOperationException>(() => new AsiBackboneGovernanceOutboxDrain(
            new InMemoryGovernanceOutboxStore(),
            NoOpGovernanceEmitter.Instance,
            outboxOptions: options));
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string suffix)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditLifecycle,
            eventId: $"event-{suffix}",
            occurredUtc: DrainUtc.AddMinutes(-1),
            envelopeId: $"envelope-{suffix}",
            correlationId: $"correlation-{suffix}",
            auditResidueId: $"residue-{suffix}",
            lifecycleStage: AuditResidueLifecycleStage.ExternalEmissionQueued,
            policyVersion: "v1",
            policyHash: "hash",
            traceId: $"trace-{suffix}",
            operationName: "governance.emit",
            outcome: "Queued",
            emitterProvider: "test-sink");
    }

    private sealed class ResultEmitter(GovernanceEmissionResult result) : IAsiBackboneGovernanceEmitter
    {
        private readonly GovernanceEmissionResult result = result;

        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingEmitter(Exception exception) : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();
            throw exception;
        }
    }
}
