using System.Collections.Concurrent;
using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;

namespace AsiBackbone.Storage.InMemory.Outbox;

/// <summary>
/// In-memory governance outbox store for tests, samples, and development hosts.
/// </summary>
/// <remarks>
/// This store is not durable across process restarts. Production hosts should use a durable provider such as EF Core or another host-owned storage adapter.
/// Same-entry status transitions use single-process compare-and-swap updates so tests and local validation do not accidentally observe last-write-wins behavior.
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

        return ValueTask.FromResult(SaveEntry(entry, cancellationToken));
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
    public ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
        string outboxEntryId,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry updatedEntry = UpdateExistingEntry(
            outboxEntryId,
            entry => IsTerminal(entry) ? entry : entry.MarkDelivered(result),
            cancellationToken);

        return ValueTask.FromResult(updatedEntry);
    }

    /// <inheritdoc />
    public ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        DateTimeOffset? nextRetryUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry updatedEntry = UpdateExistingEntry(
            outboxEntryId,
            entry => IsTerminal(entry) ? entry : entry.MarkFailed(governanceEmissionError, nextRetryUtc),
            cancellationToken);

        return ValueTask.FromResult(updatedEntry);
    }

    /// <inheritdoc />
    public ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        string? deadLetterReason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry updatedEntry = UpdateExistingEntry(
            outboxEntryId,
            entry => IsTerminal(entry) ? entry : entry.MarkDeadLettered(governanceEmissionError, deadLetterReason),
            cancellationToken);

        return ValueTask.FromResult(updatedEntry);
    }

    private GovernanceOutboxEntry SaveEntry(
        GovernanceOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!entries.TryGetValue(entry.OutboxEntryId, out GovernanceOutboxEntry? currentEntry))
            {
                if (entries.TryAdd(entry.OutboxEntryId, entry))
                {
                    return entry;
                }

                continue;
            }

            if (IsTerminal(currentEntry))
            {
                return currentEntry;
            }

            if (entries.TryUpdate(entry.OutboxEntryId, entry, currentEntry))
            {
                return entry;
            }
        }
    }

    private GovernanceOutboxEntry UpdateExistingEntry(
        string outboxEntryId,
        Func<GovernanceOutboxEntry, GovernanceOutboxEntry> updateEntry,
        CancellationToken cancellationToken)
    {
        string normalizedOutboxEntryId = outboxEntryId.Trim();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!entries.TryGetValue(normalizedOutboxEntryId, out GovernanceOutboxEntry? currentEntry))
            {
                throw new InvalidOperationException($"Outbox entry '{normalizedOutboxEntryId}' was not found.");
            }

            GovernanceOutboxEntry updatedEntry = updateEntry(currentEntry);

            if (ReferenceEquals(currentEntry, updatedEntry))
            {
                return currentEntry;
            }

            if (entries.TryUpdate(normalizedOutboxEntryId, updatedEntry, currentEntry))
            {
                return updatedEntry;
            }
        }
    }

    private static bool IsTerminal(GovernanceOutboxEntry entry)
    {
        return entry.IsDelivered || entry.IsDeadLettered;
    }

    private static int NormalizeMaxCount(int maxCount)
    {
        return maxCount <= 0
            ? throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Maximum count must be greater than zero.")
            : maxCount;
    }
}
