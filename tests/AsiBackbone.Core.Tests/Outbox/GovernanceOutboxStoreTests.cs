using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.Storage.InMemory.Audit;
using AsiBackbone.Storage.InMemory.Outbox;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

/// <summary>
/// Tests for provider-neutral governance outbox persistence and lifecycle durability.
/// </summary>
public sealed class GovernanceOutboxStoreTests
{
    /// <summary>
    /// Verifies that a governance emission envelope can be saved before any external provider delivery is attempted.
    /// </summary>
    [Fact]
    public async Task EnqueueAsyncPersistsPendingGovernanceEmissionEnvelope()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceEmissionEnvelope envelope = CreateEnvelope("event-1", "correlation-1");

        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(envelope, TestContext.Current.CancellationToken);

        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            entry.OutboxEntryId,
            TestContext.Current.CancellationToken);
        IReadOnlyList<GovernanceOutboxEntry> pendingEntries = await outboxStore.FindPendingAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(storedEntry);
        Assert.Same(envelope, storedEntry.Envelope);
        Assert.Equal(GovernanceEmissionStatus.Pending, storedEntry.Status);
        Assert.Contains(pendingEntries, pendingEntry => pendingEntry.OutboxEntryId == entry.OutboxEntryId);
    }

    /// <summary>
    /// Verifies that local lifecycle audit records remain available when a downstream provider emission fails.
    /// </summary>
    [Fact]
    public async Task FailedEmissionDoesNotLoseLocalLifecycleAuditRecord()
    {
        var lifecycleStore = new InMemoryAuditResidueLifecycleStore();
        var outboxStore = new InMemoryGovernanceOutboxStore();

        var lifecycleEvent = AuditResidueLifecycleEvent.Create(
            AuditResidueLifecycleStage.ExternalEmissionQueued,
            "correlation-1",
            auditResidueId: "residue-1",
            eventId: "lifecycle-1",
            traceId: "trace-1",
            operationName: "governance.emit");

        AuditResidueLifecycleEvent savedLifecycleEvent = await lifecycleStore.AppendAsync(
            lifecycleEvent,
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            GovernanceEmissionEnvelope.FromLifecycleEvent(savedLifecycleEvent),
            TestContext.Current.CancellationToken);

        var error = GovernanceEmissionError.Create(
            "provider.unavailable",
            "Provider was unavailable.",
            isRetryable: true,
            providerName: "test-provider");

        GovernanceOutboxEntry failedEntry = await outboxStore.MarkFailedAsync(
            entry.OutboxEntryId,
            error,
            nextRetryUtc: DateTimeOffset.UtcNow.AddMinutes(5),
            cancellationToken: TestContext.Current.CancellationToken);

        AuditResidueLifecycleEvent? storedLifecycleEvent = await lifecycleStore.FindByEventIdAsync(
            "lifecycle-1",
            TestContext.Current.CancellationToken);

        Assert.NotNull(storedLifecycleEvent);
        Assert.Equal("correlation-1", storedLifecycleEvent.CorrelationId);
        Assert.Equal("residue-1", storedLifecycleEvent.AuditResidueId);
        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, failedEntry.Status);
        Assert.Equal(1, failedEntry.RetryCount);
        Assert.Equal("test-provider", failedEntry.ProviderName);
        Assert.Equal("provider.unavailable", failedEntry.LastError?.Code);
    }

    /// <summary>
    /// Verifies that retry-ready entries are returned only after their retry time is reached.
    /// </summary>
    [Fact]
    public async Task FindRetryReadyAsyncReturnsEntriesAfterNextRetryTime()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);

        DateTimeOffset retryUtc = new(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);
        var error = GovernanceEmissionError.Create(
            "provider.timeout",
            "Provider timed out.",
            isRetryable: true);

        GovernanceOutboxEntry failedEntry = await outboxStore.MarkFailedAsync(
            entry.OutboxEntryId,
            error,
            retryUtc,
            TestContext.Current.CancellationToken);

        IReadOnlyList<GovernanceOutboxEntry> beforeRetry = await outboxStore.FindRetryReadyAsync(
            retryUtc.AddTicks(-1),
            cancellationToken: TestContext.Current.CancellationToken);
        IReadOnlyList<GovernanceOutboxEntry> afterRetry = await outboxStore.FindRetryReadyAsync(
            retryUtc,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, failedEntry.Status);
        Assert.DoesNotContain(beforeRetry, retryReadyEntry => retryReadyEntry.OutboxEntryId == entry.OutboxEntryId);
        Assert.Contains(afterRetry, retryReadyEntry => retryReadyEntry.OutboxEntryId == entry.OutboxEntryId);
    }

    /// <summary>
    /// Verifies that entries can transition to delivered and are removed from pending work.
    /// </summary>
    [Fact]
    public async Task MarkDeliveredAsyncTransitionsEntryToDelivered()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);

        GovernanceOutboxEntry deliveredEntry = await outboxStore.MarkDeliveredAsync(
            entry.OutboxEntryId,
            GovernanceEmissionResult.Delivered(
                providerName: "test-provider",
                providerRecordId: "record-1"),
            TestContext.Current.CancellationToken);
        IReadOnlyList<GovernanceOutboxEntry> pendingEntries = await outboxStore.FindPendingAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(deliveredEntry.IsDelivered);
        Assert.Equal(GovernanceEmissionStatus.Delivered, deliveredEntry.Status);
        Assert.Equal("test-provider", deliveredEntry.ProviderName);
        Assert.Equal("record-1", deliveredEntry.ProviderRecordId);
        Assert.DoesNotContain(pendingEntries, pendingEntry => pendingEntry.OutboxEntryId == entry.OutboxEntryId);
    }

    /// <summary>
    /// Verifies that concurrent retryable failure updates on the same in-memory entry do not collapse into accidental last-write-wins state.
    /// </summary>
    [Fact]
    public async Task ConcurrentMarkFailedAsyncUpdatesSameEntryWithoutLosingRetryCount()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);
        var error = GovernanceEmissionError.Create(
            "provider.timeout",
            "Provider timed out.",
            isRetryable: true,
            providerName: "test-provider");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        const int attemptCount = 4;

        Task<GovernanceOutboxEntry>[] updateTasks = [.. Enumerable.Range(0, attemptCount).Select(_ =>
            Task.Run(async () =>
            {
                await startSignal.Task.WaitAsync(cancellationToken);

                return await outboxStore.MarkFailedAsync(
                    entry.OutboxEntryId,
                    error,
                    DateTimeOffset.UtcNow.AddMinutes(1),
                    cancellationToken);
            }, cancellationToken))];

        startSignal.SetResult();
        _ = await Task.WhenAll(updateTasks);

        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            entry.OutboxEntryId,
            cancellationToken);

        Assert.NotNull(storedEntry);
        Assert.Equal(attemptCount, storedEntry.RetryCount);
        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, storedEntry.Status);
        Assert.Equal("provider.timeout", storedEntry.LastError?.Code);
    }

    /// <summary>
    /// Verifies that a delivered same-entry transition remains terminal when it races with a retryable failure transition.
    /// </summary>
    [Fact]
    public async Task ConcurrentDeliveredAndFailedTransitionsKeepDeliveredTerminalState()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);
        var error = GovernanceEmissionError.Create(
            "provider.timeout",
            "Provider timed out.",
            isRetryable: true,
            providerName: "test-provider");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<GovernanceOutboxEntry> deliveredTask = Task.Run(async () =>
        {
            await startSignal.Task.WaitAsync(cancellationToken);

            return await outboxStore.MarkDeliveredAsync(
                entry.OutboxEntryId,
                GovernanceEmissionResult.Delivered(
                    providerName: "test-provider",
                    providerRecordId: "record-1"),
                cancellationToken);
        }, cancellationToken);

        Task<GovernanceOutboxEntry> failedTask = Task.Run(async () =>
        {
            await startSignal.Task.WaitAsync(cancellationToken);

            return await outboxStore.MarkFailedAsync(
                entry.OutboxEntryId,
                error,
                DateTimeOffset.UtcNow.AddMinutes(1),
                cancellationToken);
        }, cancellationToken);

        startSignal.SetResult();
        _ = await Task.WhenAll(deliveredTask, failedTask);

        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            entry.OutboxEntryId,
            cancellationToken);

        Assert.NotNull(storedEntry);
        Assert.True(storedEntry.IsDelivered);
        Assert.Equal(GovernanceEmissionStatus.Delivered, storedEntry.Status);
        Assert.Equal("record-1", storedEntry.ProviderRecordId);
    }

    /// <summary>
    /// Verifies that a stale deferred save cannot overwrite a terminal delivered state in the in-memory store.
    /// </summary>
    [Fact]
    public async Task SaveAsyncDoesNotOverwriteTerminalDeliveredEntryWithDeferredSnapshot()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);
        var error = GovernanceEmissionError.Create(
            "provider.deferred",
            "Provider asked the host to retry later.",
            isRetryable: true,
            providerName: "test-provider");
        GovernanceOutboxEntry deferredSnapshot = entry.MarkDeferred(
            error,
            DateTimeOffset.UtcNow.AddMinutes(1));

        GovernanceOutboxEntry deliveredEntry = await outboxStore.MarkDeliveredAsync(
            entry.OutboxEntryId,
            GovernanceEmissionResult.Delivered(
                providerName: "test-provider",
                providerRecordId: "record-1"),
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry savedEntry = await outboxStore.SaveAsync(
            deferredSnapshot,
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            entry.OutboxEntryId,
            TestContext.Current.CancellationToken);

        Assert.True(deliveredEntry.IsDelivered);
        Assert.True(savedEntry.IsDelivered);
        Assert.NotNull(storedEntry);
        Assert.True(storedEntry.IsDelivered);
        Assert.Equal("record-1", storedEntry.ProviderRecordId);
    }

    /// <summary>
    /// Verifies that entries can transition to a terminal dead-letter state.
    /// </summary>
    [Fact]
    public async Task MarkDeadLetteredAsyncTransitionsEntryToTerminalDeadLetterState()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1", "correlation-1"),
            TestContext.Current.CancellationToken);
        var error = GovernanceEmissionError.Create(
            "provider.rejected",
            "Provider rejected the minimized envelope.",
            providerName: "test-provider");

        GovernanceOutboxEntry deadLetteredEntry = await outboxStore.MarkDeadLetteredAsync(
            entry.OutboxEntryId,
            error,
            "policy-quarantine",
            TestContext.Current.CancellationToken);
        IReadOnlyList<GovernanceOutboxEntry> retryReadyEntries = await outboxStore.FindRetryReadyAsync(
            DateTimeOffset.UtcNow.AddDays(1),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(deadLetteredEntry.IsDeadLettered);
        Assert.Equal(GovernanceEmissionStatus.DeadLettered, deadLetteredEntry.Status);
        Assert.Equal("policy-quarantine", deadLetteredEntry.DeadLetterReason);
        Assert.Equal("provider.rejected", deadLetteredEntry.LastError?.Code);
        Assert.DoesNotContain(retryReadyEntries, retryReadyEntry => retryReadyEntry.OutboxEntryId == entry.OutboxEntryId);
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
}
