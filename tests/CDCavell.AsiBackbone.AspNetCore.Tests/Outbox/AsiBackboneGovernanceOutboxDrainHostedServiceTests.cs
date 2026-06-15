using System.Collections.Concurrent;
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.AspNetCore.Outbox;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Outbox;

public sealed class AsiBackboneGovernanceOutboxDrainHostedServiceTests
{
    [Fact]
    public void AddAsiBackboneGovernanceOutboxDrainWorkerRegistersHostedWorkerAndOptions()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddSingleton<IAsiBackboneGovernanceOutboxStore, RecordingGovernanceOutboxStore>();
        _ = services.AddSingleton<IAsiBackboneGovernanceEmitter>(_ => new RecordingGovernanceEmitter(_ => GovernanceEmissionResult.Delivered("test-sink")));

        _ = services.AddAsiBackboneGovernanceOutboxDrainWorker(options =>
        {
            options.BatchSize = 7;
            options.PollingInterval = TimeSpan.FromSeconds(5);
            options.RetryClock = () => new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        AsiBackboneGovernanceOutboxDrainWorkerOptions options = provider.GetRequiredService<IOptions<AsiBackboneGovernanceOutboxDrainWorkerOptions>>().Value;
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        Assert.IsType<AsiBackboneGovernanceOutboxDrainHostedService>(hostedService);
        Assert.True(options.Enabled);
        Assert.Equal(7, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(5), options.PollingInterval);
        Assert.Equal(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero), options.RetryClock());
    }

    [Fact]
    public async Task DisabledWorkerDoesNotDrainEntries()
    {
        var store = new RecordingGovernanceOutboxStore();
        var emitter = new RecordingGovernanceEmitter(_ => GovernanceEmissionResult.Delivered("test-sink"));
        _ = await store.EnqueueAsync(CreateEnvelope("disabled"), TestContext.Current.CancellationToken);
        using ServiceProvider provider = BuildProvider(store, emitter, options =>
        {
            options.Enabled = false;
            options.PollingInterval = TimeSpan.FromMilliseconds(10);
        });
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, store.FindPendingCallCount);
        Assert.Equal(0, emitter.EmitCallCount);
    }

    [Fact]
    public async Task WorkerHonorsConfiguredBatchSize()
    {
        var store = new RecordingGovernanceOutboxStore();
        var firstEmission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var emitter = new RecordingGovernanceEmitter(envelope =>
        {
            firstEmission.TrySetResult();
            return GovernanceEmissionResult.Delivered("test-sink", envelope.EnvelopeId);
        });
        GovernanceOutboxEntry firstEntry = await store.EnqueueAsync(CreateEnvelope("batch-one"), TestContext.Current.CancellationToken);
        GovernanceOutboxEntry secondEntry = await store.EnqueueAsync(CreateEnvelope("batch-two"), TestContext.Current.CancellationToken);
        using ServiceProvider provider = BuildProvider(store, emitter, options =>
        {
            options.BatchSize = 1;
            options.PollingInterval = TimeSpan.FromMinutes(5);
        });
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await firstEmission.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        GovernanceOutboxEntry? firstStoredEntry = await store.FindByOutboxEntryIdAsync(firstEntry.OutboxEntryId, TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? secondStoredEntry = await store.FindByOutboxEntryIdAsync(secondEntry.OutboxEntryId, TestContext.Current.CancellationToken);

        Assert.Equal(1, emitter.EmitCallCount);
        Assert.NotNull(firstStoredEntry);
        Assert.NotNull(secondStoredEntry);
        Assert.Equal(GovernanceEmissionStatus.Delivered, firstStoredEntry.Status);
        Assert.Equal(GovernanceEmissionStatus.Pending, secondStoredEntry.Status);
    }

    [Fact]
    public async Task WorkerPreservesProviderFailureStateForRetry()
    {
        var store = new RecordingGovernanceOutboxStore();
        var failedEmission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var emitter = new ThrowingGovernanceEmitter();
        store.EntryFailed += (_, _) => failedEmission.TrySetResult();
        GovernanceOutboxEntry entry = await store.EnqueueAsync(CreateEnvelope("provider-failure"), TestContext.Current.CancellationToken);
        DateTimeOffset retryClock = new(2026, 6, 15, 18, 15, 0, TimeSpan.Zero);
        using ServiceProvider provider = BuildProvider(store, emitter, options =>
        {
            options.BatchSize = 1;
            options.PollingInterval = TimeSpan.FromMinutes(5);
            options.RetryClock = () => retryClock;
        });
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await failedEmission.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        GovernanceOutboxEntry? storedEntry = await store.FindByOutboxEntryIdAsync(entry.OutboxEntryId, TestContext.Current.CancellationToken);

        Assert.NotNull(storedEntry);
        Assert.Equal(GovernanceEmissionStatus.RetryableFailure, storedEntry.Status);
        Assert.Equal(1, storedEntry.RetryCount);
        Assert.Equal("emission.exception", storedEntry.LastError?.Code);
        Assert.Equal(retryClock.AddMinutes(1), storedEntry.NextRetryUtc);
    }

    [Fact]
    public async Task StopAsyncCanRunOptionalShutdownDrain()
    {
        var store = new RecordingGovernanceOutboxStore();
        var emission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var emitter = new RecordingGovernanceEmitter(envelope =>
        {
            emission.TrySetResult();
            return GovernanceEmissionResult.Delivered("test-sink", envelope.EnvelopeId);
        });
        GovernanceOutboxEntry entry = await store.EnqueueAsync(CreateEnvelope("shutdown"), TestContext.Current.CancellationToken);
        using ServiceProvider provider = BuildProvider(store, emitter, options =>
        {
            options.PollingInterval = TimeSpan.FromMinutes(5);
            options.DrainOnShutdown = true;
            options.ShutdownDrainTimeout = TimeSpan.FromSeconds(2);
        });
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await emission.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        GovernanceOutboxEntry? storedEntry = await store.FindByOutboxEntryIdAsync(entry.OutboxEntryId, TestContext.Current.CancellationToken);

        Assert.NotNull(storedEntry);
        Assert.Equal(GovernanceEmissionStatus.Delivered, storedEntry.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddAsiBackboneGovernanceOutboxDrainWorkerRejectsInvalidBatchSize(int batchSize)
    {
        ServiceCollection services = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAsiBackboneGovernanceOutboxDrainWorker(options => options.BatchSize = batchSize));

        Assert.Contains("batch size", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider(
        IAsiBackboneGovernanceOutboxStore store,
        IAsiBackboneGovernanceEmitter emitter,
        Action<AsiBackboneGovernanceOutboxDrainWorkerOptions> configure)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddSingleton(store);
        _ = services.AddSingleton(emitter);
        _ = services.AddAsiBackboneGovernanceOutboxDrainWorker(configure);

        return services.BuildServiceProvider();
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string suffix)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditLifecycle,
            eventId: $"event-{suffix}",
            occurredUtc: new DateTimeOffset(2026, 6, 15, 17, 59, 0, TimeSpan.Zero),
            envelopeId: $"envelope-{suffix}",
            correlationId: $"correlation-{suffix}",
            auditResidueId: $"residue-{suffix}",
            lifecycleStage: AuditResidueLifecycleStage.ExternalEmissionQueued,
            policyVersion: "v1",
            policyHash: $"hash-{suffix}",
            traceId: $"trace-{suffix}",
            spanId: $"span-{suffix}",
            parentSpanId: $"parent-span-{suffix}",
            operationName: "governance.emit",
            outcome: "Queued",
            emitterStatus: "pending",
            emitterProvider: "outbox");
    }

    private sealed class RecordingGovernanceEmitter(Func<GovernanceEmissionEnvelope, GovernanceEmissionResult> emit) : IAsiBackboneGovernanceEmitter
    {
        private readonly Func<GovernanceEmissionEnvelope, GovernanceEmissionResult> emit = emit;
        private int emitCallCount;

        public int EmitCallCount => Volatile.Read(ref emitCallCount);

        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();
            _ = Interlocked.Increment(ref emitCallCount);

            return ValueTask.FromResult(emit(envelope));
        }
    }

    private sealed class ThrowingGovernanceEmitter : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException("Provider unavailable.");
        }
    }

    private sealed class RecordingGovernanceOutboxStore : IAsiBackboneGovernanceOutboxStore
    {
        private readonly ConcurrentDictionary<string, GovernanceOutboxEntry> entries = new(StringComparer.Ordinal);
        private int findPendingCallCount;

        public event EventHandler<GovernanceOutboxEntry>? EntryFailed;

        public int FindPendingCallCount => Volatile.Read(ref findPendingCallCount);

        public ValueTask<GovernanceOutboxEntry> EnqueueAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            cancellationToken.ThrowIfCancellationRequested();
            GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(envelope);
            entries[entry.OutboxEntryId] = entry;

            return ValueTask.FromResult(entry);
        }

        public ValueTask<GovernanceOutboxEntry> SaveAsync(
            GovernanceOutboxEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);
            cancellationToken.ThrowIfCancellationRequested();
            entries[entry.OutboxEntryId] = entry;

            return ValueTask.FromResult(entry);
        }

        public ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(
            string outboxEntryId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
            cancellationToken.ThrowIfCancellationRequested();
            _ = entries.TryGetValue(outboxEntryId.Trim(), out GovernanceOutboxEntry? entry);

            return ValueTask.FromResult(entry);
        }

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = Interlocked.Increment(ref findPendingCallCount);
            IReadOnlyList<GovernanceOutboxEntry> pendingEntries = [.. entries.Values
                .Where(entry => entry.Status is GovernanceEmissionStatus.Pending)
                .OrderBy(entry => entry.CreatedUtc)
                .ThenBy(entry => entry.OutboxEntryId)
                .Take(maxCount)];

            return ValueTask.FromResult(pendingEntries);
        }

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(
            DateTimeOffset utcNow,
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<GovernanceOutboxEntry> retryReadyEntries = [.. entries.Values
                .Where(entry => entry.IsRetryReady(utcNow))
                .OrderBy(entry => entry.NextRetryUtc ?? entry.UpdatedUtc)
                .ThenBy(entry => entry.OutboxEntryId)
                .Take(maxCount)];

            return ValueTask.FromResult(retryReadyEntries);
        }

        public async ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
            string outboxEntryId,
            GovernanceEmissionResult result,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(result);
            GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
            GovernanceOutboxEntry updatedEntry = entry.MarkDelivered(result);
            entries[updatedEntry.OutboxEntryId] = updatedEntry;

            return updatedEntry;
        }

        public async ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
            string outboxEntryId,
            GovernanceEmissionError governanceEmissionError,
            DateTimeOffset? nextRetryUtc = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(governanceEmissionError);
            GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
            GovernanceOutboxEntry updatedEntry = entry.MarkFailed(governanceEmissionError, nextRetryUtc);
            entries[updatedEntry.OutboxEntryId] = updatedEntry;
            EntryFailed?.Invoke(this, updatedEntry);

            return updatedEntry;
        }

        public async ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
            string outboxEntryId,
            GovernanceEmissionError governanceEmissionError,
            string? deadLetterReason = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(governanceEmissionError);
            GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
            GovernanceOutboxEntry updatedEntry = entry.MarkDeadLettered(governanceEmissionError, deadLetterReason);
            entries[updatedEntry.OutboxEntryId] = updatedEntry;

            return updatedEntry;
        }

        private ValueTask<GovernanceOutboxEntry> RequireEntryAsync(
            string outboxEntryId,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
            cancellationToken.ThrowIfCancellationRequested();
            string normalizedOutboxEntryId = outboxEntryId.Trim();

            return entries.TryGetValue(normalizedOutboxEntryId, out GovernanceOutboxEntry? entry)
                ? ValueTask.FromResult(entry)
                : throw new InvalidOperationException($"Outbox entry '{normalizedOutboxEntryId}' was not found.");
        }
    }
}