using System.Reflection;
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

        var direct = AuditResidue.Create(
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

        var direct = AuditResidue.FromDecision(
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
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;
        var constraintResult = ConstraintEvaluationResult.Warning(
            "constraint.warning",
            "Constraint produced a warning.");
        DateTimeOffset occurredUtc = new(2026, 6, 26, 15, 30, 0, TimeSpan.Zero);

        var direct = AuditResidue.FromConstraint(
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
    public void BuilderDoesNotAllocateMetadataStorageWhenNoMetadataIsAdded()
    {
        var builder = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed");

        Assert.Null(GetMetadataStorage(builder));

        AuditResidue residue = builder.Build();

        Assert.False(residue.HasMetadata);
        Assert.Empty(residue.Metadata);
        Assert.Null(GetMetadataStorage(builder));
    }

    [Fact]
    public void AddMetadataInitializesStorageAndBuildsSingleEntry()
    {
        var builder = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed");

        AuditResidue residue = builder
            .AddMetadata("source", "unit-test")
            .Build();

        Dictionary<string, string>? storage = GetMetadataStorage(builder);
        Assert.NotNull(storage);
        _ = Assert.Single(storage);
        Assert.Equal("unit-test", residue.Metadata["source"]);
    }

    [Fact]
    public void WithMetadataInitializesStorageAndBuildsMultipleEntries()
    {
        var builder = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed");
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["source"] = "unit-test",
            ["region"] = "us-la"
        };

        AuditResidue residue = builder
            .WithMetadata(metadata)
            .Build();

        Dictionary<string, string>? storage = GetMetadataStorage(builder);
        Assert.NotNull(storage);
        Assert.Equal(2, storage.Count);
        Assert.Equal("unit-test", residue.Metadata["source"]);
        Assert.Equal("us-la", residue.Metadata["region"]);
    }

    [Fact]
    public void AddMetadataOverwritesExistingEntry()
    {
        AuditResidue residue = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed")
            .AddMetadata("source", "original")
            .AddMetadata("source", "updated")
            .Build();

        _ = Assert.Single(residue.Metadata);
        Assert.Equal("updated", residue.Metadata["source"]);
    }

    [Fact]
    public void WithMetadataClearsStorageForNullAndEmptyInputs()
    {
        AuditResidueBuilder builder = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed")
            .AddMetadata("source", "original");

        Assert.NotNull(GetMetadataStorage(builder));

        AuditResidue nullMetadataResidue = builder
            .WithMetadata(null)
            .Build();

        Assert.Null(GetMetadataStorage(builder));
        Assert.Empty(nullMetadataResidue.Metadata);

        AuditResidue emptyMetadataResidue = builder
            .AddMetadata("source", "replacement")
            .WithMetadata(new Dictionary<string, string>(StringComparer.Ordinal))
            .Build();

        Assert.Null(GetMetadataStorage(builder));
        Assert.Empty(emptyMetadataResidue.Metadata);
    }

    [Fact]
    public void BuiltResidueMetadataIsImmutable()
    {
        Dictionary<string, string> sourceMetadata = new(StringComparer.Ordinal)
        {
            ["source"] = "original"
        };

        AuditResidue residue = AuditResidueBuilder.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed")
            .WithMetadata(sourceMetadata)
            .Build();

        sourceMetadata["source"] = "mutated";

        Assert.Equal("original", residue.Metadata["source"]);
        IDictionary<string, string> mutableMetadata = Assert.IsAssignableFrom<IDictionary<string, string>>(residue.Metadata);
        _ = Assert.Throws<NotSupportedException>(() => mutableMetadata["source"] = "mutated");
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

    private static Dictionary<string, string>? GetMetadataStorage(AuditResidueBuilder builder)
    {
        FieldInfo? field = typeof(AuditResidueBuilder).GetField("metadata", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return (Dictionary<string, string>?)field.GetValue(builder);
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
