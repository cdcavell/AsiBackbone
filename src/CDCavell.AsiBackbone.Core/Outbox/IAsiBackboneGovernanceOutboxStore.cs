using CDCavell.AsiBackbone.Core.Emissions;

namespace CDCavell.AsiBackbone.Core.Outbox;

/// <summary>
/// Defines a provider-neutral durable outbox store for governance emission envelopes.
/// </summary>
/// <remarks>
/// The provider-neutral find methods return delivery candidates. They do not imply cross-process row claiming,
/// leasing, skip-locked selection, or exactly-once provider delivery. Hosts that run multiple drain workers against
/// the same durable store must add host-owned claiming, partitioning, or provider-side idempotency.
/// </remarks>
public interface IAsiBackboneGovernanceOutboxStore
{
    /// <summary>
    /// Enqueues a provider-neutral governance emission envelope before optional downstream provider delivery is attempted.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> EnqueueAsync(
        GovernanceEmissionEnvelope envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an updated outbox entry state.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> SaveAsync(
        GovernanceOutboxEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an outbox entry by its stable identifier.
    /// </summary>
    ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(
        string outboxEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds pending outbox entries ordered for delivery.
    /// </summary>
    /// <remarks>
    /// Returned entries are candidates, not claimed work items. A multi-worker host should use a storage adapter or
    /// deployment pattern that claims or partitions work before provider emission.
    /// </remarks>
    ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds retry-ready outbox entries ordered for delivery.
    /// </summary>
    /// <remarks>
    /// Returned entries are candidates, not claimed work items. A multi-worker host should use a storage adapter or
    /// deployment pattern that claims or partitions work before provider emission.
    /// </remarks>
    ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(
        DateTimeOffset utcNow,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox entry as delivered using a provider-neutral emission result.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
        string outboxEntryId,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox entry as failed or retryable failed using provider-neutral error information.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        DateTimeOffset? nextRetryUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox entry as dead-lettered.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        string? deadLetterReason = null,
        CancellationToken cancellationToken = default);
}
