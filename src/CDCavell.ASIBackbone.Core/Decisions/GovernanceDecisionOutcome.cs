namespace CDCavell.ASIBackbone.Core.Decisions;

/// <summary>
/// Represents the outcome selected by a governance decision.
/// </summary>
public enum GovernanceDecisionOutcome
{
    /// <summary>
    /// The operation is allowed to proceed.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The operation is allowed to proceed, but warning reasons should be retained for audit or host presentation.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The operation is denied and should not proceed.
    /// </summary>
    Denied = 2,

    /// <summary>
    /// The operation is deferred and should be evaluated again later or by another process.
    /// </summary>
    Deferred = 3,

    /// <summary>
    /// The operation requires acknowledgment before it may proceed.
    /// </summary>
    AcknowledgmentRequired = 4,

    /// <summary>
    /// The operation should be escalated before execution.
    /// </summary>
    EscalationRecommended = 5
}
