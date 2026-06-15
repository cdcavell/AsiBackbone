namespace CDCavell.AsiBackbone.Core.Classification;

/// <summary>
/// Represents the policy lookup key for risk- and failure-specific screening behavior overrides.
/// </summary>
/// <param name="RiskLevel">The host-assigned intent risk level.</param>
/// <param name="FailureKind">The provider-neutral failure kind.</param>
public readonly record struct DlpFailurePolicyKey(
    DlpIntentRiskLevel RiskLevel,
    DlpClassificationFailureKind FailureKind);
