using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Decisions;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace AsiBackbone.Core.Tests.Audit;

/// <summary>
/// Focused tests for allocation-sensitive audit residue decision-record creation.
/// </summary>
public sealed class AuditResidueHotPathTests
{
    [Theory]
    [MemberData(nameof(DecisionOutcomeData))]
    public void FromDecisionCopiesDecisionOutcomeTraceAndReasonCodes(
        GovernanceDecision decision,
        string expectedOutcome,
        string[] expectedReasonCodes)
    {
        var actor = AsiBackboneActorContext.Service("benchmark-service");

        var residue = AuditResidue.FromDecision(
            actor,
            "benchmark.operation",
            decision,
            eventId: "benchmark-event",
            occurredUtc: new DateTimeOffset(2026, 6, 30, 18, 0, 0, TimeSpan.Zero));

        Assert.Equal("benchmark-event", residue.EventId);
        Assert.Equal(expectedOutcome, residue.Outcome);
        Assert.Equal(expectedReasonCodes, residue.ReasonCodes);
        Assert.Equal(decision.CorrelationId, residue.CorrelationId);
        Assert.Equal(decision.TraceId, residue.TraceId);
        Assert.Equal(decision.PolicyVersion, residue.PolicyVersion);
        Assert.Equal(decision.PolicyHash, residue.PolicyHash);
    }

    [Fact]
    public void FromDecisionReusesImmutableDecisionReasonCodesForAuditFidelity()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Policy denied the benchmark operation.",
            correlationId: "benchmark-correlation",
            traceId: "benchmark-trace",
            policyVersion: "benchmark-policy-v1",
            policyHash: "benchmark-policy-hash");

        var residue = AuditResidue.FromDecision(
            AsiBackboneActorContext.Service("benchmark-service"),
            "benchmark.operation",
            decision,
            eventId: "benchmark-event");

        Assert.Same(decision.ReasonCodes, residue.ReasonCodes);
        Assert.Equal("policy.denied", Assert.Single(residue.ReasonCodes));
    }

    [Fact]
    public void FromDecisionStillDefensivelyCopiesMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [" source "] = " original ",
            [" "] = "ignored"
        };
        var decision = GovernanceDecision.Warning(
            "policy.warning",
            "Policy produced a warning.");

        var residue = AuditResidue.FromDecision(
            AsiBackboneActorContext.Service("benchmark-service"),
            "benchmark.operation",
            decision,
            metadata: metadata,
            eventId: "benchmark-event");

        metadata[" source "] = " mutated ";
        metadata[" added "] = " later ";

        _ = Assert.Single(residue.Metadata);
        Assert.Equal("original", residue.Metadata["source"]);
        Assert.False(residue.Metadata.ContainsKey("added"));
        ReadOnlyMetadataAssert.CannotMutateThroughCasts(residue.Metadata);
    }

    public static IEnumerable<object[]> DecisionOutcomeData()
    {
        yield return new object[]
        {
            GovernanceDecision.Allow(
                correlationId: "corr-allow",
                traceId: "trace-allow",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "Allowed",
            new string[0]
        };

        yield return new object[]
        {
            GovernanceDecision.Warning(
                "policy.warning",
                "Policy produced a warning.",
                correlationId: "corr-warning",
                traceId: "trace-warning",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "Warning",
            new[] { "policy.warning" }
        };

        yield return new object[]
        {
            GovernanceDecision.Deny(
                "policy.denied",
                "Policy denied the operation.",
                correlationId: "corr-denied",
                traceId: "trace-denied",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "Denied",
            new[] { "policy.denied" }
        };

        yield return new object[]
        {
            GovernanceDecision.Defer(
                "policy.deferred",
                "Policy deferred the operation.",
                correlationId: "corr-deferred",
                traceId: "trace-deferred",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "Deferred",
            new[] { "policy.deferred" }
        };

        yield return new object[]
        {
            GovernanceDecision.RequireAcknowledgment(
                "policy.acknowledgment_required",
                "Acknowledgment is required.",
                correlationId: "corr-ack",
                traceId: "trace-ack",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "AcknowledgmentRequired",
            new[] { "policy.acknowledgment_required" }
        };

        yield return new object[]
        {
            GovernanceDecision.Escalate(
                "policy.escalation_recommended",
                "Escalation is recommended.",
                correlationId: "corr-escalate",
                traceId: "trace-escalate",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),
            "EscalationRecommended",
            new[] { "policy.escalation_recommended" }
        };
    }
}

[Serializable]
public sealed class GovernanceDecision
{
    // existing implementation
}
