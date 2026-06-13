using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Audit;

/// <summary>
/// Mutation-focused tests for audit residue behavior.
/// </summary>
public sealed class AuditResidueMutationTests
{
    [Fact]
    public void FromDecisionCopiesDecisionTracePolicyMetadataAndDoesNotAliasMetadata()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human(" user-123 ", " Chris ");
        var decision = GovernanceDecision.RequireAcknowledgment(
            "decision.ack.required",
            "Acknowledgment is required.",
            correlationId: " corr-123 ",
            traceId: " trace-456 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ");
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" region "] = " us-la ",
            ["risk"] = " high "
        };

        var residue = AuditResidue.FromDecision(
            actor,
            " document.approve ",
            decision,
            eventId: " event-123 ",
            occurredUtc: new DateTimeOffset(2026, 6, 13, 7, 30, 0, TimeSpan.FromHours(-5)),
            metadata: metadata);

        metadata[" region "] = " changed ";
        metadata["added"] = " later ";

        Assert.Equal("event-123", residue.EventId);
        Assert.Equal(new DateTimeOffset(2026, 6, 13, 12, 30, 0, TimeSpan.Zero), residue.OccurredUtc);
        Assert.Equal("user-123", residue.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, residue.ActorType);
        Assert.Equal("Chris", residue.ActorDisplayName);
        Assert.Equal("document.approve", residue.OperationName);
        Assert.Equal(nameof(GovernanceDecisionOutcome.AcknowledgmentRequired), residue.Outcome);
        Assert.Equal("decision.ack.required", Assert.Single(residue.ReasonCodes));
        Assert.Equal("corr-123", residue.CorrelationId);
        Assert.Equal("trace-456", residue.TraceId);
        Assert.Equal("v1", residue.PolicyVersion);
        Assert.Equal("hash-abc", residue.PolicyHash);
        Assert.Equal("us-la", residue.Metadata["region"]);
        Assert.Equal("high", residue.Metadata["risk"]);
        Assert.False(residue.Metadata.ContainsKey("added"));
    }

    [Fact]
    public void FromConstraintCopiesFullTracePolicyDataAndMetadata()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service(" service-123 ");
        var constraintResult = ConstraintEvaluationResult.Deny(
            "constraint.denied",
            "Constraint denied the operation.");

        var residue = AuditResidue.FromConstraint(
            actor,
            " external.call ",
            constraintResult,
            eventId: " constraint-event-123 ",
            occurredUtc: new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            correlationId: " corr-constraint ",
            traceId: " trace-constraint ",
            policyVersion: " v-constraint ",
            policyHash: " hash-constraint ",
            metadata: new Dictionary<string, string>
            {
                [" source "] = " constraint-test "
            });

        Assert.Equal("constraint-event-123", residue.EventId);
        Assert.Equal("service-123", residue.ActorId);
        Assert.Equal("external.call", residue.OperationName);
        Assert.Equal(nameof(ConstraintEvaluationOutcome.Denied), residue.Outcome);
        Assert.Equal("constraint.denied", Assert.Single(residue.ReasonCodes));
        Assert.Equal("corr-constraint", residue.CorrelationId);
        Assert.Equal("trace-constraint", residue.TraceId);
        Assert.Equal("v-constraint", residue.PolicyVersion);
        Assert.Equal("hash-constraint", residue.PolicyHash);
        Assert.True(residue.HasMetadata);
        Assert.Equal("constraint-test", residue.Metadata["source"]);
    }
}
