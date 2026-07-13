namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Describes whether a claimed outbox transition was applied by the current invocation.
/// </summary>
public enum GovernanceOutboxClaimTransitionOutcome
{
    /// <summary>
    /// The caller-owned transition was persisted successfully.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// The supplied claim no longer owns the durable entry.
    /// </summary>
    StaleClaim = 1,

    /// <summary>
    /// The durable entry was already terminal before the requested transition was applied.
    /// </summary>
    Terminal = 2,

    /// <summary>
    /// A concurrent durable writer won while this invocation attempted to persist the transition.
    /// </summary>
    ConcurrencyLost = 3,

    /// <summary>
    /// The durable entry was missing before or after the attempted transition.
    /// </summary>
    Missing = 4
}
