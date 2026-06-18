using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Outbox;

public sealed class GovernanceOutboxDrainBranchTests
{
    private static readonly DateTimeOffset DrainUtc = new(2026, 6, 18, 1, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedUtc = new(2026, 6, 18, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        var store = new RecordingOutboxStore();
        var emitter = new QueueEmitter(GovernanceEmissionResult.Delivered());

        _ = Assert.Throws<ArgumentNullException>(() => new AsiBackboneGovernanceOutboxDrain(null!, emitter));
        _ = Assert.Throws<ArgumentNullException>(() => new AsiBackboneGovernanceOutboxDrain(store, null!));
    }

    [Fact]
    public async Task DrainAsyncRejectsNonPositiveMaxCount()
    {
        var drain = new AsiBackboneGovernanceOutboxDrain(
            new RecordingOutboxStore(),
            new QueueEmitter(GovernanceEmissionResult.Delivered()));

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await drain.DrainAsync(maxCount: 0));
    }

    [Fact]
    public async Task DrainAsyncDoesNotQueryRetryReadyWhenPendingEntriesFillMaxCount()
    {
        var first = CreateEntry("outbox-1", "event-1");
        var second = CreateEntry("outbox-2", "event-2");
        var retryReady = CreateEntry("outbox-3", "event-3");
        var store = new RecordingOutboxStore(
            pendingEntries: [first, second],
            retryReadyEntries: [retryReady]);
        var emitter = new QueueEmitter(
            GovernanceEmissionResult.Delivered(providerName: "sink", providerRecordId: "record-1"),
            GovernanceEmissionResult.Delivered(providerName: "sink", providerRecordId: "record-2"));
        var drain = new AsiBackboneGovernanceOutboxDrain(store, emitter);

        IReadOnlyList<GovernanceOutboxEntry> drained = await drain.DrainAsync(
            DrainUtc,
            maxCount: 2,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, drained.Count);
        Assert.Equal(2, emitter.EmitCount);
        Assert.Equal(1, store.FindPendingCallCount);
        Assert.Equal(0, store.FindRetryReadyCallCount);
        Assert.All(drained, entry => Assert.True(entry.IsDelivered));
    }

    [Fact]
    public async Task DrainAsyncDeduplicatesRetryReadyEntriesAgainstPendingEntries()
    {
        var pending = CreateEntry("outbox-1", "event-1");
        var retryReadyDuplicate = pending.MarkDeferred(
            nextRetryUtc: DrainUtc.AddMinutes(-1),
            updatedUtc: DrainUtc.AddMinutes(-2));
        var retryReady = CreateEntry("outbox-2", "event-2").MarkDeferred(
            nextRetryUtc: DrainUtc.AddMinutes(-1),
            updatedUtc: DrainUtc.AddMinutes(-2));
        var store = new RecordingOutboxStore(
            pendingEntries: [pending],
            retryReadyEntries: [retryReadyDuplicate, retryReady]);
        var emitter = new QueueEmitter(
            GovernanceEmissionResult.Delivered(providerName: "sink", providerRecordId: "record-1"),
            GovernanceEmissionResult.Delivered(providerName: "sink", providerRecordId: "record-2"));
        var drain = new AsiBackboneGovernanceOutboxDrain(store, emitter);

        IReadOnlyList<GovernanceOutboxEntry> drained = await drain.DrainAsync(
            DrainUtc.ToOffset(TimeSpan.FromHours(-5)),
            maxCount: 3,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, drained.Count);
        Assert.Equal(2, emitter.EmitCount);
        Assert.Equal(1, store.FindRetryReadyCallCount);
        Assert.Equal(DrainUtc, store.LastRetryReadyUtc);
        Assert.Contains(drained, entry => entry.OutboxEntryId == "outbox-1");
        Assert.Contains(drained, entry => entry.OutboxEntryId == "outbox-2");
    }

    [Fact]
    public async Task DrainAsyncMarksEmitterExceptionAsRetryableFailure()
    {
        var entry = CreateEntry("outbox-1", "event-1");
        var store = new RecordingOutboxStore(pendingEntries: [entry]);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            store,
            new ThrowingEmitter(new InvalidOperationException("provider failed")));

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, drained.Status);
        Assert.Equal("emission.exception", drained.LastError?.Code);
        Assert.True(drained.LastError?.IsRetryable);
        Assert.Equal(typeof(InvalidOperationException).FullName, drained.LastError?.ProviderErrorCode);
        Assert.Equal(DrainUtc.AddMinutes(1), drained.NextRetryUtc);
    }

    [Fact]
    public async Task DrainAsyncRethrowsOperationCanceledExceptionWhenCancellationIsRequestedByEmitter()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var entry = CreateEntry("outbox-1", "event-1");
        var store = new RecordingOutboxStore(pendingEntries: [entry]);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            store,
            new CancellingEmitter(cancellationTokenSource));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await drain.DrainAsync(
                DrainUtc,
                cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task DrainAsyncMarksDeadLetteredResult()
    {
        var error = GovernanceEmissionError.Create(
            "provider.rejected",
            "Provider rejected the envelope.",
            providerName: "sink");
        var entry = CreateEntry("outbox-1", "event-1");
        var store = new RecordingOutboxStore(pendingEntries: [entry]);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            store,
            new QueueEmitter(GovernanceEmissionResult.DeadLettered(error)));

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(drained.IsDeadLettered);
        Assert.Equal(GovernanceEmissionStatus.DeadLettered, drained.Status);
        Assert.Equal("provider.rejected", drained.LastError?.Code);
        Assert.Equal("Provider rejected the envelope.", drained.DeadLetterReason);
    }

    [Fact]
    public async Task DrainAsyncDefersPendingResultWithFallbackError()
    {
        var entry = CreateEntry("outbox-1", "event-1");
        var store = new RecordingOutboxStore(pendingEntries: [entry]);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            store,
            new QueueEmitter(GovernanceEmissionResult.Pending(providerName: "sink")));

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.Deferred, drained.Status);
        Assert.Equal("emission.pending", drained.LastError?.Code);
        Assert.True(drained.LastError?.IsRetryable);
        Assert.Equal("sink", drained.ProviderName);
        Assert.Equal(DrainUtc.AddMinutes(1), drained.NextRetryUtc);
    }

    [Fact]
    public async Task DrainAsyncDefersDeferredResultWithoutError()
    {
        DateTimeOffset retryAfterUtc = DrainUtc.AddMinutes(5);
        var entry = CreateEntry("outbox-1", "event-1");
        var store = new RecordingOutboxStore(pendingEntries: [entry]);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            store,
            new QueueEmitter(GovernanceEmissionResult.Deferred(retryAfterUtc: retryAfterUtc)));

        GovernanceOutboxEntry drained = Assert.Single(await drain.DrainAsync(
            DrainUtc,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(GovernanceEmissionStatus.Deferred, drained.Status);
        Assert.Null(drained.LastError);
        Assert.Equal(retryAfterUtc, drained.NextRetryUtc);
    }

    private static GovernanceOutboxEntry CreateEntry(string outboxEntryId, string eventId)
    {
        return GovernanceOutboxEntry.Create(
            CreateEnvelope(eventId),
            outboxEntryId: outboxEntryId,
            createdUtc: CreatedUtc);
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string eventId)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditLifecycle,
            eventId: eventId,
            occurredUtc: CreatedUtc,
            envelopeId: $"envelope-{eventId}",
            correlationId: $"correlation-{eventId}",
            auditResidueId: $"residue-{eventId}",
            lifecycleStage: AuditResidueLifecycleStage.ExternalEmissionQueued,
            policyVersion: "v1",
            policyHash: "hash",
            traceId: $"trace-{eventId}",
            operationName: "governance.emit",
            outcome: "Queued");
    }

    private sealed class RecordingOutboxStore : IAsiBackboneGovernanceOutboxStore
    {
        private readonly Dictionary<string, GovernanceOutboxEntry> entries = new(StringComparer.Ordinal);
        private readonly List<GovernanceOutboxEntry> pendingEntries;
        private readonly List<GovernanceOutboxEntry> retryReadyEntries;

        public RecordingOutboxStore(
            IEnumerable<GovernanceOutboxEntry>? pendingEntries = null,
            IEnumerable<GovernanceOutboxEntry>? retryReadyEntries = null)
        {
            this.pendingEntries = pendingEntries?.ToList() ?? [];
            this.retryReadyEntries = retryReadyEntries?.ToList() ?? [];

            foreach (GovernanceOutboxEntry entry in this.pendingEntries.Concat(this.retryReadyEntries))
            {
                entries[entry.OutboxEntryId] = entry;
            }
        }

        public int FindPendingCallCount { get; private set; }

        public int FindRetryReadyCallCount { get; private set; }

        public DateTimeOffset? LastRetryReadyUtc { get; private set; }

        public ValueTask<GovernanceOutboxEntry> EnqueueAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            var entry = GovernanceOutboxEntry.Create(envelope);
            entries[entry.OutboxEntryId] = entry;
            pendingEntries.Add(entry);
            return ValueTask.FromResult(entry);
        }

        public ValueTask<GovernanceOutboxEntry> SaveAsync(
            GovernanceOutboxEntry entry,
            CancellationToken cancellationToken = default)
        {
            entries[entry.OutboxEntryId] = entry;
            return ValueTask.FromResult(entry);
        }

        public ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(
            string outboxEntryId,
            CancellationToken cancellationToken = default)
        {
            entries.TryGetValue(outboxEntryId, out GovernanceOutboxEntry? entry);
            return ValueTask.FromResult(entry);
        }

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            FindPendingCallCount++;
            IReadOnlyList<GovernanceOutboxEntry> result = pendingEntries.Take(maxCount).ToArray();
            return ValueTask.FromResult(result);
        }

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(
            DateTimeOffset utcNow,
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            FindRetryReadyCallCount++;
            LastRetryReadyUtc = utcNow;
            IReadOnlyList<GovernanceOutboxEntry> result = retryReadyEntries.Take(maxCount).ToArray();
            return ValueTask.FromResult(result);
        }

        public ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
            string outboxEntryId,
            GovernanceEmissionResult result,
            CancellationToken cancellationToken = default)
        {
            GovernanceOutboxEntry updated = entries[outboxEntryId].MarkDelivered(result, DrainUtc);
            entries[outboxEntryId] = updated;
            return ValueTask.FromResult(updated);
        }

        public ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
            string outboxEntryId,
            GovernanceEmissionError governanceEmissionError,
            DateTimeOffset? nextRetryUtc = null,
            CancellationToken cancellationToken = default)
        {
            GovernanceOutboxEntry updated = entries[outboxEntryId].MarkFailed(governanceEmissionError, nextRetryUtc, DrainUtc);
            entries[outboxEntryId] = updated;
            return ValueTask.FromResult(updated);
        }

        public ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
            string outboxEntryId,
            GovernanceEmissionError governanceEmissionError,
            string? deadLetterReason = null,
            CancellationToken cancellationToken = default)
        {
            GovernanceOutboxEntry updated = entries[outboxEntryId].MarkDeadLettered(governanceEmissionError, deadLetterReason, DrainUtc);
            entries[outboxEntryId] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    private sealed class QueueEmitter(params GovernanceEmissionResult[] results) : IAsiBackboneGovernanceEmitter
    {
        private readonly Queue<GovernanceEmissionResult> results = new(results);

        public int EmitCount { get; private set; }

        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();
            EmitCount++;
            return ValueTask.FromResult(results.Count > 0 ? results.Dequeue() : GovernanceEmissionResult.Delivered());
        }
    }

    private sealed class ThrowingEmitter(Exception exception) : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            throw exception;
        }
    }

    private sealed class CancellingEmitter(CancellationTokenSource cancellationTokenSource) : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationTokenSource.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
