using CDCavell.ASIBackbone.Core.Decisions;
using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Decisions;

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
}
