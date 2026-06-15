namespace CDCavell.AsiBackbone.Core.Classification;

/// <summary>
/// Represents provider-neutral policy behavior when DLP or classification screening fails or cannot produce a usable result.
/// </summary>
public enum DlpFailureBehavior
{
    /// <summary>
    /// Allow the operation to proceed without adding a warning decision.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Allow the operation to proceed while preserving an audit-worthy warning.
    /// </summary>
    WarnAndAllow = 1,

    /// <summary>
    /// Deny the operation. This is the fail-closed behavior.
    /// </summary>
    Deny = 2,

    /// <summary>
    /// Defer the operation for later evaluation, retry, or review.
    /// </summary>
    Defer = 3,

    /// <summary>
    /// Require user or system acknowledgment before the operation can proceed.
    /// </summary>
    RequireAcknowledgment = 4,

    /// <summary>
    /// Recommend escalation before execution or provider emission.
    /// </summary>
    Escalate = 5
}
