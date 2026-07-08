using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.Storage.InMemory.Outbox;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

public sealed class GovernanceOutboxDrainNonClaimTests
{
    [Fact]
    public async Task DrainAsyncReturnsEmptyWhenNoEntriesAreEligible()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        AsiBackboneGovernanceOutboxDrain drain = CreateDrain(outboxStore, new ResultEmitter(GovernanceEmissionResult.Delivered("provider", "record-empty")));

        IReadOnlyList<GovernanceOutboxEntry> drainedEntries = await drain.DrainAsync(
            new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(drainedEntries);
    }

    [Fact]
    public async Task DrainAsyncMergesPendingAndRetryReadyEntriesWhenCapacityRemains()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        DateTimeOffset drainUtc = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        GovernanceOutboxEntry pendingEntry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-pending", "correlation-pending"),
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry retryEntry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-retry", "correlation-retry"),
            TestContext.Current.CancellationToken);
        _ = await outboxStore.MarkFailedAsync(
            retryEntry.OutboxEntryId,
            GovernanceEmissionError.Create("provider.transient", "Transient failure.", isRetryable: true),
            drainUtc.AddMinutes(-1),
            TestContext.Current.CancellationToken);
        AsiBackboneGovernanceOutboxDrain drain = CreateDrain(outboxStore, new PerEnvelopeDeliveredEmitter());

        IReadOnlyList<GovernanceOutboxEntry> drainedEntries = await drain.DrainAsync(
            drainUtc,
            maxCount: 2,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, drainedEntries.Count);
        Assert.Contains(drainedEntries, entry => entry.OutboxEntryId == pendingEntry.OutboxEntryId && entry.IsDelivered);
        Assert.Contains(drainedEntries, entry => entry.OutboxEntryId == retryEntry.OutboxEntryId && entry.IsDelivered);
    }

    [Fact]
    public async Task DrainAsyncStopsAtPendingLimitBeforeRetryReadyLookup()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        DateTimeOffset drainUtc = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        _ = await outboxStore.EnqueueAsync(CreateEnvelope("event-pending", "correlation-pending"), TestContext.Current.CancellationToken);
        GovernanceOutboxEntry retryEntry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-retry", "correlation-retry"),
            TestContext.Current.CancellationToken);
        _ = await outboxStore.MarkFailedAsync(
            retryEntry.OutboxEntryId,
            GovernanceEmissionError.Create("provider.transient", "Transient failure.", isRetryable: true),
            drainUtc.AddMinutes(-1),
            TestContext.Current.CancellationToken);
        AsiBackboneGovernanceOutboxDrain drain = CreateDrain(outboxStore, new PerEnvelopeDeliveredEmitter());

        IReadOnlyList<GovernanceOutboxEntry> drainedEntries = await drain.DrainAsync(
            drainUtc,
            maxCount: 1,
            cancellationToken: TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? storedRetryEntry = await outboxStore.FindByOutboxEntryIdAsync(
            retryEntry.OutboxEntryId,
            TestContext.Current.CancellationToken);

        GovernanceOutboxEntry drainedEntry = Assert.Single(drainedEntries);
        Assert.True(drainedEntry.IsDelivered);
        Assert.NotNull(storedRetryEntry);
        Assert.False(storedRetryEntry.IsDelivered);
    }

    [Fact]
    public async Task DrainAsyncAppliesNonClaimedResultTransitions()
    {
        DateTimeOffset drainUtc = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

        GovernanceOutboxEntry deferredEntry = await DrainSingleAsync(
            GovernanceEmissionResult.Deferred(
                GovernanceEmissionError.Create("provider.deferred", "Deferred.", isRetryable: true),
                drainUtc.AddMinutes(10),
                "provider"),
            drainUtc,
            "deferred");
        Assert.Equal(GovernanceEmissionStatus.Deferred, deferredEntry.Status);
        Assert.Equal(drainUtc.AddMinutes(10), deferredEntry.NextRetryUtc);

        GovernanceOutboxEntry pendingEntry = await DrainSingleAsync(
            GovernanceEmissionResult.Pending("provider"),
            drainUtc,
            "pending");
        Assert.Equal(GovernanceEmissionStatus.Deferred, pendingEntry.Status);
        Assert.Equal("emission.pending", pendingEntry.LastError?.Code);
        Assert.Equal(drainUtc.AddMinutes(1), pendingEntry.NextRetryUtc);

        GovernanceOutboxEntry deadLetterEntry = await DrainSingleAsync(
            GovernanceEmissionResult.DeadLettered(
                GovernanceEmissionError.Create("provider.dead", "Dead letter.", providerName: "provider")),
            drainUtc,
            "dead");
        Assert.True(deadLetterEntry.IsDeadLettered);

        GovernanceOutboxEntry failedEntry = await DrainSingleAsync(
            GovernanceEmissionResult.Failed(
                GovernanceEmissionError.Create("provider.failed", "Failed.", isRetryable: false, providerName: "provider")),
            drainUtc,
            "failed");
        Assert.Equal(GovernanceEmissionStatus.Failed, failedEntry.Status);
    }

    [Fact]
    public async Task DrainAsyncMarksNonClaimedEntryRetryableWhenEmitterThrows()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        DateTimeOffset drainUtc = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        _ = await outboxStore.EnqueueAsync(CreateEnvelope("event-throw", "correlation-throw"), TestContext.Current.CancellationToken);
        AsiBackboneGovernanceOutboxDrain drain = CreateDrain(outboxStore, new ThrowingEmitter());

        GovernanceOutboxEntry thrownEntry = Assert.Single(await drain.DrainAsync(
            drainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, thrownEntry.Status);
        Assert.Equal("emission.exception", thrownEntry.LastError?.Code);
        Assert.Equal(drainUtc.AddMinutes(1), thrownEntry.NextRetryUtc);
    }

    private static async Task<GovernanceOutboxEntry> DrainSingleAsync(
        GovernanceEmissionResult result,
        DateTimeOffset drainUtc,
        string suffix)
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        _ = await outboxStore.EnqueueAsync(
            CreateEnvelope($"event-{suffix}", $"correlation-{suffix}"),
            TestContext.Current.CancellationToken);
        AsiBackboneGovernanceOutboxDrain drain = CreateDrain(outboxStore, new ResultEmitter(result));

        return Assert.Single(await drain.DrainAsync(
            drainUtc,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    private static AsiBackboneGovernanceOutboxDrain CreateDrain(
        InMemoryGovernanceOutboxStore outboxStore,
        IAsiBackboneGovernanceEmitter emitter)
    {
        return new AsiBackboneGovernanceOutboxDrain(
            outboxStore,
            emitter,
            outboxOptions: Options.Create(new AsiBackboneGovernanceOutboxOptions()));
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string eventId, string correlationId)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditResidue,
            eventId: eventId,
            occurredUtc: new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.Zero),
            envelopeId: $"envelope-{eventId}",
            correlationId: correlationId,
            auditResidueId: $"residue-{eventId}",
            traceId: $"trace-{eventId}",
            operationName: "governance.emit",
            emitterStatus: "pending",
            emitterProvider: "outbox");
    }

    private sealed class ResultEmitter(GovernanceEmissionResult result) : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(result);
        }
    }

    private sealed class PerEnvelopeDeliveredEmitter : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceEmissionResult.Delivered("provider", $"record-{envelope.EventId}"));
        }
    }

    private sealed class ThrowingEmitter : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Emitter failed.");
        }
    }
}
