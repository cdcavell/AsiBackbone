using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Handshakes;
using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Handshakes;

/// <summary>
/// Mutation-focused tests for liability and responsibility handshake behavior.
/// </summary>
public sealed class LiabilityHandshakeMutationTests
{
    [Fact]
    public void FromDecisionUsesFirstDecisionReasonAndPreservesTracePolicyMetadata()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human(" user-123 ", " Chris ");
        OperationReason[] reasons =
        [
            OperationReason.Create("decision.first", "First decision reason."),
            OperationReason.Create("decision.second", "Second decision reason.")
        ];
        var decision = GovernanceDecision.Deny(
            reasons,
            correlationId: " corr-123 ",
            traceId: " trace-456 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ");
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" region "] = " us-la "
        };

        var request = LiabilityHandshakeRequest.FromDecision(
            actor,
            " document.approve ",
            decision,
            " ACK-001 ",
            " I understand this action is consequential. ",
            LiabilityHandshakeRiskLevel.High,
            riskCategory: " administrative ",
            handshakeId: " handshake-123 ",
            metadata: metadata);

        metadata[" region "] = " changed ";
        metadata["other"] = "added";

        Assert.Equal("handshake-123", request.HandshakeId);
        Assert.Equal("user-123", request.ActorId);
        Assert.Equal("document.approve", request.OperationName);
        Assert.Equal("decision.first", request.ReasonCode);
        Assert.Equal("First decision reason.", request.Message);
        Assert.Equal("ACK-001", request.RequiredAcknowledgmentCode);
        Assert.Equal("I understand this action is consequential.", request.RequiredAcknowledgmentText);
        Assert.Equal(LiabilityHandshakeRiskLevel.High, request.RiskLevel);
        Assert.Equal("administrative", request.RiskCategory);
        Assert.Equal("corr-123", request.CorrelationId);
        Assert.Equal("trace-456", request.TraceId);
        Assert.Equal("v1", request.PolicyVersion);
        Assert.Equal("hash-abc", request.PolicyHash);
        Assert.True(request.HasMetadata);
        Assert.Equal("us-la", request.Metadata["region"]);
        Assert.False(request.Metadata.ContainsKey("other"));
    }

    [Fact]
    public void AcknowledgmentUsesRespondingActorAndRequestHandshakeBoundary()
    {
        IAsiBackboneActorContext requestActor = AsiBackboneActorContext.Human("request-user", "Request User");
        var request = LiabilityHandshakeRequest.Create(
            requestActor,
            "document.approve",
            "decision.ack.required",
            "Acknowledgment is required.",
            "ACK-777",
            "I accept responsibility.",
            LiabilityHandshakeRiskLevel.Critical,
            handshakeId: " handshake-777 ",
            correlationId: " corr-777 ",
            traceId: " trace-777 ");
        IAsiBackboneActorContext respondingActor = AsiBackboneActorContext.Service(" service-456 ", " Approval Service ");

        var acknowledgment = LiabilityHandshakeAcknowledgment.Accept(
            request,
            respondingActor,
            acknowledgmentId: " ack-777 ",
            occurredUtc: new DateTimeOffset(2026, 6, 13, 7, 0, 0, TimeSpan.FromHours(-5)));

        Assert.Equal("ack-777", acknowledgment.AcknowledgmentId);
        Assert.Equal("handshake-777", acknowledgment.HandshakeId);
        Assert.Equal("service-456", acknowledgment.ActorId);
        Assert.Equal(AsiBackboneActorType.Service, acknowledgment.ActorType);
        Assert.Equal("Approval Service", acknowledgment.ActorDisplayName);
        Assert.Equal("ACK-777", acknowledgment.AcknowledgmentCode);
        Assert.True(acknowledgment.Acknowledged);
        Assert.False(acknowledgment.Rejected);
        Assert.Equal(new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero), acknowledgment.OccurredUtc);
        Assert.Equal("corr-777", acknowledgment.CorrelationId);
        Assert.Equal("trace-777", acknowledgment.TraceId);
    }
}
