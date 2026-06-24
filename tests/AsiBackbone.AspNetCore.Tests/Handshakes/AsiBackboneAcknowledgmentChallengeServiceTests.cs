using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Handshakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Handshakes;

public sealed class AsiBackboneAcknowledgmentChallengeServiceTests
{
    [Fact]
    public void CreateChallengeBuildsHostFriendlyChallengeFromAcknowledgmentDecision()
    {
        var actor = AsiBackboneActorContext.Human(" user-123 ", " Test User ");
        var decision = GovernanceDecision.RequireAcknowledgment(
            "risk.high",
            "Manual acknowledgment is required.",
            correlationId: " correlation-123 ",
            traceId: " trace-123 ",
            policyVersion: " v1 ",
            policyHash: " hash-123 ");
        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentCode = "CONFIRM",
            RequiredAcknowledgmentText = "Confirm responsibility before continuing.",
            RiskLevel = LiabilityHandshakeRiskLevel.High,
            RiskCategory = "content-risk",
            IncludeTraceId = true,
            IncludePolicyMetadata = true,
        };
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService(options);

        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(
            actor,
            " PublishEpisode ",
            decision,
            new Dictionary<string, string>
            {
                [" source "] = " web ",
            });

        Assert.Equal("PublishEpisode", challenge.OperationName);
        Assert.Equal("risk.high", challenge.ReasonCode);
        Assert.Equal("Manual acknowledgment is required.", challenge.ReasonMessage);
        Assert.Equal("CONFIRM", challenge.RequiredAcknowledgmentCode);
        Assert.Equal("Confirm responsibility before continuing.", challenge.RequiredAcknowledgmentText);
        Assert.Equal(LiabilityHandshakeRiskLevel.High, challenge.RiskLevel);
        Assert.Equal("content-risk", challenge.RiskCategory);
        Assert.Equal("correlation-123", challenge.CorrelationId);
        Assert.Equal("trace-123", challenge.TraceId);
        Assert.Equal("v1", challenge.PolicyVersion);
        Assert.Equal("hash-123", challenge.PolicyHash);
        Assert.Equal("web", challenge.Metadata["source"]);
        Assert.Equal(challenge.HandshakeId, challenge.HandshakeRequest.HandshakeId);
    }

    [Fact]
    public void CreateChallengeHidesOptionalDiagnosticFieldsByDefault()
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

        Assert.Equal("Acknowledgment required.", challenge.ReasonMessage);
        Assert.Equal("correlation-123", challenge.CorrelationId);
        Assert.Null(challenge.TraceId);
        Assert.Null(challenge.PolicyVersion);
        Assert.Null(challenge.PolicyHash);
    }

    [Fact]
    public void CreateChallengeCanHideReasonMessage()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Do not expose this.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService(new AsiBackboneAcknowledgmentChallengeOptions
        {
            IncludeReasonMessage = false,
        });

        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);

        Assert.Null(challenge.ReasonMessage);
    }

    [Fact]
    public void CreateChallengeRejectsDecisionThatDoesNotRequireAcknowledgment()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.Allow();
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateChallenge(actor, "RunOperation", decision));

        Assert.Contains("acknowledgment-required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HandleResponseCreatesAcceptedAcknowledgmentWhenResponseMatchesChallenge()
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
            HandshakeId = $" {challenge.HandshakeId} ",
            AcknowledgmentCode = " CONFIRM ",
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
        Assert.Equal("CONFIRM", result.Acknowledgment.AcknowledgmentCode);
        Assert.Equal("correlation-123", result.Acknowledgment.CorrelationId);
        Assert.Equal("trace-123", result.Acknowledgment.TraceId);
        Assert.Equal("minimal-api", result.Acknowledgment.Metadata["transport"]);
        Assert.Equal(occurredUtc, result.Acknowledgment.OccurredUtc);
    }

    [Fact]
    public void HandleResponseCreatesRejectedAcknowledgmentWhenActorDeclines()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = challenge.HandshakeId,
            AcknowledgmentCode = challenge.RequiredAcknowledgmentCode,
            Acknowledged = false,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.True(result.Succeeded);
        Assert.False(result.Acknowledged);
        Assert.True(result.Rejected);
        Assert.NotNull(result.Acknowledgment);
        Assert.True(result.Acknowledgment.Rejected);
    }

    [Fact]
    public void HandleResponseFailsWhenHandshakeIdDoesNotMatch()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = "different-handshake",
            AcknowledgmentCode = challenge.RequiredAcknowledgmentCode,
            Acknowledged = true,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.False(result.Succeeded);
        Assert.Null(result.Acknowledgment);
        Assert.Contains("acknowledgment.challenge.mismatch", result.Result.ReasonCodes);
    }

    [Fact]
    public void HandleResponseFailsWhenAcknowledgmentCodeDoesNotMatch()
    {
        var actor = AsiBackboneActorContext.Human("user-123");
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        AsiBackboneAcknowledgmentChallenge challenge = service.CreateChallenge(actor, "RunOperation", decision);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = challenge.HandshakeId,
            AcknowledgmentCode = "wrong-code",
            Acknowledged = true,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.False(result.Succeeded);
        Assert.Null(result.Acknowledgment);
        Assert.Contains("acknowledgment.challenge.code_mismatch", result.Result.ReasonCodes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChallengeOptionsRejectMissingAcknowledgmentCode(string? code)
    {
        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentCode = code!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("acknowledgment code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultAsiBackboneAcknowledgmentChallengeService CreateService(
        AsiBackboneAcknowledgmentChallengeOptions? options = null)
    {
        return new DefaultAsiBackboneAcknowledgmentChallengeService(
            Options.Create(options ?? new AsiBackboneAcknowledgmentChallengeOptions()));
    }
}
