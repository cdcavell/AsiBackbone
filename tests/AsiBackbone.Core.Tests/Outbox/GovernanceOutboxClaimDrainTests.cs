using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.Storage.InMemory.Outbox;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

public sealed class GovernanceOutboxClaimDrainTests
{
    [Fact]
    public async Task DrainAsyncThrowsWhenClaimLeasesEnabledWithoutClaimStore()
    {
        var drain = new AsiBackboneGovernanceOutboxDrain(
            new SelectionOnlyOutboxStore(),
            new DeliveredEmitter(),
            outboxOptions: Options.Create(new AsiBackboneGovernanceOutboxOptions
            {
                UseClaimLeases = true,
                ClaimWorkerId = "worker-1"
            }));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await drain.DrainAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DrainAsyncClaimsPendingEntryBeforeDeliveryWhenClaimLeasesEnabled()
    {
        var outboxStore = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await outboxStore.EnqueueAsync(
            CreateEnvelope("event-1"),
            TestContext.Current.CancellationToken);
        var drain = new AsiBackboneGovernanceOutboxDrain(
            outboxStore,
            new DeliveredEmitter(),
            outboxOptions: Options.Create(new AsiBackboneGovernanceOutboxOptions
            {
                UseClaimLeases = true,
                ClaimWorkerId = "worker-1",
                ClaimLeaseDuration = TimeSpan.FromMinutes(5)
            }));

        IReadOnlyList<GovernanceOutboxEntry> drainedEntries = await drain.DrainAsync(
            new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero),
            maxCount: 10,
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? storedEntry = await outboxStore.FindByOutboxEntryIdAsync(
            entry.OutboxEntryId,
            TestContext.Current.CancellationToken);

        GovernanceOutboxEntry drainedEntry = Assert.Single(drainedEntries);
        Assert.True(drainedEntry.IsDelivered);
        Assert.False(drainedEntry.HasClaim);
        Assert.NotNull(storedEntry);
        Assert.True(storedEntry.IsDelivered);
        Assert.False(storedEntry.HasClaim);
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string eventId)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditResidue,
            eventId: eventId,
            occurredUtc: new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero),
            envelopeId: $"envelope-{eventId}");
    }

    private sealed class DeliveredEmitter : IAsiBackboneGovernanceEmitter
    {
        public ValueTask<GovernanceEmissionResult> EmitAsync(
            GovernanceEmissionEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceEmissionResult.Delivered("test-provider", $"record-{envelope.EventId}"));
        }
    }

    private sealed class SelectionOnlyOutboxStore : IAsiBackboneGovernanceOutboxStore
    {
        public ValueTask<GovernanceOutboxEntry> EnqueueAsync(GovernanceEmissionEnvelope envelope, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(GovernanceOutboxEntry.Create(envelope));

        public ValueTask<GovernanceOutboxEntry> SaveAsync(GovernanceOutboxEntry entry, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(entry);

        public ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(string outboxEntryId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<GovernanceOutboxEntry?>(null);

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(int maxCount = 100, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<GovernanceOutboxEntry>>(Array.Empty<GovernanceOutboxEntry>());

        public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(DateTimeOffset utcNow, int maxCount = 100, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<GovernanceOutboxEntry>>(Array.Empty<GovernanceOutboxEntry>());

        public ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(string outboxEntryId, GovernanceEmissionResult result, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<GovernanceOutboxEntry> MarkFailedAsync(string outboxEntryId, GovernanceEmissionError governanceEmissionError, DateTimeOffset? nextRetryUtc = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(string outboxEntryId, GovernanceEmissionError governanceEmissionError, string? deadLetterReason = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}