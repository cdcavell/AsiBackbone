namespace AsiBackbone.Core.Signing;

/// <summary>
/// Describes the host-facing action selected by verification policy.
/// </summary>
public enum VerificationPolicyAction
{
    /// <summary>
    /// Allow the governed operation or high-assurance emission to proceed.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Deny the governed operation or high-assurance emission.
    /// </summary>
    Deny = 1,

    /// <summary>
    /// Defer the workflow until verification can be completed later.
    /// </summary>
    Defer = 2,

    /// <summary>
    /// Require an explicit acknowledgment before proceeding.
    /// </summary>
    RequireAcknowledgment = 3,

    /// <summary>
    /// Escalate to an operator, reviewer, or host-defined governance process.
    /// </summary>
    Escalate = 4,

    /// <summary>
    /// Retry verification or downstream handling according to host retry policy.
    /// </summary>
    Retry = 5,

    /// <summary>
    /// Move the record or emission request to dead-letter handling.
    /// </summary>
    DeadLetter = 6
}
