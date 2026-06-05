using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Decisions;

/// <summary>
/// Unit tests for the GovernanceDecision class, covering creation of different decision outcomes,
/// </summary>
public sealed class GovernanceDecisionTests
{
    /// <summary>
    /// Verifies that the Allow factory method creates a decision with the Allowed outcome and correct properties.
    /// </summary>
    [Fact]
    public void AllowCreatesAllowedDecision()
    {
        var decision = GovernanceDecision.Allow();

        Assert.Equal(GovernanceDecisionOutcome.Allowed, decision.Outcome);
        Assert.True(decision.CanProceed);
        Assert.True(decision.IsAllowed);
        Assert.False(decision.HasReasons);
        Assert.Empty(decision.Reasons);
        Assert.Empty(decision.ReasonCodes);
    }

    /// <summary>
    /// Verifies that the Deny factory method creates a decision with the Denied outcome, correct reason code, and properties.
    /// </summary>
    [Fact]
    public void DenyCreatesDeniedDecisionWithReasonCode()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Policy denied the operation.");

        Assert.Equal(GovernanceDecisionOutcome.Denied, decision.Outcome);
        Assert.False(decision.CanProceed);
        Assert.True(decision.IsDenied);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("policy.denied", reason.Code);
        Assert.Equal("Policy denied the operation.", reason.Message);
        Assert.Equal("policy.denied", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Deny factory method creates a decision with the Denied outcome and default reason when no reasons are provided.
    /// </summary>
    [Fact]
    public void DenyWithNoReasonsUsesDefaultReason()
    {
        var decision = GovernanceDecision.Deny([]);

        Assert.True(decision.IsDenied);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.denied", reason.Code);
        Assert.Equal("Decision denied the operation.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Warning factory method creates a decision with the Warning outcome, correct reason code, and properties.
    /// </summary>
    [Fact]
    public void WarningCreatesNonBlockingDecisionWithReasonCode()
    {
        var decision = GovernanceDecision.Warning(
            "risk.high",
            "Operation is allowed but high risk.");

        Assert.Equal(GovernanceDecisionOutcome.Warning, decision.Outcome);
        Assert.True(decision.CanProceed);
        Assert.True(decision.IsWarning);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("risk.high", reason.Code);
        Assert.Equal("Operation is allowed but high risk.", reason.Message);
        Assert.Equal("risk.high", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Warning factory method creates a decision with the Warning outcome and default reason when no reasons are provided.
    /// </summary>
    [Fact]
    public void WarningWithNoReasonsUsesDefaultReason()
    {
        var decision = GovernanceDecision.Warning([]);

        Assert.True(decision.IsWarning);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.warning", reason.Code);
        Assert.Equal("Decision produced a warning.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Defer factory method creates a decision with the Deferred outcome and correct reason code.
    /// </summary>
    [Fact]
    public void DeferCreatesDeferredDecision()
    {
        var decision = GovernanceDecision.Defer(
            "decision.waiting_for_policy",
            "Policy data is not available yet.");

        Assert.Equal(GovernanceDecisionOutcome.Deferred, decision.Outcome);
        Assert.False(decision.CanProceed);
        Assert.True(decision.IsDeferred);
        Assert.Equal("decision.waiting_for_policy", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the RequireAcknowledgment factory method creates a decision with the AcknowledgmentRequired outcome, correct reason code, and properties.
    /// </summary>
    [Fact]
    public void RequireAcknowledgmentCreatesAcknowledgmentRequiredDecision()
    {
        var decision = GovernanceDecision.RequireAcknowledgment(
            "ack.required",
            "User acknowledgment is required before execution.");

        Assert.Equal(GovernanceDecisionOutcome.AcknowledgmentRequired, decision.Outcome);
        Assert.False(decision.CanProceed);
        Assert.True(decision.RequiresAcknowledgment);
        Assert.Equal("ack.required", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Escalate factory method creates a decision with the EscalationRecommended outcome, correct reason code, and properties.
    /// </summary>
    [Fact]
    public void EscalateCreatesEscalationRecommendedDecision()
    {
        var decision = GovernanceDecision.Escalate(
            "escalation.required",
            "Supervisor review is required.");

        Assert.Equal(GovernanceDecisionOutcome.EscalationRecommended, decision.Outcome);
        Assert.False(decision.CanProceed);
        Assert.True(decision.EscalationRecommended);
        Assert.Equal("escalation.required", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Allow factory method normalizes trace-related fields by trimming whitespace and converting empty strings to null.
    /// </summary>
    [Fact]
    public void AllowNormalizesTraceFields()
    {
        var decision = GovernanceDecision.Allow(
            correlationId: " correlation-123 ",
            traceId: " trace-456 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ");

        Assert.Equal("correlation-123", decision.CorrelationId);
        Assert.Equal("trace-456", decision.TraceId);
        Assert.Equal("v1", decision.PolicyVersion);
        Assert.Equal("hash-abc", decision.PolicyHash);
    }

    /// <summary>
    /// Verifies that the Allow factory method converts whitespace-only strings for trace-related fields to null.
    /// </summary>
    [Fact]
    public void AllowConvertsWhitespaceTraceFieldsToNull()
    {
        var decision = GovernanceDecision.Allow(
            correlationId: " ",
            traceId: "",
            policyVersion: null,
            policyHash: " ");

        Assert.Null(decision.CorrelationId);
        Assert.Null(decision.TraceId);
        Assert.Null(decision.PolicyVersion);
        Assert.Null(decision.PolicyHash);
    }

    /// <summary>
    /// Verifies that the outcome flags (IsAllowed, IsWarning, IsDenied, IsDeferred, RequiresAcknowledgment, EscalationRecommended) correctly reflect the properties of each governance decision outcome.
    /// </summary>
    [Fact]
    public void OutcomeFlagsReflectEachGovernanceDecisionOutcome()
    {
        var allowed = GovernanceDecision.Allow();

        Assert.True(allowed.CanProceed);
        Assert.True(allowed.IsAllowed);
        Assert.False(allowed.IsWarning);
        Assert.False(allowed.IsDenied);
        Assert.False(allowed.IsDeferred);
        Assert.False(allowed.RequiresAcknowledgment);
        Assert.False(allowed.EscalationRecommended);

        var warning = GovernanceDecision.Warning(
            "decision.warning",
            "Decision produced a warning.");

        Assert.True(warning.CanProceed);
        Assert.False(warning.IsAllowed);
        Assert.True(warning.IsWarning);
        Assert.False(warning.IsDenied);
        Assert.False(warning.IsDeferred);
        Assert.False(warning.RequiresAcknowledgment);
        Assert.False(warning.EscalationRecommended);

        var denied = GovernanceDecision.Deny(
            "decision.denied",
            "Decision denied the operation.");

        Assert.False(denied.CanProceed);
        Assert.False(denied.IsAllowed);
        Assert.False(denied.IsWarning);
        Assert.True(denied.IsDenied);
        Assert.False(denied.IsDeferred);
        Assert.False(denied.RequiresAcknowledgment);
        Assert.False(denied.EscalationRecommended);

        var deferred = GovernanceDecision.Defer(
            "decision.deferred",
            "Decision was deferred.");

        Assert.False(deferred.CanProceed);
        Assert.False(deferred.IsAllowed);
        Assert.False(deferred.IsWarning);
        Assert.False(deferred.IsDenied);
        Assert.True(deferred.IsDeferred);
        Assert.False(deferred.RequiresAcknowledgment);
        Assert.False(deferred.EscalationRecommended);

        var acknowledgmentRequired = GovernanceDecision.RequireAcknowledgment(
            "ack.required",
            "Acknowledgment is required.");

        Assert.False(acknowledgmentRequired.CanProceed);
        Assert.False(acknowledgmentRequired.IsAllowed);
        Assert.False(acknowledgmentRequired.IsWarning);
        Assert.False(acknowledgmentRequired.IsDenied);
        Assert.False(acknowledgmentRequired.IsDeferred);
        Assert.True(acknowledgmentRequired.RequiresAcknowledgment);
        Assert.False(acknowledgmentRequired.EscalationRecommended);

        var escalationRecommended = GovernanceDecision.Escalate(
            "escalation.required",
            "Escalation is required.");

        Assert.False(escalationRecommended.CanProceed);
        Assert.False(escalationRecommended.IsAllowed);
        Assert.False(escalationRecommended.IsWarning);
        Assert.False(escalationRecommended.IsDenied);
        Assert.False(escalationRecommended.IsDeferred);
        Assert.False(escalationRecommended.RequiresAcknowledgment);
        Assert.True(escalationRecommended.EscalationRecommended);
    }

    /// <summary>
    /// Verifies that the Deny factory method creates a decision with the Denied outcome and default reason when null is provided for reasons.
    /// </summary>
    [Fact]
    public void DenyWithNullReasonsUsesDefaultDeniedReason()
    {
        var decision = GovernanceDecision.Deny(
            (IEnumerable<OperationReason>?)null!);

        Assert.True(decision.IsDenied);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.denied", reason.Code);
        Assert.Equal("Decision denied the operation.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Warning factory method creates a decision with the Warning outcome and default reason when null is provided for reasons.
    /// </summary>
    [Fact]
    public void WarningWithNullReasonsUsesDefaultWarningReason()
    {
        var decision = GovernanceDecision.Warning(
            (IEnumerable<OperationReason>?)null!);

        Assert.True(decision.IsWarning);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.warning", reason.Code);
        Assert.Equal("Decision produced a warning.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Deny factory method filters null reason entries while preserving valid reasons.
    /// </summary>
    [Fact]
    public void DenyWithReasonsContainingNullFiltersNullReasons()
    {
        OperationReason[] reasons =
        [
            OperationReason.Create("policy.first", "First policy failure."),
            null!,
            OperationReason.Create("policy.second", "Second policy failure.")
        ];

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);
        Assert.Equal(2, decision.Reasons.Count);
        Assert.Equal(["policy.first", "policy.second"], decision.ReasonCodes);
    }

    /// <summary>
    /// Verifies that the Warning factory method filters null reason entries while preserving valid reasons.
    /// </summary>
    [Fact]
    public void WarningWithReasonsContainingNullFiltersNullReasons()
    {
        OperationReason[] reasons =
        [
            OperationReason.Create("policy.warning", "Policy warning."),
            null!,
            OperationReason.Create("risk.warning", "Risk warning.")
        ];

        var decision = GovernanceDecision.Warning(reasons);

        Assert.True(decision.IsWarning);
        Assert.Equal(2, decision.Reasons.Count);
        Assert.Equal(["policy.warning", "risk.warning"], decision.ReasonCodes);
    }

    /// <summary>
    /// Verifies that the Deny factory method falls back to the default denied reason when all provided reasons are null.
    /// </summary>
    [Fact]
    public void DenyWithOnlyNullReasonsUsesDefaultDeniedReason()
    {
        OperationReason[] reasons = [null!];

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.denied", reason.Code);
        Assert.Equal("Decision denied the operation.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Warning factory method falls back to the default warning reason when all provided reasons are null.
    /// </summary>
    [Fact]
    public void WarningWithOnlyNullReasonsUsesDefaultWarningReason()
    {
        OperationReason[] reasons = [null!];

        var decision = GovernanceDecision.Warning(reasons);

        Assert.True(decision.IsWarning);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.warning", reason.Code);
        Assert.Equal("Decision produced a warning.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Deny factory method throws when the single reason overload receives null.
    /// </summary>
    [Fact]
    public void DenyWithNullReasonThrows()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            GovernanceDecision.Deny((OperationReason)null!));
    }

    /// <summary>
    /// Verifies that the Warning factory method throws when the single reason overload receives null.
    /// </summary>
    [Fact]
    public void WarningWithNullReasonThrows()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            GovernanceDecision.Warning((OperationReason)null!));
    }
}
