namespace CDCavell.AsiBackbone.Core.Emissions;

/// <summary>
/// Defines provider-neutral emission result states.
/// </summary>
public enum GovernanceEmissionStatus
{
    /// <summary>
    /// The emission was accepted locally or by an outbox but has not been delivered downstream yet.
    /// </summary>
    Pending = 100,

    /// <summary>
    /// The emission was delivered successfully.
    /// </summary>
    Delivered = 200,

    /// <summary>
    /// The emission was intentionally deferred and may be retried later according to host policy.
    /// </summary>
    Deferred = 300,

    /// <summary>
    /// The emission failed and the caller or host policy must decide whether retry is appropriate.
    /// </summary>
    Failed = 400,

    /// <summary>
    /// The emission failed in a way that is expected to be retryable.
    /// </summary>
    RetryableFailure = 410,

    /// <summary>
    /// The emission reached a terminal dead-letter state.
    /// </summary>
    DeadLettered = 500
}
