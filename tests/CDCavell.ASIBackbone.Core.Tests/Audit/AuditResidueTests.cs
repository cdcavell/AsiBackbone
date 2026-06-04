using CDCavell.ASIBackbone.Core.Actors;
using CDCavell.ASIBackbone.Core.Audit;
using CDCavell.ASIBackbone.Core.Constraints;
using CDCavell.ASIBackbone.Core.Decisions;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Audit;

/// <summary>
/// Unit tests for the <see cref="AuditResidue"/> class, which represents the audit information captured from a governance decision or constraint evaluation.
/// </summary>
public sealed class AuditResidueTests
{
    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method correctly stores all required fields and normalizes input values by trimming whitespace.
    /// </summary>
    [Fact]
    public void CreateStoresRequiredFields()
    {
        var actor = AsiBackboneActorContext.Human(" user-123 ", " Chris ");
        DateTimeOffset occurredUtc = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        var residue = AuditResidue.Create(
            actor,
            " document.approve ",
            " Allowed ",
            eventId: " event-123 ",
            occurredUtc: occurredUtc);

        Assert.Equal("event-123", residue.EventId);
        Assert.Equal(occurredUtc, residue.OccurredUtc);
        Assert.Equal("user-123", residue.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, residue.ActorType);
        Assert.Equal("Chris", residue.ActorDisplayName);
        Assert.Equal("document.approve", residue.OperationName);
        Assert.Equal("Allowed", residue.Outcome);
        Assert.Empty(residue.ReasonCodes);
        Assert.False(residue.HasReasonCodes);
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method generates a non-empty EventId when one is not provided, and that it is properly normalized if provided with whitespace.
    /// </summary>
    [Fact]
    public void CreateGeneratesEventIdWhenMissing()
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;

        var residue = AuditResidue.Create(
            actor,
            "system.sync",
            "Allowed");

        Assert.False(string.IsNullOrWhiteSpace(residue.EventId));
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method normalizes reason codes by trimming whitespace and removing empty entries, and that it normalizes trace fields and
    /// </summary>
    [Fact]
    public void CreateNormalizesReasonCodesTraceFieldsAndMetadata()
    {
        var actor = AsiBackboneActorContext.Service(" service-123 ");

        var residue = AuditResidue.Create(
            actor,
            "external.call",
            "Warning",
            reasonCodes: [" risk.high ", "", " policy.warning "],
            eventId: " event-123 ",
            occurredUtc: new DateTimeOffset(2026, 6, 4, 7, 0, 0, TimeSpan.FromHours(-5)),
            correlationId: " correlation-123 ",
            traceId: " trace-456 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ",
            metadata: new Dictionary<string, string>
            {
                [" region "] = " us-la ",
                [" "] = "ignored",
                ["risk"] = " high "
            });

        Assert.Equal(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero), residue.OccurredUtc);
        Assert.Equal(["risk.high", "policy.warning"], residue.ReasonCodes);
        Assert.True(residue.HasReasonCodes);
        Assert.Equal("correlation-123", residue.CorrelationId);
        Assert.Equal("trace-456", residue.TraceId);
        Assert.Equal("v1", residue.PolicyVersion);
        Assert.Equal("hash-abc", residue.PolicyHash);
        Assert.True(residue.HasMetadata);
        Assert.Equal("us-la", residue.Metadata["region"]);
        Assert.Equal("high", residue.Metadata["risk"]);
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method converts whitespace-only trace fields (CorrelationId, TraceId, PolicyVersion, PolicyHash) to null, as they are considered optional and should not be stored as empty strings.
    /// </summary>
    [Fact]
    public void CreateConvertsWhitespaceTraceFieldsToNull()
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;

        var residue = AuditResidue.Create(
            actor,
            "system.sync",
            "Allowed",
            correlationId: " ",
            traceId: "",
            policyVersion: null,
            policyHash: " ");

        Assert.Null(residue.CorrelationId);
        Assert.Null(residue.TraceId);
        Assert.Null(residue.PolicyVersion);
        Assert.Null(residue.PolicyHash);
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method throws an <see cref="ArgumentNullException"/> when the required <c>actor</c> parameter is null.
    /// </summary>
    [Fact]
    public void CreateThrowsForMissingActor()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AuditResidue.Create(
                actor: null!,
                operationName: "document.approve",
                outcome: "Allowed"));
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method throws an <see cref="ArgumentException"/> when the required <paramref name="operationName"/> parameter is null, empty, or whitespace, as an audit residue must have a valid operation name to be meaningful.
    /// </summary>
    /// <param name="operationName">
    /// The invalid operation name value to test, which can be an empty string or a whitespace string. The test will verify that both cases are properly handled by the method and result in an exception being thrown.
    /// </param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateThrowsForMissingOperationName(string operationName)
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;

        _ = Assert.Throws<ArgumentException>(() =>
            AuditResidue.Create(
                actor,
                operationName,
                "Allowed"));
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.Create"/> method throws an <see cref="ArgumentException"/> when the required <paramref name="outcome"/> parameter is null, empty, or whitespace, as an audit residue must have a valid outcome to indicate the result of the operation being audited.
    /// </summary>
    /// <param name="outcome">
    /// The invalid outcome value to test, which can be an empty string or a whitespace string. The test will verify that both cases are properly handled by the method and result in an exception being thrown.
    /// </param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateThrowsForMissingOutcome(string outcome)
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;

        _ = Assert.Throws<ArgumentException>(() =>
            AuditResidue.Create(
                actor,
                "document.approve",
                outcome));
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.FromDecision"/> method correctly copies the outcome, reason codes, correlation ID, trace ID, policy version, and policy hash from the provided <see cref="GovernanceDecision"/> when creating an audit residue, ensuring that all relevant information from the decision is captured in the audit record.
    /// </summary>
    [Fact]
    public void FromDecisionCopiesDecisionOutcomeAndTraceData()
    {
        var actor = AsiBackboneActorContext.Human("user-123", "Chris");
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Policy denied the operation.",
            correlationId: "correlation-123",
            traceId: "trace-456",
            policyVersion: "v1",
            policyHash: "hash-abc");

        var residue = AuditResidue.FromDecision(
            actor,
            "document.approve",
            decision,
            eventId: "event-123",
            occurredUtc: new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("event-123", residue.EventId);
        Assert.Equal("Denied", residue.Outcome);
        Assert.Equal("policy.denied", Assert.Single(residue.ReasonCodes));
        Assert.Equal("correlation-123", residue.CorrelationId);
        Assert.Equal("trace-456", residue.TraceId);
        Assert.Equal("v1", residue.PolicyVersion);
        Assert.Equal("hash-abc", residue.PolicyHash);
    }

    /// <summary>
    /// Verifies that the <see cref="AuditResidue.FromConstraint"/> method correctly copies the outcome, reason codes, correlation ID, and policy version from the provided <see cref="ConstraintEvaluationResult"/> when creating an audit residue, ensuring that all relevant information from the constraint evaluation is captured in the audit record.
    /// </summary>
    [Fact]
    public void FromConstraintCopiesConstraintOutcomeAndReasonCodes()
    {
        var actor = AsiBackboneActorContext.Service("service-123");
        var constraintResult = ConstraintEvaluationResult.Warning(
            "constraint.high_risk",
            "Constraint produced a high-risk warning.");

        var residue = AuditResidue.FromConstraint(
            actor,
            "external.call",
            constraintResult,
            eventId: "event-123",
            occurredUtc: new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero),
            correlationId: "correlation-123",
            policyVersion: "v1");

        Assert.Equal("event-123", residue.EventId);
        Assert.Equal("Warning", residue.Outcome);
        Assert.Equal("constraint.high_risk", Assert.Single(residue.ReasonCodes));
        Assert.Equal("correlation-123", residue.CorrelationId);
        Assert.Equal("v1", residue.PolicyVersion);
    }
}
