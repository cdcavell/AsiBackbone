using System.Collections.Concurrent;
using AsiBackbone.Core.Audit;

namespace AsiBackbone.Storage.InMemory.Audit;

/// <summary>
/// In-memory audit residue lifecycle store for tests, samples, and development hosts.
/// </summary>
/// <remarks>
/// This store is not durable across process restarts. Production hosts should use a durable provider such as EF Core or another host-owned storage adapter.
/// </remarks>
public sealed class InMemoryAuditResidueLifecycleStore : IAsiBackboneAuditResidueLifecycleStore
{
    private readonly ConcurrentDictionary<string, AuditResidueLifecycleEvent> events = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<AuditResidueLifecycleEvent> AppendAsync(
        AuditResidueLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        cancellationToken.ThrowIfCancellationRequested();

        return !events.TryAdd(lifecycleEvent.EventId, lifecycleEvent)
            ? throw new InvalidOperationException($"Lifecycle event '{lifecycleEvent.EventId}' already exists.")
            : ValueTask.FromResult(lifecycleEvent);
    }

    /// <inheritdoc />
    public ValueTask<AuditResidueLifecycleEvent?> FindByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        cancellationToken.ThrowIfCancellationRequested();

        _ = events.TryGetValue(eventId.Trim(), out AuditResidueLifecycleEvent? lifecycleEvent);

        return ValueTask.FromResult(lifecycleEvent);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedCorrelationId = correlationId.Trim();

        IReadOnlyList<AuditResidueLifecycleEvent> matches = [.. events.Values
            .Where(lifecycleEvent => string.Equals(lifecycleEvent.CorrelationId, normalizedCorrelationId, StringComparison.Ordinal))
            .OrderBy(lifecycleEvent => lifecycleEvent.OccurredUtc)
            .ThenBy(lifecycleEvent => lifecycleEvent.EventId)];

        return ValueTask.FromResult(matches);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByAuditResidueIdAsync(
        string auditResidueId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditResidueId);
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedAuditResidueId = auditResidueId.Trim();

        IReadOnlyList<AuditResidueLifecycleEvent> matches = [.. events.Values
            .Where(lifecycleEvent => string.Equals(lifecycleEvent.AuditResidueId, normalizedAuditResidueId, StringComparison.Ordinal))
            .OrderBy(lifecycleEvent => lifecycleEvent.OccurredUtc)
            .ThenBy(lifecycleEvent => lifecycleEvent.EventId)];

        return ValueTask.FromResult(matches);
    }
}
