using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Decisions;

/// <summary>
/// Mutation-focused tests for governance decision behavior.
/// </summary>
public sealed class GovernanceDecisionMutationTests
{
    [Fact]
    public void NonAllowDecisionFactoriesPreserveTraceFieldsAndReasonMessages()
    {
        AssertDecision(
            GovernanceDecision.Deny(
                "decision.denied.custom",
                "Denied for test coverage.",
                correlationId: " corr-denied ",
                traceId: " trace-denied ",
                policyVersion: " v-denied ",
                policyHash: " hash-denied "),
            GovernanceDecisionOutcome.Denied,
            "decision.denied.custom",
            "Denied for test coverage.",
            "corr-denied",
            "trace-denied",
            "v-denied",
            "hash-denied",
            expectedCanProceed: false);

        AssertDecision(
            GovernanceDecision.Warning(
                "decision.warning.custom",
                "Warning for test coverage.",
                correlationId: " corr-warning ",
                traceId: " trace-warning ",
                policyVersion: " v-warning ",
                policyHash: " hash-warning "),
            GovernanceDecisionOutcome.Warning,
            "decision.warning.custom",
            "Warning for test coverage.",
            "corr-warning",
            "trace-warning",
            "v-warning",
            "hash-warning",
            expectedCanProceed: true);

        AssertDecision(
            GovernanceDecision.Defer(
                "decision.deferred.custom",
                "Deferred for test coverage.",
                correlationId: " corr-deferred ",
                traceId: " trace-deferred ",
                policyVersion: " v-deferred ",
                policyHash: " hash-deferred "),
            GovernanceDecisionOutcome.Deferred,
            "decision.deferred.custom",
            "Deferred for test coverage.",
            "corr-deferred",
            "trace-deferred",
            "v-deferred",
            "hash-deferred",
            expectedCanProceed: false);

        AssertDecision(
            GovernanceDecision.RequireAcknowledgment(
                "decision.ack.custom",
                "Acknowledgment required for test coverage.",
                correlationId: " corr-ack ",
                traceId: " trace-ack ",
                policyVersion: " v-ack ",
                policyHash: " hash-ack "),
            GovernanceDecisionOutcome.AcknowledgmentRequired,
            "decision.ack.custom",
            "Acknowledgment required for test coverage.",
            "corr-ack",
            "trace-ack",
            "v-ack",
            "hash-ack",
            expectedCanProceed: false);

        AssertDecision(
            GovernanceDecision.Escalate(
                "decision.escalate.custom",
                "Escalation required for test coverage.",
                correlationId: " corr-escalate ",
                traceId: " trace-escalate ",
                policyVersion: " v-escalate ",
                policyHash: " hash-escalate "),
            GovernanceDecisionOutcome.EscalationRecommended,
            "decision.escalate.custom",
            "Escalation required for test coverage.",
            "corr-escalate",
            "trace-escalate",
            "v-escalate",
            "hash-escalate",
            expectedCanProceed: false);
    }

    [Fact]
    public void DenyReasonCollectionsAreReadOnlySnapshots()
    {
        List<OperationReason> sourceReasons =
        [
            OperationReason.Create("policy.first", "First policy failure."),
            OperationReason.Create("policy.second", "Second policy failure.")
        ];

        var decision = GovernanceDecision.Deny(sourceReasons);

        sourceReasons.Add(OperationReason.Create("policy.third", "Third policy failure."));

        Assert.Equal(["policy.first", "policy.second"], decision.ReasonCodes);
        Assert.Equal(2, decision.Reasons.Count);

        IList<OperationReason> reasonView = Assert.IsAssignableFrom<IList<OperationReason>>(decision.Reasons);
        _ = Assert.Throws<NotSupportedException>(() =>
            reasonView.Add(OperationReason.Create("policy.add", "Add was blocked.")));

        IList<string> codeView = Assert.IsAssignableFrom<IList<string>>(decision.ReasonCodes);
        _ = Assert.Throws<NotSupportedException>(() =>
            codeView.Add("policy.add"));
    }

    [Fact]
    public void WarningReasonCollectionsAreReadOnlySnapshots()
    {
        List<OperationReason> sourceReasons =
        [
            OperationReason.Create("warning.first", "First warning."),
            OperationReason.Create("warning.second", "Second warning.")
        ];

        var decision = GovernanceDecision.Warning(sourceReasons);

        sourceReasons.Clear();

        Assert.True(decision.IsWarning);
        Assert.Equal(["warning.first", "warning.second"], decision.ReasonCodes);
        Assert.Equal(2, decision.Reasons.Count);
    }

    private static void AssertDecision(
        GovernanceDecision decision,
        GovernanceDecisionOutcome expectedOutcome,
        string expectedCode,
        string expectedMessage,
        string expectedCorrelationId,
        string expectedTraceId,
        string expectedPolicyVersion,
        string expectedPolicyHash,
        bool expectedCanProceed)
    {
        Assert.Equal(expectedOutcome, decision.Outcome);
        Assert.Equal(expectedCanProceed, decision.CanProceed);
        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal(expectedCode, reason.Code);
        Assert.Equal(expectedMessage, reason.Message);
        Assert.Equal(expectedCode, Assert.Single(decision.ReasonCodes));
        Assert.Equal(expectedCorrelationId, decision.CorrelationId);
        Assert.Equal(expectedTraceId, decision.TraceId);
        Assert.Equal(expectedPolicyVersion, decision.PolicyVersion);
        Assert.Equal(expectedPolicyHash, decision.PolicyHash);
        Assert.True(decision.HasReasons);
    }
}
