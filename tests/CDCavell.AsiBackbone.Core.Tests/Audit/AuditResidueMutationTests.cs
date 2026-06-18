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

    [Fact]
    public void CreateUsesGeneratedIdentifiersAndEmptyCollectionsWhenOptionalInputsAreMissing()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service(" service-123 ");

        var residue = AuditResidue.Create(actor, "gateway.execute", "Allowed");

        Assert.False(string.IsNullOrWhiteSpace(residue.EventId));
        Assert.Equal(residue.EventId, residue.AuditResidueId);
        Assert.False(residue.HasReasonCodes);
        Assert.False(residue.HasMetadata);
        Assert.Empty(residue.ReasonCodes);
        Assert.Empty(residue.Metadata);
        Assert.Null(residue.ActorDisplayName);
        Assert.Null(residue.CorrelationId);
        Assert.Null(residue.TraceId);
        Assert.Null(residue.PolicyVersion);
        Assert.Null(residue.PolicyHash);
    }

    [Fact]
    public void CreateNormalizesReasonCodesTelemetryAndMetadataBranches()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human(" user-123 ", " Chris ");
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" source "] = " branch-test ",
            [" "] = " ignored ",
            ["nullable"] = null!
        };

        var residue = AuditResidue.Create(
            actor,
            " gateway.execute ",
            " Allowed ",
            reasonCodes: [" reason.beta ", " ", "reason.alpha"],
            eventId: " event-123 ",
            occurredUtc: new DateTimeOffset(2026, 6, 13, 7, 30, 0, TimeSpan.FromHours(-5)),
            correlationId: " ",
            traceId: " trace-123 ",
            policyVersion: " policy-v1 ",
            policyHash: " policy-hash ",
            metadata: metadata,
            auditResidueId: " residue-123 ",
            spanId: " span-123 ",
            parentSpanId: " parent-span-123 ",
            decisionLatencyMs: 0,
            constraintSetHash: " constraint-hash ",
            constraintCount: 0,
            riskScore: 0,
            policyScope: " regional ",
            tenantHash: " tenant-hash ",
            organizationHash: " organization-hash ",
            emitterStatus: " queued ",
            emitterProvider: " provider ",
            outboxSequence: 0,
            gatewayExecutionId: " gateway-123 ",
            decisionStage: " evaluated ");

        Assert.Equal("event-123", residue.EventId);
        Assert.Equal("residue-123", residue.AuditResidueId);
        Assert.Equal("gateway.execute", residue.OperationName);
        Assert.Equal("Allowed", residue.Outcome);
        Assert.True(residue.HasReasonCodes);
        Assert.Collection(
            residue.ReasonCodes,
            reasonCode => Assert.Equal("reason.beta", reasonCode),
            reasonCode => Assert.Equal("reason.alpha", reasonCode));
        Assert.Null(residue.CorrelationId);
        Assert.Equal("trace-123", residue.TraceId);
        Assert.Equal("span-123", residue.SpanId);
        Assert.Equal("parent-span-123", residue.ParentSpanId);
        Assert.Equal(0, residue.DecisionLatencyMs);
        Assert.Equal("constraint-hash", residue.ConstraintSetHash);
        Assert.Equal(0, residue.ConstraintCount);
        Assert.Equal(0, residue.RiskScore);
        Assert.Equal("regional", residue.PolicyScope);
        Assert.Equal("tenant-hash", residue.TenantHash);
        Assert.Equal("organization-hash", residue.OrganizationHash);
        Assert.Equal("queued", residue.EmitterStatus);
        Assert.Equal("provider", residue.EmitterProvider);
        Assert.Equal(0, residue.OutboxSequence);
        Assert.Equal("gateway-123", residue.GatewayExecutionId);
        Assert.Equal("evaluated", residue.DecisionStage);
        Assert.Equal("branch-test", residue.Metadata["source"]);
        Assert.Equal(string.Empty, residue.Metadata["nullable"]);
        Assert.False(residue.Metadata.ContainsKey(string.Empty));
    }

    [Fact]
    public void CreateRejectsNegativeDecisionLatency()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("service-123");

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditResidue.Create(
                actor,
                "gateway.execute",
                "Allowed",
                decisionLatencyMs: -1));
    }

    [Fact]
    public void CreateRejectsNegativeConstraintCount()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("service-123");

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditResidue.Create(
                actor,
                "gateway.execute",
                "Allowed",
                constraintCount: -1));
    }

    [Fact]
    public void CreateRejectsNegativeOutboxSequence()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("service-123");

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditResidue.Create(
                actor,
                "gateway.execute",
                "Allowed",
                outboxSequence: -1));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CreateRejectsInvalidRiskScore(double riskScore)
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("service-123");

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditResidue.Create(
                actor,
                "gateway.execute",
                "Allowed",
                riskScore: riskScore));
    }
}
