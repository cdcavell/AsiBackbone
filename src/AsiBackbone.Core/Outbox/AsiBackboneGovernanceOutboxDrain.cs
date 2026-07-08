using AsiBackbone.Core.Emissions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
/// <param name="logger">The logger used to record local operational diagnostics for drain failures.</param>
/// <param name="outboxOptions">The provider-neutral retry timing options used by the drain.</param>
public sealed class AsiBackboneGovernanceOutboxDrain(
    IAsiBackboneGovernanceOutboxStore outboxStore,
    IAsiBackboneGovernanceEmitter emitter,
    ILogger<AsiBackboneGovernanceOutboxDrain>? logger = null,
    IOptions<AsiBackboneGovernanceOutboxOptions>? outboxOptions = null)
{
    private static readonly Action<ILogger, string, int, string, DateTimeOffset, string?, string?, Exception?> LogGovernanceEmissionException = LoggerMessage.Define<string, int, string, DateTimeOffset, string?, string?>(
        LogLevel.Warning,
        new EventId(19701, nameof(LogGovernanceEmissionException)),
        "Governance outbox emission threw an exception for outbox entry {OutboxEntryId} on attempt {AttemptCount}. Emitter provider: {EmitterProvider}. Next retry UTC: {NextRetryUtc}. Correlation ID: {CorrelationId}. Audit residue ID: {AuditResidueId}.");

    private readonly IAsiBackboneGovernanceOutboxStore outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
    private readonly IAsiBackboneGovernanceEmitter emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
    private readonly ILogger<AsiBackboneGovernanceOutboxDrain> logger = logger ?? NullLogger<AsiBackboneGovernanceOutboxDrain>.Instance;
    private readonly AsiBackboneGovernanceOutboxOptions retryOptions = ResolveOptions(outboxOptions);

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

        if (retryOptions.UseClaimLeases)
        {
            return await DrainClaimedAsync(drainUtc, maxCount, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<GovernanceOutboxEntry> pendingEntries = await outboxStore
            .FindPendingAsync(maxCount, cancellationToken)
            .ConfigureAwait(false);

        if (pendingEntries.Count >= maxCount)
        {
            return await DrainEntriesAsync(pendingEntries, drainUtc, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<GovernanceOutboxEntry> retryReadyEntries = await outboxStore
            .FindRetryReadyAsync(drainUtc, maxCount - pendingEntries.Count, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<GovernanceOutboxEntry> entriesToDrain = MergeEntries(pendingEntries, retryReadyEntries, maxCount);
        return await DrainEntriesAsync(entriesToDrain, drainUtc, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> DrainClaimedAsync(
        DateTimeOffset drainUtc,
        int maxCount,
        CancellationToken cancellationToken)
    {
        if (outboxStore is not IAsiBackboneGovernanceOutboxClaimStore claimStore)
        {
            throw new InvalidOperationException("Claim leases are enabled, but the configured outbox store does not implement IAsiBackboneGovernanceOutboxClaimStore.");
        }

        string workerId = retryOptions.ClaimWorkerId ?? throw new InvalidOperationException("ClaimWorkerId is required when claim leases are enabled.");
        var pendingRequest = GovernanceOutboxClaimRequest.Create(
            workerId,
            drainUtc,
            retryOptions.ClaimLeaseDuration,
            maxCount);
        IReadOnlyList<GovernanceOutboxClaim> pendingClaims = await claimStore
            .ClaimPendingAsync(pendingRequest, cancellationToken)
            .ConfigureAwait(false);

        if (pendingClaims.Count >= maxCount)
        {
            return await DrainClaimsAsync(claimStore, pendingClaims, drainUtc, cancellationToken).ConfigureAwait(false);
        }

        var retryRequest = GovernanceOutboxClaimRequest.Create(
            workerId,
            drainUtc,
            retryOptions.ClaimLeaseDuration,
            maxCount - pendingClaims.Count);
        IReadOnlyList<GovernanceOutboxClaim> retryReadyClaims = await claimStore
            .ClaimRetryReadyAsync(retryRequest, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<GovernanceOutboxClaim> claimsToDrain = MergeClaims(pendingClaims, retryReadyClaims, maxCount);
        return await DrainClaimsAsync(claimStore, claimsToDrain, drainUtc, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> DrainEntriesAsync(
        IReadOnlyList<GovernanceOutboxEntry> entriesToDrain,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        if (entriesToDrain.Count == 0)
        {
            return Array.Empty<GovernanceOutboxEntry>();
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

    private async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> DrainClaimsAsync(
        IAsiBackboneGovernanceOutboxClaimStore claimStore,
        IReadOnlyList<GovernanceOutboxClaim> claimsToDrain,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        if (claimsToDrain.Count == 0)
        {
            return Array.Empty<GovernanceOutboxEntry>();
        }

        List<GovernanceOutboxEntry> updatedEntries = new(claimsToDrain.Count);

        foreach (GovernanceOutboxClaim claim in claimsToDrain)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GovernanceOutboxEntry updatedEntry = await DrainClaimAsync(claimStore, claim, drainUtc, cancellationToken).ConfigureAwait(false);
            updatedEntries.Add(updatedEntry);
        }

        return updatedEntries;
    }

    private static IReadOnlyList<GovernanceOutboxEntry> MergeEntries(
        IReadOnlyList<GovernanceOutboxEntry> pendingEntries,
        IReadOnlyList<GovernanceOutboxEntry> retryReadyEntries,
        int maxCount)
    {
        if (pendingEntries.Count == 0)
        {
            return retryReadyEntries;
        }

        if (retryReadyEntries.Count == 0)
        {
            return pendingEntries;
        }

        var entriesToDrain = new List<GovernanceOutboxEntry>(Math.Min(maxCount, pendingEntries.Count + retryReadyEntries.Count));
        var existingEntryIds = new HashSet<string>(pendingEntries.Count + retryReadyEntries.Count, StringComparer.Ordinal);

        foreach (GovernanceOutboxEntry pendingEntry in pendingEntries)
        {
            _ = existingEntryIds.Add(pendingEntry.OutboxEntryId);
            entriesToDrain.Add(pendingEntry);
        }

        foreach (GovernanceOutboxEntry retryReadyEntry in retryReadyEntries)
        {
            if (entriesToDrain.Count >= maxCount)
            {
                break;
            }

            if (existingEntryIds.Add(retryReadyEntry.OutboxEntryId))
            {
                entriesToDrain.Add(retryReadyEntry);
            }
        }

        return entriesToDrain;
    }

    private static IReadOnlyList<GovernanceOutboxClaim> MergeClaims(
        IReadOnlyList<GovernanceOutboxClaim> pendingClaims,
        IReadOnlyList<GovernanceOutboxClaim> retryReadyClaims,
        int maxCount)
    {
        if (pendingClaims.Count == 0)
        {
            return retryReadyClaims;
        }

        if (retryReadyClaims.Count == 0)
        {
            return pendingClaims;
        }

        var claimsToDrain = new List<GovernanceOutboxClaim>(Math.Min(maxCount, pendingClaims.Count + retryReadyClaims.Count));
        var existingEntryIds = new HashSet<string>(pendingClaims.Count + retryReadyClaims.Count, StringComparer.Ordinal);

        foreach (GovernanceOutboxClaim pendingClaim in pendingClaims)
        {
            _ = existingEntryIds.Add(pendingClaim.OutboxEntryId);
            claimsToDrain.Add(pendingClaim);
        }

        foreach (GovernanceOutboxClaim retryReadyClaim in retryReadyClaims)
        {
            if (claimsToDrain.Count >= maxCount)
            {
                break;
            }

            if (existingEntryIds.Add(retryReadyClaim.OutboxEntryId))
            {
                claimsToDrain.Add(retryReadyClaim);
            }
        }

        return claimsToDrain;
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
            DateTimeOffset nextRetryUtc = GetRetryUtc(drainUtc);
            LogEmissionException(entry, nextRetryUtc, ex);
            GovernanceEmissionError governanceEmissionError = CreateExceptionError(ex);

            return await outboxStore.MarkFailedAsync(
                entry.OutboxEntryId,
                governanceEmissionError,
                nextRetryUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }

        return await ApplyEmissionResultAsync(entry, result, drainUtc, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<GovernanceOutboxEntry> DrainClaimAsync(
        IAsiBackboneGovernanceOutboxClaimStore claimStore,
        GovernanceOutboxClaim claim,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        GovernanceEmissionResult result;

        try
        {
            result = await emitter.EmitAsync(claim.Entry.Envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            DateTimeOffset nextRetryUtc = GetRetryUtc(drainUtc);
            LogEmissionException(claim.Entry, nextRetryUtc, ex);
            GovernanceEmissionError governanceEmissionError = CreateExceptionError(ex);

            return await claimStore.MarkClaimFailedAsync(
                claim,
                governanceEmissionError,
                nextRetryUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }

        return await ApplyEmissionResultAsync(claimStore, claim, result, drainUtc, cancellationToken).ConfigureAwait(false);
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
            return await outboxStore.MarkDeliveredAsync(entry.OutboxEntryId, result, cancellationToken).ConfigureAwait(false);
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
                result.RetryAfterUtc ?? GetDeferredUtc(drainUtc),
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

    private async ValueTask<GovernanceOutboxEntry> ApplyEmissionResultAsync(
        IAsiBackboneGovernanceOutboxClaimStore claimStore,
        GovernanceOutboxClaim claim,
        GovernanceEmissionResult result,
        DateTimeOffset drainUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return await claimStore.MarkClaimDeliveredAsync(claim, result, cancellationToken).ConfigureAwait(false);
        }

        if (result.Status is GovernanceEmissionStatus.DeadLettered)
        {
            GovernanceEmissionError governanceEmissionError = result.Error ?? GovernanceEmissionError.Create(
                "emission.deadlettered",
                "Governance emission returned a dead-lettered result.",
                providerName: result.ProviderName);

            return await claimStore.MarkClaimDeadLetteredAsync(
                claim,
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

            GovernanceOutboxEntry deferredEntry = claim.Entry.MarkDeferred(
                governanceEmissionError,
                result.RetryAfterUtc ?? GetDeferredUtc(drainUtc),
                drainUtc);

            return await claimStore.SaveClaimAsync(claim, deferredEntry, cancellationToken).ConfigureAwait(false);
        }

        GovernanceEmissionError failure = result.Error ?? GovernanceEmissionError.Create(
            "emission.failed",
            "Governance emission returned a failed result without provider-neutral error details.",
            isRetryable: result.ShouldRetry,
            providerName: result.ProviderName);

        return await claimStore.MarkClaimFailedAsync(
            claim,
            failure,
            result.RetryAfterUtc,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private void LogEmissionException(GovernanceOutboxEntry entry, DateTimeOffset nextRetryUtc, Exception exception)
    {
        LogGovernanceEmissionException(
            logger,
            entry.OutboxEntryId,
            entry.RetryCount + 1,
            ResolveEmitterProvider(entry),
            nextRetryUtc,
            entry.Envelope.CorrelationId,
            entry.Envelope.AuditResidueId,
            exception);
    }

    private static GovernanceEmissionError CreateExceptionError(Exception exception)
    {
        return GovernanceEmissionError.Create(
            "emission.exception",
            $"Governance emission threw {exception.GetType().Name} during outbox drain.",
            isRetryable: true,
            providerErrorCode: exception.GetType().FullName);
    }

    private DateTimeOffset GetRetryUtc(DateTimeOffset drainUtc)
    {
        return drainUtc.Add(retryOptions.RetryDelay);
    }

    private DateTimeOffset GetDeferredUtc(DateTimeOffset drainUtc)
    {
        return drainUtc.Add(retryOptions.DeferredDelay);
    }

    private static AsiBackboneGovernanceOutboxOptions ResolveOptions(IOptions<AsiBackboneGovernanceOutboxOptions>? options)
    {
        AsiBackboneGovernanceOutboxOptions resolved = options?.Value ?? new AsiBackboneGovernanceOutboxOptions();
        resolved.Validate();

        return new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = resolved.RetryDelay,
            DeferredDelay = resolved.DeferredDelay,
            UseClaimLeases = resolved.UseClaimLeases,
            ClaimWorkerId = string.IsNullOrWhiteSpace(resolved.ClaimWorkerId) ? null : resolved.ClaimWorkerId.Trim(),
            ClaimLeaseDuration = resolved.ClaimLeaseDuration
        };
    }

    private static string ResolveEmitterProvider(GovernanceOutboxEntry entry)
    {
        return entry.Envelope.EmitterProvider ?? entry.ProviderName ?? "unspecified";
    }
}
