using AsiBackbone.Core.CapabilityTokens;

namespace AsiBackbone.Storage.InMemory.CapabilityTokens;

/// <summary>
/// Provides a non-durable, in-process capability grant use store for tests, samples, and local validation.
/// </summary>
/// <remarks>
/// This store is thread-safe within a single process, but it is not durable, distributed, replicated, or suitable for
/// production replay protection. Hosts that require production single-use or bounded-use guarantees should provide a
/// durable implementation of <see cref="ICapabilityGrantUseStore" /> with documented transaction, locking, retention,
/// and failure semantics.
/// </remarks>
public sealed class InMemoryCapabilityGrantUseStore : ICapabilityGrantUseStore
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, int> useCounts = new(StringComparer.Ordinal);
    private readonly HashSet<string> stoppedGrantIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> cancelledGrantIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the observed use count for a grant identifier.
    /// </summary>
    /// <param name="grantId">The stable capability grant identifier.</param>
    /// <returns>The observed use count, or zero when the grant has not been consumed by this store instance.</returns>
    public int GetUseCount(string grantId)
    {
        string normalizedGrantId = NormalizeGrantId(grantId);

        lock (syncRoot)
        {
            return useCounts.TryGetValue(normalizedGrantId, out int useCount)
                ? useCount
                : 0;
        }
    }

    /// <summary>
    /// Marks a grant as stopped for subsequent local validation attempts.
    /// </summary>
    /// <param name="grantId">The stable capability grant identifier.</param>
    public void StopGrant(string grantId)
    {
        string normalizedGrantId = NormalizeGrantId(grantId);

        lock (syncRoot)
        {
            _ = stoppedGrantIds.Add(normalizedGrantId);
            _ = cancelledGrantIds.Remove(normalizedGrantId);
        }
    }

    /// <summary>
    /// Marks a grant as cancelled for subsequent local validation attempts.
    /// </summary>
    /// <param name="grantId">The stable capability grant identifier.</param>
    public void CancelGrant(string grantId)
    {
        string normalizedGrantId = NormalizeGrantId(grantId);

        lock (syncRoot)
        {
            _ = cancelledGrantIds.Add(normalizedGrantId);
            _ = stoppedGrantIds.Remove(normalizedGrantId);
        }
    }

    /// <summary>
    /// Clears use-count and stopped/cancelled state from this in-memory store instance.
    /// </summary>
    public void Clear()
    {
        lock (syncRoot)
        {
            useCounts.Clear();
            stoppedGrantIds.Clear();
            cancelledGrantIds.Clear();
        }
    }

    /// <inheritdoc />
    public ValueTask<CapabilityGrantUseResult> TryConsumeAsync(
        CapabilityTokenGrant grant,
        int maxUseCount,
        DateTimeOffset usedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxUseCount, 1);
        _ = usedUtc.ToUniversalTime();
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            if (stoppedGrantIds.Contains(grant.TokenId))
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.Stopped("The in-memory capability grant use store marked this grant as stopped."));
            }

            if (cancelledGrantIds.Contains(grant.TokenId))
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.Cancelled("The in-memory capability grant use store marked this grant as cancelled."));
            }

            useCounts.TryGetValue(grant.TokenId, out int currentCount);

            if (currentCount >= maxUseCount)
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.UseLimitExceeded(
                    currentCount,
                    "The in-memory capability grant use limit was exceeded."));
            }

            int nextCount = currentCount + 1;
            useCounts[grant.TokenId] = nextCount;
            return ValueTask.FromResult(CapabilityGrantUseResult.Accepted(nextCount));
        }
    }

    private static string NormalizeGrantId(string grantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grantId);
        return grantId.Trim();
    }
}
