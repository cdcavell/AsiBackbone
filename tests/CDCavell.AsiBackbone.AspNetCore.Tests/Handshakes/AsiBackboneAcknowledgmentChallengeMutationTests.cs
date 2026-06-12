using CDCavell.AsiBackbone.AspNetCore.Handshakes;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Handshakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Handshakes;

public sealed class AsiBackboneAcknowledgmentChallengeMutationTests
{
    [Fact]
    public void DefaultChallengeKeepsTraceAndPolicyMetadataOutOfHostFacingShape()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment(
            "ack.required",
            "Acknowledgment required.",
            correlationId: "correlation-123",
            traceId: "trace-123",
            policyVersion: "v1",
            policyHash: "hash-123");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();

        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);

        Assert.Equal("ack.required", challenge.ReasonCode);
        Assert.Equal("correlation-123", challenge.CorrelationId);
        Assert.Null(challenge.TraceId);
        Assert.Null(challenge.PolicyVersion);
        Assert.Null(challenge.PolicyHash);
        Assert.Equal("trace-123", challenge.HandshakeRequest.TraceId);
        Assert.Equal("v1", challenge.HandshakeRequest.PolicyVersion);
        Assert.Equal("hash-123", challenge.HandshakeRequest.PolicyHash);
    }

    [Fact]
    public void HiddenReasonMessageStillStaysInsideCoreHandshakeRequest()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Do not expose this.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService(new AsiBackboneAcknowledgmentChallengeOptions
        {
            IncludeReasonMessage = false,
        });

        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);

        Assert.Null(challenge.ReasonMessage);
        Assert.Equal("Do not expose this.", challenge.HandshakeRequest.Message);
    }

    [Fact]
    public void AcceptedResponseCopiesActorAndResponseMetadataIntoCoreAcknowledgment()
    {
        var actor = AsiBackboneActorContext.Human("user-123", "Test User");
        var decision = GovernanceDecision.RequireAcknowledgment(
            "ack.required",
            "Acknowledgment required.",
            correlationId: "correlation-123",
            traceId: "trace-123");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService(new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentCode = "CONFIRM",
        });
        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = challenge.HandshakeId,
            AcknowledgmentCode = "CONFIRM",
            Acknowledged = true,
            Metadata = new Dictionary<string, string>
            {
                ["transport"] = "minimal-api",
            },
        };
        DateTimeOffset occurredUtc = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response, occurredUtc);

        Assert.True(result.Succeeded);
        Assert.True(result.Acknowledged);
        Assert.False(result.Rejected);
        Assert.NotNull(result.Acknowledgment);
        Assert.Equal(challenge.HandshakeId, result.Acknowledgment.HandshakeId);
        Assert.Equal("user-123", result.Acknowledgment.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, result.Acknowledgment.ActorType);
        Assert.Equal("Test User", result.Acknowledgment.ActorDisplayName);
        Assert.Equal("CONFIRM", result.Acknowledgment.AcknowledgmentCode);
        Assert.Equal("correlation-123", result.Acknowledgment.CorrelationId);
        Assert.Equal("trace-123", result.Acknowledgment.TraceId);
        Assert.Equal("minimal-api", result.Acknowledgment.Metadata["transport"]);
        Assert.Equal(occurredUtc, result.Acknowledgment.OccurredUtc);
    }

    [Fact]
    public void DeclinedResponseStillRequiresMatchingAcknowledgmentCode()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = challenge.HandshakeId,
            AcknowledgmentCode = "wrong-code",
            Acknowledged = false,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.False(result.Succeeded);
        Assert.False(result.Acknowledged);
        Assert.False(result.Rejected);
        Assert.Null(result.Acknowledgment);
        Assert.Contains("acknowledgment.challenge.code_mismatch", result.Result.ReasonCodes);
    }

    [Fact]
    public void FromHandshakeRequestNormalizesMetadataKeysAndValues()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        var request = LiabilityHandshakeRequest.FromDecision(
            actor,
            "RunOperation",
            decision,
            "CONFIRM",
            "Confirm responsibility.",
            LiabilityHandshakeRiskLevel.Medium,
            metadata: new Dictionary<string, string>
            {
                [" source "] = " web ",
                ["   "] = "ignored",
                ["empty-value"] = "   "
            });

        var challenge =
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(request);

        Assert.Equal(2, challenge.Metadata.Count);
        Assert.Equal("web", challenge.Metadata["source"]);
        Assert.Equal(string.Empty, challenge.Metadata["empty-value"]);
        Assert.False(challenge.Metadata.ContainsKey("   "));
    }

    [Fact]
    public void FromHandshakeRequestUsesEmptyMetadataWhenMetadataIsNullOrEmpty()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        var request = LiabilityHandshakeRequest.FromDecision(
            actor,
            "RunOperation",
            decision,
            "CONFIRM",
            "Confirm responsibility.",
            LiabilityHandshakeRiskLevel.Medium);

        var challenge =
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(request);

        Assert.Empty(challenge.Metadata);
    }

    [Fact]
    public void FromHandshakeRequestIncludesOptionalTraceAndPolicyMetadataWhenEnabled()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment(
            "ack.required",
            "Acknowledgment required.",
            correlationId: "correlation-123",
            traceId: "trace-123",
            policyVersion: "v1",
            policyHash: "hash-123");

        var request = LiabilityHandshakeRequest.FromDecision(
            actor,
            "RunOperation",
            decision,
            "CONFIRM",
            "Confirm responsibility.",
            LiabilityHandshakeRiskLevel.Medium);

        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            IncludeTraceId = true,
            IncludePolicyMetadata = true
        };

        var challenge =
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(request, options);

        Assert.Equal("trace-123", challenge.TraceId);
        Assert.Equal("v1", challenge.PolicyVersion);
        Assert.Equal("hash-123", challenge.PolicyHash);
    }

    [Fact]
    public void FromHandshakeRequestRejectsNullRequest()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(null!));
    }

    [Fact]
    public void FromHandshakeRequestRejectsInvalidOptions()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        var request = LiabilityHandshakeRequest.FromDecision(
            actor,
            "RunOperation",
            decision,
            "CONFIRM",
            "Confirm responsibility.",
            LiabilityHandshakeRiskLevel.Medium);

        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(() =>
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(request, options));
    }

    private static DefaultAsiBackboneAcknowledgmentChallengeService CreateService(
        AsiBackboneAcknowledgmentChallengeOptions? options = null)
    {
        return new DefaultAsiBackboneAcknowledgmentChallengeService(
            Options.Create(options ?? new AsiBackboneAcknowledgmentChallengeOptions()));
    }
}
