using CDCavell.AsiBackbone.Core.CapabilityTokens;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.CapabilityTokens;

public sealed class CapabilityTokenGrantBranchTests
{
    private static readonly DateTimeOffset IssuedUtc = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresUtc = new(2026, 6, 16, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void CreateNormalizesScopesMetadataAndOptionalBindings()
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" region "] = " us-la ",
            [" "] = " ignored ",
            ["nullable"] = null!
        };

        CapabilityTokenGrant grant = CapabilityTokenGrant.Create(
            tokenId: " grant-1 ",
            issuer: " issuer-1 ",
            audience: " gateway-1 ",
            scopes: [" robotics.write ", "robotics.read", " ", "robotics.read"],
            issuedUtc: IssuedUtc.ToOffset(TimeSpan.FromHours(-5)),
            expiresUtc: ExpiresUtc.ToOffset(TimeSpan.FromHours(-5)),
            notBeforeUtc: IssuedUtc.AddMinutes(1).ToOffset(TimeSpan.FromHours(-5)),
            subjectId: " subject-1 ",
            operationName: " robotics.execute ",
            policyVersion: " policy-v1 ",
            policyHash: " policy-hash ",
            acknowledgmentId: " ack-1 ",
            handshakeId: " handshake-1 ",
            gatewayBinding: " gateway-binding ",
            resourceBinding: " robot-arm-1 ",
            metadata: metadata,
            schemaVersion: " stable-v1 ");

        Assert.Equal("grant-1", grant.TokenId);
        Assert.Equal("issuer-1", grant.Issuer);
        Assert.Equal("gateway-1", grant.Audience);
        Assert.Equal(["robotics.read", "robotics.write"], grant.Scopes);
        Assert.Equal(IssuedUtc, grant.IssuedUtc);
        Assert.Equal(IssuedUtc.AddMinutes(1), grant.NotBeforeUtc);
        Assert.Equal(ExpiresUtc, grant.ExpiresUtc);
        Assert.Equal("subject-1", grant.SubjectId);
        Assert.Equal("robotics.execute", grant.OperationName);
        Assert.Equal("policy-v1", grant.PolicyVersion);
        Assert.Equal("policy-hash", grant.PolicyHash);
        Assert.True(grant.HasAcknowledgmentReference);
        Assert.True(grant.HasHandshakeReference);
        Assert.True(grant.HasMetadata);
        Assert.Equal("us-la", grant.Metadata["region"]);
        Assert.Equal(string.Empty, grant.Metadata["nullable"]);
        Assert.False(grant.Metadata.ContainsKey(string.Empty));
    }

    [Fact]
    public void CreateAllowsMinimalGrantAndReportsMissingOptionalReferences()
    {
        CapabilityTokenGrant grant = CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: ["robotics.read"],
            issuedUtc: IssuedUtc,
            expiresUtc: ExpiresUtc,
            metadata: new Dictionary<string, string>
            {
                [" "] = "ignored"
            });

        Assert.False(grant.HasAcknowledgmentReference);
        Assert.False(grant.HasHandshakeReference);
        Assert.False(grant.HasMetadata);
        Assert.Empty(grant.Metadata);
        Assert.Null(grant.SubjectId);
        Assert.Null(grant.OperationName);
        Assert.Null(grant.PolicyVersion);
        Assert.Null(grant.PolicyHash);
        Assert.Null(grant.GatewayBinding);
        Assert.Null(grant.ResourceBinding);
    }

    [Fact]
    public void CreateRejectsNullScopes()
    {
        Assert.Throws<ArgumentNullException>(() => CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: null!,
            issuedUtc: IssuedUtc,
            expiresUtc: ExpiresUtc));
    }

    [Fact]
    public void CreateRejectsEmptyNormalizedScopes()
    {
        Assert.Throws<ArgumentException>(() => CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: [" ", ""],
            issuedUtc: IssuedUtc,
            expiresUtc: ExpiresUtc));
    }

    [Fact]
    public void CreateRejectsNotBeforeAfterExpiration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: ["robotics.read"],
            issuedUtc: IssuedUtc,
            expiresUtc: ExpiresUtc,
            notBeforeUtc: ExpiresUtc.AddTicks(1)));
    }

    [Fact]
    public void CreateRejectsIssuedAfterExpiration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: ["robotics.read"],
            issuedUtc: ExpiresUtc.AddTicks(1),
            expiresUtc: ExpiresUtc));
    }
}
