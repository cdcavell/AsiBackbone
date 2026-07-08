using AsiBackbone.Core.Emissions;

namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Defines an opt-in claim-capable governance outbox store for coordinated multi-worker emission.
/// </summary>
/// <remarks>
/// This contract is additive to <see cref="IAsiBackboneGovernanceOutboxStore" />. Hosts opt in when scaled workers need to claim a lease before provider emission. Claim support reduces duplicate selection risk for cooperating workers, but it does not create exactly-once provider delivery.
/// </remarks>
public interface IAsiBackboneGovernanceOutboxClaimStore : IAsiBackboneGovernanceOutboxStore
{
    /// <summary>
    /// Claims pending entries ordered for delivery.
    /// </summary>
    ValueTask<IReadOnlyList<GovernanceOutboxClaim>> ClaimPendingAsync(
        GovernanceOutboxClaimRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims retry-ready entries ordered for delivery.
    /// </summary>
    ValueTask<IReadOnlyList<GovernanceOutboxClaim>> ClaimRetryReadyAsync(
        GovernanceOutboxClaimRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a claimed entry as delivered when the claim token still matches.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkClaimDeliveredAsync(
        GovernanceOutboxClaim claim,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a claimed entry as failed or retryable failed when the claim token still matches.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkClaimFailedAsync(
        GovernanceOutboxClaim claim,
        GovernanceEmissionError governanceEmissionError,
        DateTimeOffset? nextRetryUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a claimed entry as dead-lettered when the claim token still matches.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> MarkClaimDeadLetteredAsync(
        GovernanceOutboxClaim claim,
        GovernanceEmissionError governanceEmissionError,
        string? deadLetterReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a claimed entry transition, such as provider-directed deferred state, when the claim token still matches.
    /// </summary>
    ValueTask<GovernanceOutboxEntry> SaveClaimAsync(
        GovernanceOutboxClaim claim,
        GovernanceOutboxEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a claim without changing provider emission state when the claim token still matches.
    /// </summary>
    ValueTask<GovernanceOutboxEntry?> ReleaseClaimAsync(
        GovernanceOutboxClaim claim,
        string? reason = null,
        CancellationToken cancellationToken = default);
}