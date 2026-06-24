using AsiBackbone.Core.Emissions;

namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Drains provider-neutral governance outbox entries through a configured governance emitter.
/// </summary>
/// <remarks>
/// This drain path is provider-neutral. It is suitable for tests, samples, local validation, and host-owned workers that need to hand persisted outbox entries to an optional downstream emitter without coupling Core to a provider SDK.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="AsiBackboneGovernanceOutboxDrain" /> class.
/// </remarks>
/// <param name="outboxStore">The provider-neutral outbox store.</param>
/// <param name="emitter">The provider-neutral governance emitter.</param>
public sealed class AsiBackboneGovernanceOutboxDrain(
    IAsiBackboneGovernanceOutboxStore outboxStore,
    IAsiBackboneGovernanceEmitter emitter)
{
    private readonly IAsiBackboneGovernanceOutboxStore outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
    private readonly IAsiBackboneGovernanceEmitter emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));

    /// <summary>
    /// Drains pending and retry-ready outbox entries through the configured emitter.
    /// </summary>
    /// <param name="utcNow">The UTC timestamp used for retry-ready checks.</param>
    /// <param name="maxCount">The maximum number of entries to drain.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated outbox entries that were attempted by the drain.</returns>
    public async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> DrainAsync(
        DateTimeOffset? utcNow = null,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Maximum count must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset drainUtc = (utcNow ?? DateTimeOffset.UtcNow).ToUniversalTime();
        IReadOnlyList<GovernanceOutboxEntry> pendingEntries = await outboxStore
            .FindPendingAsync(maxCount, cancellationToken)
            .ConfigureAwait(false);

        List<GovernanceOutboxEntry> entriesToDrain = [.. pendingEntries];

        if (entriesToDrain.Count < maxCount)
        {
            IReadOnlyList<GovernanceOutboxEntry> retryReadyEntries = await outboxStore
                .FindRetryReadyAsync(drainUtc, maxCount - entriesToDrain.Count, cancellationToken)
                .ConfigureAwait(false);

            HashSet<string> existingEntryIds = new(
                entriesToDrain.Select(entry => entry.OutboxEntryId),
                StringComparer.Ordinal);

            foreach (GovernanceOutboxEntry retryReadyEntry in retryReadyEntries)
            {
                if (existingEntryIds.Add(retryReadyEntry.OutboxEntryId))
                {
                    entriesToDrain.Add(retryReadyEntry);
                }
            }
        }

        List<GovernanceOutboxEntry> updatedEntries = new(entriesToDrain.Count);

        foreach (GovernanceOutboxEntry entry in entriesToDrain)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GovernanceOutboxEntry updatedEntry = await DrainEntryAsync(entry, drainUtc, cancellationToken).ConfigureAwait(false);
            updatedEntries.Add(updatedEntry);
        }

        return updatedEntries;
    }

    private async ValueTask<GovernanceOutboxEntry> DrainEntryAsync(
        GovernanceOutboxEntry entry,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        GovernanceEmissionResult result;

        try
        {
            result = await emitter.EmitAsync(entry.Envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var governanceEmissionError = GovernanceEmissionError.Create(
                "emission.exception",
                $"Governance emission threw {ex.GetType().Name} during outbox drain.",
                isRetryable: true,
                providerErrorCode: ex.GetType().FullName);

            return await outboxStore.MarkFailedAsync(
                entry.OutboxEntryId,
                governanceEmissionError,
                nextRetryUtc: drainUtc.AddMinutes(1),
                cancellationToken)
                .ConfigureAwait(false);
        }

        return await ApplyEmissionResultAsync(entry, result, drainUtc, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<GovernanceOutboxEntry> ApplyEmissionResultAsync(
        GovernanceOutboxEntry entry,
        GovernanceEmissionResult result,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return await outboxStore.MarkDeliveredAsync(
                entry.OutboxEntryId,
                result,
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (result.Status is GovernanceEmissionStatus.DeadLettered)
        {
            GovernanceEmissionError governanceEmissionError = result.Error ?? GovernanceEmissionError.Create(
                "emission.deadlettered",
                "Governance emission returned a dead-lettered result.",
                providerName: result.ProviderName);

            return await outboxStore.MarkDeadLetteredAsync(
                entry.OutboxEntryId,
                governanceEmissionError,
                governanceEmissionError.Message,
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (result.Status is GovernanceEmissionStatus.Deferred or GovernanceEmissionStatus.Pending)
        {
            GovernanceEmissionError? governanceEmissionError = result.Error ?? (result.Status is GovernanceEmissionStatus.Pending
                ? GovernanceEmissionError.Create(
                    "emission.pending",
                    "Governance emission remained pending after the outbox drain attempt.",
                    isRetryable: true,
                    providerName: result.ProviderName)
                : null);

            GovernanceOutboxEntry deferredEntry = entry.MarkDeferred(
                governanceEmissionError,
                result.RetryAfterUtc ?? drainUtc.AddMinutes(1),
                drainUtc);

            return await outboxStore.SaveAsync(deferredEntry, cancellationToken).ConfigureAwait(false);
        }

        GovernanceEmissionError failure = result.Error ?? GovernanceEmissionError.Create(
            "emission.failed",
            "Governance emission returned a failed result without provider-neutral error details.",
            isRetryable: result.ShouldRetry,
            providerName: result.ProviderName);

        return await outboxStore.MarkFailedAsync(
            entry.OutboxEntryId,
            failure,
            result.RetryAfterUtc,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
