using System.Collections.Concurrent;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;

namespace CDCavell.AsiBackbone.Storage.InMemory.Outbox;

/// <summary>
/// In-memory governance outbox store for tests, samples, and development hosts.
/// </summary>
/// <remarks>
/// This store is not durable across process restarts. Production hosts should use a durable provider such as EF Core or another host-owned storage adapter.
/// </remarks>
public sealed class InMemoryGovernanceOutboxStore : IAsiBackboneGovernanceOutboxStore
{
    private readonly ConcurrentDictionary<string, GovernanceOutboxEntry> entries = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<GovernanceOutboxEntry> EnqueueAsync(
        GovernanceEmissionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        var entry = GovernanceOutboxEntry.Create(envelope);

        return !entries.TryAdd(entry.OutboxEntryId, entry)
            ? throw new InvalidOperationException($"Outbox entry '{entry.OutboxEntryId}' already exists.")
            : ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask<GovernanceOutboxEntry> SaveAsync(
        GovernanceOutboxEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        entries[entry.OutboxEntryId] = entry;

        return ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(
        string outboxEntryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        cancellationToken.ThrowIfCancellationRequested();

        _ = entries.TryGetValue(outboxEntryId.Trim(), out GovernanceOutboxEntry? entry);

        return ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int normalizedMaxCount = NormalizeMaxCount(maxCount);

        IReadOnlyList<GovernanceOutboxEntry> matches = [.. entries.Values
            .Where(entry => entry.Status is GovernanceEmissionStatus.Pending)
            .OrderBy(entry => entry.CreatedUtc)
            .ThenBy(entry => entry.OutboxEntryId)
            .Take(normalizedMaxCount)];

        return ValueTask.FromResult(matches);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(
        DateTimeOffset utcNow,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int normalizedMaxCount = NormalizeMaxCount(maxCount);
        DateTimeOffset normalizedUtcNow = utcNow.ToUniversalTime();

        IReadOnlyList<GovernanceOutboxEntry> matches = [.. entries.Values
            .Where(entry => entry.IsRetryReady(normalizedUtcNow))
            .OrderBy(entry => entry.NextRetryUtc ?? entry.UpdatedUtc)
            .ThenBy(entry => entry.OutboxEntryId)
            .Take(normalizedMaxCount)];

        return ValueTask.FromResult(matches);
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
        string outboxEntryId,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkDelivered(result);

        entries[updatedEntry.OutboxEntryId] = updatedEntry;

        return updatedEntry;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        DateTimeOffset? nextRetryUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkFailed(governanceEmissionError, nextRetryUtc);

        entries[updatedEntry.OutboxEntryId] = updatedEntry;

        return updatedEntry;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        string? deadLetterReason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkDeadLettered(governanceEmissionError, deadLetterReason);

        entries[updatedEntry.OutboxEntryId] = updatedEntry;

        return updatedEntry;
    }

    private ValueTask<GovernanceOutboxEntry> RequireEntryAsync(
        string outboxEntryId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedOutboxEntryId = outboxEntryId.Trim();

        return entries.TryGetValue(normalizedOutboxEntryId, out GovernanceOutboxEntry? entry)
            ? ValueTask.FromResult(entry)
            : throw new InvalidOperationException($"Outbox entry '{normalizedOutboxEntryId}' was not found.");
    }

    private static int NormalizeMaxCount(int maxCount)
    {
        return maxCount <= 0
            ? throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Maximum count must be greater than zero.")
            : maxCount;
    }
}
