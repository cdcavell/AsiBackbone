using AsiBackbone.Core.CapabilityTokens;
using AsiBackbone.Core.Signing;
using Xunit;

namespace AsiBackbone.Core.Tests.CapabilityTokens;

public sealed class CapabilityGrantResultBranchTests
{
    private static readonly DateTimeOffset IssuedUtc = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresUtc = new(2026, 6, 18, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void UseResultAcceptedNormalizesSuccessBranches()
    {
        var result = CapabilityGrantUseResult.Accepted(2);

        Assert.True(result.IsAccepted);
        Assert.Equal(GrantUseState.Accepted, result.State);
        Assert.Equal(2, result.UseCount);
        Assert.Null(result.FailureCode);
        Assert.Null(result.FailureMessage);
    }

    [Theory]
    [InlineData(" use limit reached ", GrantUseState.UseLimitExceeded, "capability.use-limit-exceeded", 3)]
    [InlineData(" stopped ", GrantUseState.Stopped, "capability.grant-stopped", 0)]
    [InlineData(" cancelled ", GrantUseState.Cancelled, "capability.grant-cancelled", 0)]
    [InlineData(" unavailable ", GrantUseState.Unavailable, "capability.use-store-unavailable", 0)]
    public void UseResultFailureFactoriesNormalizeMessages(
        string failureMessage,
        GrantUseState expectedState,
        string expectedFailureCode,
        int expectedUseCount)
    {
        CapabilityGrantUseResult result = expectedState switch
        {
            GrantUseState.Accepted => throw new ArgumentException("Accepted is covered by the success-result test.", nameof(expectedState)),
            GrantUseState.UseLimitExceeded => CapabilityGrantUseResult.UseLimitExceeded(expectedUseCount, failureMessage),
            GrantUseState.Stopped => CapabilityGrantUseResult.Stopped(failureMessage),
            GrantUseState.Cancelled => CapabilityGrantUseResult.Cancelled(failureMessage),
            GrantUseState.Unavailable => CapabilityGrantUseResult.Unavailable(failureMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedState), expectedState, null)
        };

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(expectedUseCount, result.UseCount);
        Assert.Equal(expectedFailureCode, result.FailureCode);
        Assert.Equal(failureMessage.Trim(), result.FailureMessage);
    }

    [Fact]
    public void UseResultRejectsNegativeUseCount()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => CapabilityGrantUseResult.Accepted(-1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => CapabilityGrantUseResult.UseLimitExceeded(-1));
    }

    [Fact]
    public void ValidationResultValidIncludesSafeMetadataAndAllows()
    {
        CapabilityTokenGrant grant = CreateGrant();

        var result = CapabilityGrantValidationResult.Valid(grant);

        Assert.True(result.IsValid);
        Assert.True(result.ShouldAllow);
        Assert.Equal(CapabilityTokenValidationCategory.Valid, result.Category);
        Assert.Equal(VerificationPolicyAction.Allow, result.Action);
        Assert.Equal("Valid", result.Status);
        Assert.Equal("grant-1", result.TokenId);
        Assert.Null(result.FailureCode);
        Assert.Equal("ack-1", result.SafeMetadata["acknowledgment_id"]);
        Assert.Equal("handshake-1", result.SafeMetadata["handshake_id"]);
        Assert.Equal("policy-v1", result.SafeMetadata["policy_version"]);
        Assert.Equal("policy-hash", result.SafeMetadata["policy_hash"]);
        Assert.Equal("robot-arm-1", result.SafeMetadata["resource_binding"]);
    }

    [Fact]
    public void ValidationResultFailedNormalizesFailureMessageAndMetadata()
    {
        CapabilityTokenGrant grant = CreateGrant(
            acknowledgmentId: " ack-1 ",
            handshakeId: " handshake-1 ",
            policyVersion: " policy-v1 ",
            policyHash: " policy-hash ",
            resourceBinding: " robot-arm-1 ");

        var result = CapabilityGrantValidationResult.Failed(
            grant,
            CapabilityTokenValidationCategory.WrongScope,
            VerificationPolicyAction.Deny,
            " capability.scope-missing ",
            " Missing scope. ");

        Assert.False(result.IsValid);
        Assert.False(result.ShouldAllow);
        Assert.Equal(CapabilityTokenValidationCategory.WrongScope, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
        Assert.Equal("Failed", result.Status);
        Assert.Equal("capability.scope-missing", result.FailureCode);
        Assert.Equal("Missing scope.", result.FailureMessage);
        Assert.Equal("capability.scope-missing", result.SafeMetadata["failure_code"]);
    }

    [Fact]
    public void ValidationResultOmitsBlankOptionalSafeMetadata()
    {
        CapabilityTokenGrant grant = CreateGrant(
            acknowledgmentId: null,
            handshakeId: null,
            policyVersion: null,
            policyHash: null,
            resourceBinding: null);

        var result = CapabilityGrantValidationResult.Failed(
            grant,
            CapabilityTokenValidationCategory.Failed,
            VerificationPolicyAction.Escalate,
            "capability.failed",
            " ");

        Assert.Null(result.FailureMessage);
        Assert.False(result.SafeMetadata.ContainsKey("acknowledgment_id"));
        Assert.False(result.SafeMetadata.ContainsKey("handshake_id"));
        Assert.False(result.SafeMetadata.ContainsKey("policy_version"));
        Assert.False(result.SafeMetadata.ContainsKey("policy_hash"));
        Assert.False(result.SafeMetadata.ContainsKey("resource_binding"));
    }

    [Fact]
    public void ValidationResultFactoriesRejectNullInputs()
    {
        _ = Assert.Throws<ArgumentNullException>(() => CapabilityGrantValidationResult.Valid(null!));
        _ = Assert.Throws<ArgumentNullException>(() => CapabilityGrantValidationResult.Failed(
            null!,
            CapabilityTokenValidationCategory.Failed,
            VerificationPolicyAction.Escalate,
            "capability.failed"));
        _ = Assert.Throws<ArgumentException>(() => CapabilityGrantValidationResult.Failed(
            CreateGrant(),
            CapabilityTokenValidationCategory.Failed,
            VerificationPolicyAction.Escalate,
            " "));
    }

    private static CapabilityTokenGrant CreateGrant(
        string? acknowledgmentId = "ack-1",
        string? handshakeId = "handshake-1",
        string? policyVersion = "policy-v1",
        string? policyHash = "policy-hash",
        string? resourceBinding = "robot-arm-1")
    {
        return CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: ["robotics.execute"],
            issuedUtc: IssuedUtc,
            expiresUtc: ExpiresUtc,
            acknowledgmentId: acknowledgmentId,
            handshakeId: handshakeId,
            policyVersion: policyVersion,
            policyHash: policyHash,
            resourceBinding: resourceBinding);
    }
}
