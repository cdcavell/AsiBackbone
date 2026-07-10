namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Provides provider-neutral retry timing, poison-message, and optional claim/lease options for governance outbox drain processing.
/// </summary>
/// <remarks>
/// These options control default retry timestamps when a downstream emitter does not provide its own retry-after value. Claim leasing remains opt-in and requires a claim-capable outbox store.
/// </remarks>
public sealed class AsiBackboneGovernanceOutboxOptions
{
    /// <summary>
    /// Gets the stable default reason code used when the drain dead-letters an entry after the configured retry threshold is reached.
    /// </summary>
    public const string DefaultDeadLetterReasonCode = "outbox.max_retry_attempts_exceeded";

    /// <summary>
    /// Gets the stable default reason message used when the drain dead-letters an entry after the configured retry threshold is reached.
    /// </summary>
    public const string DefaultDeadLetterReasonMessage = "Governance outbox entry exceeded the configured maximum retry attempts.";

    /// <summary>
    /// Gets or sets the default delay applied after a transient emission failure or unexpected emitter exception.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the default delay applied when an emitter returns a pending or deferred result without a retry-after timestamp.
    /// </summary>
    public TimeSpan DeferredDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum number of failed emission attempts permitted before the drain applies its poison-message policy.
    /// </summary>
    /// <remarks>
    /// The threshold counts the failure currently being processed. A value of <c>1</c> dead-letters the first failed attempt when <see cref="DeadLetterOnMaxRetryAttempts" /> is enabled.
    /// </remarks>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether entries are dead-lettered when <see cref="MaxRetryAttempts" /> is reached.
    /// </summary>
    public bool DeadLetterOnMaxRetryAttempts { get; set; } = true;

    /// <summary>
    /// Gets or sets the provider-neutral reason code recorded when the configured retry threshold is reached.
    /// </summary>
    public string DeadLetterReasonCode { get; set; } = DefaultDeadLetterReasonCode;

    /// <summary>
    /// Gets or sets the provider-neutral reason message recorded when the configured retry threshold is reached.
    /// </summary>
    public string DeadLetterReasonMessage { get; set; } = DefaultDeadLetterReasonMessage;

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
    /// Validates the configured outbox retry, poison-message, timing, and claim options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a retry threshold, reason, delay, or claim lease option is invalid.</exception>
    public void Validate()
    {
        ValidateDelay(RetryDelay, nameof(RetryDelay));
        ValidateDelay(DeferredDelay, nameof(DeferredDelay));

        if (MaxRetryAttempts <= 0)
        {
            throw new InvalidOperationException($"{nameof(MaxRetryAttempts)} must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(DeadLetterReasonCode))
        {
            throw new InvalidOperationException($"{nameof(DeadLetterReasonCode)} is required.");
        }

        if (string.IsNullOrWhiteSpace(DeadLetterReasonMessage))
        {
            throw new InvalidOperationException($"{nameof(DeadLetterReasonMessage)} is required.");
        }

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
