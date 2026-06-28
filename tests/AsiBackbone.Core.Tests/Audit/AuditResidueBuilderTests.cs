using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using Xunit;

namespace AsiBackbone.Core.Tests.Audit;

/// <summary>
/// Unit tests for the fluent <see cref="AuditResidueBuilder" /> construction path.
/// </summary>
public sealed class AuditResidueBuilderTests
{
    [Fact]
    public void BuilderCreateMatchesDirectCreateSemantics()
    {
        var actor = AsiBackboneActorContext.Human(" user-123 ", " Chris ");
        DateTimeOffset occurredUtc = new(2026, 6, 26, 10, 30, 0, TimeSpan.FromHours(-5));
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" region "] = " us-la ",
            ["risk"] = " high "
        };

        AuditResidue direct = AuditResidue.Create(
            actor,
            " documents.approve ",
            " Warning ",
            reasonCodes: [" policy.warning ", " latency.high "],
            eventId: " event-123 ",
            occurredUtc: occurredUtc,
            correlationId: " correlation-123 ",
            traceId: " trace-456 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ",
            metadata: metadata,
            auditResidueId: " residue-123 ",
            spanId: " span-789 ",
            parentSpanId: " parent-456 ",
            decisionLatencyMs: 42,
            constraintSetHash: " constraint-hash ",
            constraintCount: 3,
            riskScore: 0.75,
            policyScope: " payments ",
            tenantHash: " tenant-hash ",
            organizationHash: " org-hash ",
            emitterStatus: " queued ",
            emitterProvider: " open-telemetry ",
            outboxSequence: 12,
            gatewayExecutionId: " gateway-123 ",
            decisionStage: " DecisionEvaluated ",
            schemaVersion: " asi.audit.v9 ");

        AuditResidue built = AuditResidueBuilder.Create(actor, " documents.approve ", " Warning ")
            .AddReasonCode(" policy.warning ")
            .AddReasonCode(" latency.high ")
            .WithEventId(" event-123 ")
            .WithOccurredUtc(occurredUtc)
            .WithCorrelationId(" correlation-123 ")
            .WithTraceId(" trace-456 ")
            .WithPolicyVersion(" v1 ")
            .WithPolicyHash(" hash-abc ")
            .WithMetadata(metadata)
            .WithAuditResidueId(" residue-123 ")
            .WithSpanId(" span-789 ")
            .WithParentSpanId(" parent-456 ")
            .WithDecisionLatencyMs(42)
            .WithConstraintSetHash(" constraint-hash ")
            .WithConstraintCount(3)
            .WithRiskScore(0.75)
            .WithPolicyScope(" payments ")
            .WithTenantHash(" tenant-hash ")
            .WithOrganizationHash(" org-hash ")
            .WithEmitterStatus(" queued ")
            .WithEmitterProvider(" open-telemetry ")
            .WithOutboxSequence(12)
            .WithGatewayExecutionId(" gateway-123 ")
            .WithDecisionStage(" DecisionEvaluated ")
            .WithSchemaVersion(" asi.audit.v9 ")
            .Build();

        AssertEquivalentResidue(direct, built);
    }

    [Fact]
    public void BuilderFromDecisionMatchesDirectFromDecisionSemantics()
    {
        var actor = AsiBackboneActorContext.Service("service-123");
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Policy denied the operation.",
            correlationId: "correlation-123",
            traceId: "trace-456",
            policyVersion: "v1",
            policyHash: "hash-abc");
        DateTimeOffset occurredUtc = new(2026, 6, 26, 15, 30, 0, TimeSpan.Zero);
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["source"] = "unit-test"
        };

        AuditResidue direct = AuditResidue.FromDecision(
            actor,
            "system.sync",
            decision,
            eventId: "event-123",
            occurredUtc: occurredUtc,
            metadata: metadata,
            auditResidueId: "residue-123",
            spanId: "span-789",
            parentSpanId: "parent-456",
            decisionLatencyMs: 42,
            constraintSetHash: "constraint-hash",
            constraintCount: 3,
            riskScore: 0.75,
            policyScope: "policy-scope",
            tenantHash: "tenant-hash",
            organizationHash: "org-hash",
            emitterStatus: "queued",
            emitterProvider: "open-telemetry",
            outboxSequence: 12,
            gatewayExecutionId: "gateway-123",
            decisionStage: "DecisionEvaluated",
            schemaVersion: "asi.audit.v9");

        AuditResidue built = AuditResidueBuilder.FromDecision(actor, "system.sync", decision)
            .WithEventId("event-123")
            .WithOccurredUtc(occurredUtc)
            .WithMetadata(metadata)
            .WithAuditResidueId("residue-123")
            .WithSpanId("span-789")
            .WithParentSpanId("parent-456")
            .WithDecisionLatencyMs(42)
            .WithConstraintSetHash("constraint-hash")
            .WithConstraintCount(3)
            .WithRiskScore(0.75)
            .WithPolicyScope("policy-scope")
            .WithTenantHash("tenant-hash")
            .WithOrganizationHash("org-hash")
            .WithEmitterStatus("queued")
            .WithEmitterProvider("open-telemetry")
            .WithOutboxSequence(12)
            .WithGatewayExecutionId("gateway-123")
            .WithDecisionStage("DecisionEvaluated")
            .WithSchemaVersion("asi.audit.v9")
            .Build();

        AssertEquivalentResidue(direct, built);
    }

    [Fact]
    public void BuilderFromConstraintMatchesDirectFromConstraintSemantics()
    {
        var actor = AsiBackboneActorContext.System;
        ConstraintEvaluationResult constraintResult = ConstraintEvaluationResult.Warning(
            "constraint.warning",
            "Constraint produced a warning.");
        DateTimeOffset occurredUtc = new(2026, 6, 26, 15, 30, 0, TimeSpan.Zero);

        AuditResidue direct = AuditResidue.FromConstraint(
            actor,
            "system.sync",
            constraintResult,
            eventId: "event-123",
            occurredUtc: occurredUtc,
            correlationId: "correlation-123",
            traceId: "trace-456",
            policyVersion: "v1",
            policyHash: "hash-abc",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "unit-test"
            });

        AuditResidue built = AuditResidueBuilder.FromConstraint(actor, "system.sync", constraintResult)
            .WithEventId("event-123")
            .WithOccurredUtc(occurredUtc)
            .WithCorrelationId("correlation-123")
            .WithTraceId("trace-456")
            .WithPolicyVersion("v1")
            .WithPolicyHash("hash-abc")
            .AddMetadata("source", "unit-test")
            .Build();

        AssertEquivalentResidue(direct, built);
    }

    [Fact]
    public void BuiltResidueDoesNotChangeWhenBuilderIsReused()
    {
        AuditResidueBuilder builder = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Warning")
            .AddReasonCode("policy.warning")
            .AddMetadata("source", "original");

        AuditResidue first = builder.Build();
        AuditResidue second = builder
            .AddReasonCode("policy.added")
            .AddMetadata("source", "mutated")
            .Build();

        Assert.Equal("policy.warning", Assert.Single(first.ReasonCodes));
        Assert.Equal("original", first.Metadata["source"]);
        Assert.Equal(["policy.warning", "policy.added"], second.ReasonCodes);
        Assert.Equal("mutated", second.Metadata["source"]);
    }

    [Fact]
    public void BuilderThrowsForMissingDecision()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AuditResidueBuilder.FromDecision(
                AsiBackboneActorContext.System,
                "system.sync",
                decision: null!));
    }

    [Fact]
    public void BuilderThrowsForMissingConstraintResult()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AuditResidueBuilder.FromConstraint(
                AsiBackboneActorContext.System,
                "system.sync",
                constraintResult: null!));
    }

    private static void AssertEquivalentResidue(AuditResidue expected, AuditResidue actual)
    {
        Assert.Equal(expected.EventId, actual.EventId);
        Assert.Equal(expected.AuditResidueId, actual.AuditResidueId);
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.OccurredUtc, actual.OccurredUtc);
        Assert.Equal(expected.ActorId, actual.ActorId);
        Assert.Equal(expected.ActorType, actual.ActorType);
        Assert.Equal(expected.ActorDisplayName, actual.ActorDisplayName);
        Assert.Equal(expected.OperationName, actual.OperationName);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.ReasonCodes, actual.ReasonCodes);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.TraceId, actual.TraceId);
        Assert.Equal(expected.SpanId, actual.SpanId);
        Assert.Equal(expected.ParentSpanId, actual.ParentSpanId);
        Assert.Equal(expected.DecisionLatencyMs, actual.DecisionLatencyMs);
        Assert.Equal(expected.ConstraintSetHash, actual.ConstraintSetHash);
        Assert.Equal(expected.ConstraintCount, actual.ConstraintCount);
        Assert.Equal(expected.RiskScore, actual.RiskScore);
        Assert.Equal(expected.PolicyScope, actual.PolicyScope);
        Assert.Equal(expected.TenantHash, actual.TenantHash);
        Assert.Equal(expected.OrganizationHash, actual.OrganizationHash);
        Assert.Equal(expected.EmitterStatus, actual.EmitterStatus);
        Assert.Equal(expected.EmitterProvider, actual.EmitterProvider);
        Assert.Equal(expected.OutboxSequence, actual.OutboxSequence);
        Assert.Equal(expected.GatewayExecutionId, actual.GatewayExecutionId);
        Assert.Equal(expected.DecisionStage, actual.DecisionStage);
        Assert.Equal(expected.PolicyVersion, actual.PolicyVersion);
        Assert.Equal(expected.PolicyHash, actual.PolicyHash);
        Assert.Equal(expected.Metadata, actual.Metadata);
    }
}
