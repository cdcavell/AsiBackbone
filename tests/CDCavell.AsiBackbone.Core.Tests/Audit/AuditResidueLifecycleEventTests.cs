using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using Xunit;

namespace AsiBackbone.Core.Tests.Audit;

/// <summary>
/// Unit tests for audit residue lifecycle stages and events.
/// </summary>
public sealed class AuditResidueLifecycleEventTests
{
    /// <summary>
    /// Verifies that lifecycle stages expose stable provider-neutral sequence values.
    /// </summary>
    [Fact]
    public void LifecycleStagesExposeStableProviderNeutralSequenceValues()
    {
        Assert.Equal(100, (int)AuditResidueLifecycleStage.DecisionEvaluated);
        Assert.Equal(200, (int)AuditResidueLifecycleStage.AcknowledgmentRequested);
        Assert.Equal(210, (int)AuditResidueLifecycleStage.AcknowledgmentCompleted);
        Assert.Equal(300, (int)AuditResidueLifecycleStage.CapabilityTokenIssued);
        Assert.Equal(400, (int)AuditResidueLifecycleStage.GatewayExecutionStarted);
        Assert.Equal(410, (int)AuditResidueLifecycleStage.GatewayExecutionCompleted);
        Assert.Equal(420, (int)AuditResidueLifecycleStage.GatewayExecutionDenied);
        Assert.Equal(500, (int)AuditResidueLifecycleStage.ExternalEmissionQueued);
        Assert.Equal(510, (int)AuditResidueLifecycleStage.ExternalEmissionDelivered);
        Assert.Equal(520, (int)AuditResidueLifecycleStage.ExternalEmissionFailed);
        Assert.Equal(530, (int)AuditResidueLifecycleStage.ExternalEmissionDeadLettered);
    }

    /// <summary>
    /// Verifies that a lifecycle event stores and normalizes required correlation fields.
    /// </summary>
    [Fact]
    public void CreateStoresLifecycleStageAndCorrelationFields()
    {
        DateTimeOffset occurredUtc = new(2026, 6, 13, 8, 30, 0, TimeSpan.FromHours(-5));

        var lifecycleEvent = AuditResidueLifecycleEvent.Create(
            AuditResidueLifecycleStage.AcknowledgmentRequested,
            " correlation-123 ",
            auditResidueId: " residue-456 ",
            eventId: " lifecycle-789 ",
            occurredUtc: occurredUtc,
            traceId: " trace-abc ",
            operationName: " document.approve ",
            outcome: " RequireAcknowledgment ",
            metadata: new Dictionary<string, string>
            {
                [" stageSource "] = " unit-test ",
                [" "] = "ignored"
            });

        Assert.Equal("lifecycle-789", lifecycleEvent.EventId);
        Assert.Equal(AuditResidueLifecycleStage.AcknowledgmentRequested, lifecycleEvent.Stage);
        Assert.Equal(200, lifecycleEvent.StageSequence);
        Assert.Equal(new DateTimeOffset(2026, 6, 13, 13, 30, 0, TimeSpan.Zero), lifecycleEvent.OccurredUtc);
        Assert.Equal("correlation-123", lifecycleEvent.CorrelationId);
        Assert.Equal("residue-456", lifecycleEvent.AuditResidueId);
        Assert.True(lifecycleEvent.HasAuditResidueId);
        Assert.Equal("trace-abc", lifecycleEvent.TraceId);
        Assert.Equal("document.approve", lifecycleEvent.OperationName);
        Assert.Equal("RequireAcknowledgment", lifecycleEvent.Outcome);
        Assert.True(lifecycleEvent.HasMetadata);
        Assert.Equal("unit-test", lifecycleEvent.Metadata["stageSource"]);
    }

    /// <summary>
    /// Verifies that lifecycle events can be correlated to original decision residue without rewriting that original residue.
    /// </summary>
    [Fact]
    public void FromResidueCopiesDecisionContextWithoutRewritingOriginalResidue()
    {
        var residue = AuditResidue.Create(
            AsiBackboneActorContext.Human("user-123", "Chris"),
            "document.approve",
            "RequireAcknowledgment",
            reasonCodes: ["ack.required"],
            eventId: "audit-residue-123",
            correlationId: "correlation-123",
            traceId: "trace-456",
            metadata: new Dictionary<string, string>
            {
                [" workflow "] = " document-approval "
            });

        var lifecycleEvent = AuditResidueLifecycleEvent.FromResidue(
            AuditResidueLifecycleStage.AcknowledgmentCompleted,
            residue,
            eventId: "lifecycle-event-123",
            outcome: "Acknowledged",
            metadata: new Dictionary<string, string>
            {
                [" acknowledgmentId "] = " ack-123 "
            });

        Assert.Equal("lifecycle-event-123", lifecycleEvent.EventId);
        Assert.Equal(AuditResidueLifecycleStage.AcknowledgmentCompleted, lifecycleEvent.Stage);
        Assert.Equal("correlation-123", lifecycleEvent.CorrelationId);
        Assert.Equal("audit-residue-123", lifecycleEvent.AuditResidueId);
        Assert.Equal("trace-456", lifecycleEvent.TraceId);
        Assert.Equal("document.approve", lifecycleEvent.OperationName);
        Assert.Equal("Acknowledged", lifecycleEvent.Outcome);
        Assert.Equal("document-approval", lifecycleEvent.Metadata["workflow"]);
        Assert.Equal("ack-123", lifecycleEvent.Metadata["acknowledgmentId"]);

        Assert.Equal("RequireAcknowledgment", residue.Outcome);
        Assert.Equal("ack.required", Assert.Single(residue.ReasonCodes));
        Assert.Equal("document-approval", residue.Metadata["workflow"]);
        Assert.False(residue.Metadata.ContainsKey("acknowledgmentId"));
    }

    /// <summary>
    /// Verifies that lifecycle events can represent partial progress across acknowledgment, token issuance, gateway execution, and external emission stages.
    /// </summary>
    [Fact]
    public void LifecycleEventsRepresentPartialProgressAsSeparateEvents()
    {
        const string correlationId = "correlation-123";
        const string auditResidueId = "audit-residue-123";

        AuditResidueLifecycleStage[] stages =
        [
            AuditResidueLifecycleStage.DecisionEvaluated,
            AuditResidueLifecycleStage.AcknowledgmentRequested,
            AuditResidueLifecycleStage.AcknowledgmentCompleted,
            AuditResidueLifecycleStage.CapabilityTokenIssued,
            AuditResidueLifecycleStage.GatewayExecutionStarted,
            AuditResidueLifecycleStage.GatewayExecutionCompleted,
            AuditResidueLifecycleStage.GatewayExecutionDenied,
            AuditResidueLifecycleStage.ExternalEmissionQueued,
            AuditResidueLifecycleStage.ExternalEmissionDelivered,
            AuditResidueLifecycleStage.ExternalEmissionFailed,
            AuditResidueLifecycleStage.ExternalEmissionDeadLettered
        ];

        AuditResidueLifecycleEvent[] lifecycleEvents = [.. stages.Select(stage =>
            AuditResidueLifecycleEvent.Create(
                stage,
                correlationId,
                auditResidueId,
                eventId: $"lifecycle-{(int)stage}"))];

        Assert.Equal(stages.Length, lifecycleEvents.Select(lifecycleEvent => lifecycleEvent.EventId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(lifecycleEvents, lifecycleEvent => Assert.Equal(correlationId, lifecycleEvent.CorrelationId));
        Assert.All(lifecycleEvents, lifecycleEvent => Assert.Equal(auditResidueId, lifecycleEvent.AuditResidueId));
        Assert.Equal(stages.Select(stage => (int)stage), lifecycleEvents.Select(lifecycleEvent => lifecycleEvent.StageSequence));
    }

    /// <summary>
    /// Verifies that lifecycle event creation requires a correlation identifier.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateThrowsForMissingCorrelationId(string? correlationId)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            AuditResidueLifecycleEvent.Create(
                AuditResidueLifecycleStage.DecisionEvaluated,
                correlationId!));
    }

    /// <summary>
    /// Verifies that lifecycle event creation rejects undefined lifecycle stages.
    /// </summary>
    [Fact]
    public void CreateThrowsForUndefinedLifecycleStage()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditResidueLifecycleEvent.Create(
                (AuditResidueLifecycleStage)9999,
                "correlation-123"));
    }

    /// <summary>
    /// Verifies that creating a lifecycle event from residue requires either residue correlation or an explicit correlation override.
    /// </summary>
    [Fact]
    public void FromResidueRequiresCorrelationWhenResidueDoesNotContainOne()
    {
        var residue = AuditResidue.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed",
            eventId: "audit-residue-123");

        _ = Assert.Throws<ArgumentException>(() =>
            AuditResidueLifecycleEvent.FromResidue(
                AuditResidueLifecycleStage.DecisionEvaluated,
                residue));

        var lifecycleEvent = AuditResidueLifecycleEvent.FromResidue(
            AuditResidueLifecycleStage.DecisionEvaluated,
            residue,
            correlationId: "correlation-override");

        Assert.Equal("correlation-override", lifecycleEvent.CorrelationId);
        Assert.Equal("audit-residue-123", lifecycleEvent.AuditResidueId);
    }
}
