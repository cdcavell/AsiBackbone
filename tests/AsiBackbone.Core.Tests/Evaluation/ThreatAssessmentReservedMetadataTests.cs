using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.ThreatModeling;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Verifies that contributor metadata cannot replace framework-generated threat provenance.
/// </summary>
public sealed class ThreatAssessmentReservedMetadataTests
{
    /// <summary>
    /// Verifies that a contributor cannot override the evaluator-selected effective outcome.
    /// </summary>
    [Fact]
    public void ThreatAssessmentRejectsEffectiveOutcomeMetadataOverride()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.High,
            ThreatCategories.PolicyBypassAttempt,
            "threat.policy_bypass_attempt",
            "A policy bypass attempt was reported.",
            GovernanceDecisionOutcome.Denied,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["threat.effective_outcome"] = GovernanceDecisionOutcome.Allowed.ToString()
            }));

        Assert.Equal("metadata", exception.ParamName);
        Assert.Contains("threat.", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the complete threat namespace is reserved regardless of casing or surrounding whitespace.
    /// </summary>
    [Theory]
    [InlineData("threat.category")]
    [InlineData("threat.severity")]
    [InlineData("threat.recommended_outcome")]
    [InlineData("threat.effective_outcome")]
    [InlineData("threat.confidence")]
    [InlineData("threat.contributor")]
    [InlineData("threat.custom_framework_field")]
    [InlineData(" THREAT.CATEGORY ")]
    public void ThreatAssessmentRejectsReservedThreatMetadataKeys(string metadataKey)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Medium,
            ThreatCategories.AuditIntegrityRisk,
            "threat.audit_integrity_risk",
            "An audit integrity risk was reported.",
            GovernanceDecisionOutcome.Deferred,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [metadataKey] = "untrusted-value"
            }));

        Assert.Equal("metadata", exception.ParamName);
    }

    /// <summary>
    /// Verifies that non-conflicting contributor metadata remains normalized and retained.
    /// </summary>
    [Fact]
    public void ThreatAssessmentRetainsNonReservedContributorMetadata()
    {
        ThreatAssessment assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.input_malformed",
            "Malformed input was reported.",
            GovernanceDecisionOutcome.Warning,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [" input.field "] = " request.intent "
            });

        var reason = assessment.ToOperationReason(
            "input-shape-contributor",
            GovernanceDecisionOutcome.Warning);

        Assert.Equal("request.intent", reason.Metadata["input.field"]);
        Assert.Equal("input-shape-contributor", reason.Metadata["threat.contributor"]);
        Assert.Equal(ThreatCategories.InputMalformed, reason.Metadata["threat.category"]);
        Assert.Equal(ThreatSeverity.Low.ToString(), reason.Metadata["threat.severity"]);
        Assert.Equal(GovernanceDecisionOutcome.Warning.ToString(), reason.Metadata["threat.recommended_outcome"]);
        Assert.Equal(GovernanceDecisionOutcome.Warning.ToString(), reason.Metadata["threat.effective_outcome"]);
    }
}
