using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Decisions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Handshakes;

public sealed class AsiBackboneAcknowledgmentChallengeBranchTests
{
    [Fact]
    public void ChallengeResultSuccessRejectsNullAcknowledgment()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AsiBackboneAcknowledgmentChallengeResult.Success(null!));
    }

    [Fact]
    public void ChallengeResultFailureExposesUnacknowledgedStateWithoutAcknowledgment()
    {
        var result = AsiBackboneAcknowledgmentChallengeResult.Failure(
            "ack.failed",
            "Acknowledgment failed.");

        Assert.False(result.Succeeded);
        Assert.False(result.Acknowledged);
        Assert.False(result.Rejected);
        Assert.Null(result.Acknowledgment);
        Assert.Contains("ack.failed", result.Result.ReasonCodes);
    }

    [Fact]
    public void CreateChallengeRejectsNullActor()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");

        _ = Assert.Throws<ArgumentNullException>(() => service.CreateChallenge(null!, "RunOperation", decision));
    }

    [Fact]
    public void CreateChallengeRejectsNullDecision()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");

        _ = Assert.Throws<ArgumentNullException>(() => service.CreateChallenge(actor, "RunOperation", null!));
    }

    [Fact]
    public void HandleResponseRejectsNullChallenge()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        var response = new AsiBackboneAcknowledgmentChallengeRequest();

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(null!, actor, response));
    }

    [Fact]
    public void HandleResponseRejectsNullActor()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);
        var response = new AsiBackboneAcknowledgmentChallengeRequest();

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(challenge, null!, response));
    }

    [Fact]
    public void HandleResponseRejectsNullResponse()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(challenge, actor, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HandleResponseFailsWhenHandshakeIdIsMissingOrBlank(string? handshakeId)
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = handshakeId,
            AcknowledgmentCode = challenge.RequiredAcknowledgmentCode,
            Acknowledged = true,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.False(result.Succeeded);
        Assert.Contains("acknowledgment.challenge.mismatch", result.Result.ReasonCodes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HandleResponseFailsWhenAcknowledgmentCodeIsMissingOrBlank(string? acknowledgmentCode)
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);
        var response = new AsiBackboneAcknowledgmentChallengeRequest
        {
            HandshakeId = challenge.HandshakeId,
            AcknowledgmentCode = acknowledgmentCode,
            Acknowledged = true,
        };

        AsiBackboneAcknowledgmentChallengeResult result = service.HandleResponse(challenge, actor, response);

        Assert.False(result.Succeeded);
        Assert.Contains("acknowledgment.challenge.code_mismatch", result.Result.ReasonCodes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChallengeOptionsRejectMissingAcknowledgmentText(string? text)
    {
        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentText = text!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("acknowledgment text", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServiceConstructorRejectsNullOptions()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new DefaultAsiBackboneAcknowledgmentChallengeService(null!));
    }

    [Fact]
    public void ServiceConstructorRejectsInvalidOptions()
    {
        var options = new AsiBackboneAcknowledgmentChallengeOptions
        {
            RequiredAcknowledgmentCode = " ",
        };

        _ = Assert.Throws<InvalidOperationException>(() =>
            new DefaultAsiBackboneAcknowledgmentChallengeService(Options.Create(options)));
    }

    private static AsiBackboneAcknowledgmentChallenge CreateChallenge(
        DefaultAsiBackboneAcknowledgmentChallengeService service,
        IAsiBackboneActorContext actor)
    {
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");

        return service.CreateChallenge(actor, "RunOperation", decision);
    }

    private static DefaultAsiBackboneAcknowledgmentChallengeService CreateService(
        AsiBackboneAcknowledgmentChallengeOptions? options = null)
    {
        return new DefaultAsiBackboneAcknowledgmentChallengeService(
            Options.Create(options ?? new AsiBackboneAcknowledgmentChallengeOptions()));
    }
}
