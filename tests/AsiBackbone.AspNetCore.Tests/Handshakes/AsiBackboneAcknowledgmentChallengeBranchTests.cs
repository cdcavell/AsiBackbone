using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Decisions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Handshakes;

/// <summary>
/// Unit tests for the <see cref="AsiBackboneAcknowledgmentChallenge"/> and related classes.
/// </summary>
public sealed class AsiBackboneAcknowledgmentChallengeBranchTests
{
    /// <summary>
    /// Tests that the <see cref="AsiBackboneAcknowledgmentChallengeResult.Success(LiabilityHandshakeAcknowledgment)"/> method throws an <see cref="ArgumentNullException"/> when a null acknowledgment is provided.
    /// </summary>
    [Fact]
    public void ChallengeResultSuccessRejectsNullAcknowledgment()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AsiBackboneAcknowledgmentChallengeResult.Success(null!));
    }

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAcknowledgmentChallengeResult.Failure(string, string)"/> method correctly exposes the unacknowledged state without an acknowledgment.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.CreateChallenge(IAsiBackboneActorContext, string, GovernanceDecision)"/> method throws an <see cref="ArgumentNullException"/> when a null actor is provided.
    /// </summary>
    [Fact]
    public void CreateChallengeRejectsNullActor()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "Acknowledgment required.");

        _ = Assert.Throws<ArgumentNullException>(() => service.CreateChallenge(null!, "RunOperation", decision));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.CreateChallenge(IAsiBackboneActorContext, string, GovernanceDecision)"/> method throws an <see cref="ArgumentNullException"/> when a null decision is provided.
    /// </summary>
    [Fact]
    public void CreateChallengeRejectsNullDecision()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");

        _ = Assert.Throws<ArgumentNullException>(() => service.CreateChallenge(actor, "RunOperation", null!));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.HandleResponse(AsiBackboneAcknowledgmentChallenge, IAsiBackboneActorContext, AsiBackboneAcknowledgmentChallengeRequest)"/> method throws an <see cref="ArgumentNullException"/> when a null challenge is provided.
    /// </summary>
    [Fact]
    public void HandleResponseRejectsNullChallenge()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        var response = new AsiBackboneAcknowledgmentChallengeRequest();

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(null!, actor, response));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.HandleResponse(AsiBackboneAcknowledgmentChallenge, IAsiBackboneActorContext, AsiBackboneAcknowledgmentChallengeRequest)"/> method throws an <see cref="ArgumentNullException"/> when a null actor is provided.
    /// </summary>
    [Fact]
    public void HandleResponseRejectsNullActor()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);
        var response = new AsiBackboneAcknowledgmentChallengeRequest();

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(challenge, null!, response));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.HandleResponse(AsiBackboneAcknowledgmentChallenge, IAsiBackboneActorContext, AsiBackboneAcknowledgmentChallengeRequest)"/> method throws an <see cref="ArgumentNullException"/> when a null response is provided.
    /// </summary>
    [Fact]
    public void HandleResponseRejectsNullResponse()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");
        AsiBackboneAcknowledgmentChallenge challenge = CreateChallenge(service, actor);

        _ = Assert.Throws<ArgumentNullException>(() => service.HandleResponse(challenge, actor, null!));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.HandleResponse(AsiBackboneAcknowledgmentChallenge, IAsiBackboneActorContext, AsiBackboneAcknowledgmentChallengeRequest)"/> method fails when the handshake ID is missing or blank in the response.
    /// </summary>
    /// <param name="handshakeId">
    /// The handshake ID to test, which can be null, empty, or whitespace.
    /// </param>
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

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService.HandleResponse(AsiBackboneAcknowledgmentChallenge, IAsiBackboneActorContext, AsiBackboneAcknowledgmentChallengeRequest)"/> method fails when the acknowledgment code is missing or blank in the response.
    /// </summary>
    /// <param name="acknowledgmentCode">
    /// The acknowledgment code to test, which can be null, empty, or whitespace.
    /// </param>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAcknowledgmentChallengeOptions.Validate"/> method throws an <see cref="InvalidOperationException"/> when the required acknowledgment text is missing or blank.
    /// </summary>
    /// <param name="text">
    /// The acknowledgment text to test, which can be null, empty, or whitespace.
    /// </param>
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

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService"/> constructor throws an <see cref="ArgumentNullException"/> when null options are provided.
    /// </summary>
    [Fact]
    public void ServiceConstructorRejectsNullOptions()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new DefaultAsiBackboneAcknowledgmentChallengeService(null!));
    }

    /// <summary>
    /// Tests that the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService"/> constructor throws an <see cref="InvalidOperationException"/> when invalid options are provided (e.g., missing required acknowledgment code).
    /// </summary>
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
