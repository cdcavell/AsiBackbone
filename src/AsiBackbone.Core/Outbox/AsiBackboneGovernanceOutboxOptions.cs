namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Provides provider-neutral retry timing and optional claim/lease options for governance outbox drain processing.
/// </summary>
/// <remarks>
/// These options control default retry timestamps when a downstream emitter does not provide its own retry-after value. Claim leasing remains opt-in and requires a claim-capable outbox store.
/// </remarks>
public sealed class AsiBackboneGovernanceOutboxOptions
{
    /// <summary>
    /// Gets or sets the default delay applied after a transient emission failure or unexpected emitter exception.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the default delay applied when an emitter returns a pending or deferred result without a retry-after timestamp.
    /// </summary>
    public TimeSpan DeferredDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether the drain should claim outbox entries before provider emission when the store supports claim leases.
    /// </summary>
    public bool UseClaimLeases { get; set; }

    /// <summary>
    /// Gets or sets the worker, process, node, or partition identifier used when <see cref="UseClaimLeases" /> is enabled.
    /// </summary>
    public string? ClaimWorkerId { get; set; }

    /// <summary>
    /// Gets or sets the lease duration used when <see cref="UseClaimLeases" /> is enabled.
    /// </summary>
    public TimeSpan ClaimLeaseDuration { get; set; } = GovernanceOutboxClaimRequest.DefaultLeaseDuration;

    /// <summary>
    /// Validates the configured outbox timing and claim options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a delay is negative or a claim lease option is invalid.</exception>
    public void Validate()
    {
        ValidateDelay(RetryDelay, nameof(RetryDelay));
        ValidateDelay(DeferredDelay, nameof(DeferredDelay));

        if (ClaimLeaseDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ClaimLeaseDuration)} must be greater than TimeSpan.Zero.");
        }

        if (UseClaimLeases && string.IsNullOrWhiteSpace(ClaimWorkerId))
        {
            throw new InvalidOperationException($"{nameof(ClaimWorkerId)} is required when {nameof(UseClaimLeases)} is enabled.");
        }
    }

    private static void ValidateDelay(TimeSpan delay, string propertyName)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{propertyName} must be greater than or equal to TimeSpan.Zero.");
        }
    }
}
