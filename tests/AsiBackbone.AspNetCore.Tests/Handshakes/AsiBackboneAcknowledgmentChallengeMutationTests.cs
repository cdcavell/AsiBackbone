using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Handshakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Handshakes;

/// <summary>
/// Unit tests for the <see cref="AsiBackboneAcknowledgmentChallenge"/> class and related functionality.
/// </summary>
public sealed class AsiBackboneAcknowledgmentChallengeMutationTests
{
    /// <summary>
    /// Tests that the default acknowledgment challenge service correctly creates a challenge while keeping trace and policy metadata out of the host-facing shape, ensuring that sensitive information is not exposed in the public API response.
    /// </summary>
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

    /// <summary>
    /// Tests that when the acknowledgment challenge service is configured to exclude the reason message, the reason message is not included in the public-facing challenge object, but it still remains accessible within the internal handshake request for auditing or logging purposes.
    /// </summary>
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

    /// <summary>
    /// Tests that accepted responses correctly copy actor and response metadata into the core acknowledgment object.
    /// </summary>
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

    /// <summary>
    /// Tests that a declined acknowledgment response still requires the correct acknowledgment code to be provided, and if the code does not match, the response is rejected with an appropriate reason code indicating a mismatch.
    /// </summary>
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

    /// <summary>
    /// Tests that when creating an acknowledgment challenge from a handshake request, the metadata keys and values are normalized by trimming whitespace and ignoring empty keys or values, ensuring that the resulting metadata dictionary is clean and consistent.
    /// </summary>
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

    /// <summary>
    /// Tests that when creating an acknowledgment challenge from a handshake request with null or empty metadata, the resulting challenge has an empty metadata dictionary, ensuring that no null references or unexpected values are present in the challenge's metadata.
    /// </summary>
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

    /// <summary>
    /// Tests that when creating an acknowledgment challenge from a handshake request with optional trace and policy metadata enabled, the resulting challenge includes the trace ID, policy version, and policy hash from the handshake request, ensuring that this information is preserved in the challenge for auditing or tracking purposes.
    /// </summary>
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

    /// <summary>
    /// Tests that when creating an acknowledgment challenge from a handshake request with null input, an ArgumentNullException is thrown, ensuring that the method correctly validates its input parameters and does not allow null requests to be processed.
    /// </summary>
    [Fact]
    public void FromHandshakeRequestRejectsNullRequest()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(null!));
    }

    /// <summary>
    /// Tests that when creating an acknowledgment challenge from a handshake request with invalid options (e.g., a required acknowledgment code that is null or whitespace), an InvalidOperationException is thrown, ensuring that the method enforces valid configuration and does not allow challenges to be created with invalid parameters.
    /// </summary>
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

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a null handshake request, an ArgumentNullException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow null requests to be processed.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullHandshakeRequest()
    {
        LiabilityHandshakeRequest request = CreateHandshakeRequest();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new AsiBackboneAcknowledgmentChallenge(
                null!,
                request.HandshakeId,
                request.OperationName,
                request.ReasonCode,
                request.Message,
                request.RequiredAcknowledgmentCode,
                request.RequiredAcknowledgmentText,
                request.RiskLevel,
                request.RiskCategory,
                request.CorrelationId,
                request.TraceId,
                request.PolicyVersion,
                request.PolicyHash,
                request.Metadata));

        Assert.Equal("handshakeRequest", exception.ParamName);
    }

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a missing handshake ID (null or whitespace), an ArgumentException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow challenges to be created without a valid handshake ID.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMissingHandshakeId()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateChallengeUsingInternalConstructor(handshakeId: " "));

        Assert.Equal("handshakeId", exception.ParamName);
    }

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a missing operation name (null or whitespace), an ArgumentException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow challenges to be created without a valid operation name.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMissingOperationName()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateChallengeUsingInternalConstructor(operationName: " "));

        Assert.Equal("operationName", exception.ParamName);
    }

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a missing reason code (null or whitespace), an ArgumentException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow challenges to be created without a valid reason code.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMissingReasonCode()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateChallengeUsingInternalConstructor(reasonCode: " "));

        Assert.Equal("reasonCode", exception.ParamName);
    }

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a missing required acknowledgment code (null or whitespace), an ArgumentException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow challenges to be created without a valid required acknowledgment code.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMissingRequiredAcknowledgmentCode()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateChallengeUsingInternalConstructor(requiredAcknowledgmentCode: " "));

        Assert.Equal("requiredAcknowledgmentCode", exception.ParamName);
    }

    /// <summary>
    /// Tests that when constructing an acknowledgment challenge with a missing required acknowledgment text (null or whitespace), an ArgumentException is thrown, ensuring that the constructor correctly validates its input parameters and does not allow challenges to be created without a valid required acknowledgment text.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMissingRequiredAcknowledgmentText()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateChallengeUsingInternalConstructor(requiredAcknowledgmentText: " "));

        Assert.Equal("requiredAcknowledgmentText", exception.ParamName);
    }

    /// <summary>
    /// Tests that when creating an acknowledgment challenge with a null actor before a decision is made, an ArgumentNullException is thrown, ensuring that the service correctly validates its input parameters and does not allow challenges to be created without a valid actor context.
    /// </summary>
    [Fact]
    public void CreateChallengeRejectsNullActorBeforeDecision()
    {
        DefaultAsiBackboneAcknowledgmentChallengeService service = CreateService();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            service.CreateChallenge(null!, "RunOperation", null!));

        Assert.Equal("actor", exception.ParamName);
    }

    private static AsiBackboneAcknowledgmentChallenge CreateChallengeUsingInternalConstructor(
        string? handshakeId = null,
        string? operationName = null,
        string? reasonCode = null,
        string? requiredAcknowledgmentCode = null,
        string? requiredAcknowledgmentText = null)
    {
        LiabilityHandshakeRequest request = CreateHandshakeRequest();

        return new AsiBackboneAcknowledgmentChallenge(
            request,
            handshakeId ?? request.HandshakeId,
            operationName ?? request.OperationName,
            reasonCode ?? request.ReasonCode,
            request.Message,
            requiredAcknowledgmentCode ?? request.RequiredAcknowledgmentCode,
            requiredAcknowledgmentText ?? request.RequiredAcknowledgmentText,
            request.RiskLevel,
            request.RiskCategory,
            request.CorrelationId,
            request.TraceId,
            request.PolicyVersion,
            request.PolicyHash,
            request.Metadata);
    }

    private static LiabilityHandshakeRequest CreateHandshakeRequest()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Human("user-123");

        return LiabilityHandshakeRequest.Create(
            actor,
            "RunOperation",
            "ack.required",
            "Acknowledgment required.",
            "CONFIRM",
            "Confirm responsibility.",
            LiabilityHandshakeRiskLevel.Medium);
    }

    private static DefaultAsiBackboneAcknowledgmentChallengeService CreateService(
        AsiBackboneAcknowledgmentChallengeOptions? options = null)
    {
        return new DefaultAsiBackboneAcknowledgmentChallengeService(
            Options.Create(options ?? new AsiBackboneAcknowledgmentChallengeOptions()));
    }
}
