namespace AsiBackbone.Core.Classification;

/// <summary>
/// Represents the host-assigned risk level for an intent that depends on DLP or classification screening.
/// </summary>
public enum DlpIntentRiskLevel
{
    /// <summary>
    /// The intent is low risk and may be allowed with warning when screening is unavailable, depending on policy.
    /// </summary>
    Low = 0,

    /// <summary>
    /// The intent is medium risk and may require acknowledgment, deferral, or escalation when screening is unavailable, depending on policy.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// The intent is high risk, regulated, or consequential enough to fail closed or escalate when screening is unavailable, depending on policy.
    /// </summary>
    High = 2
}
