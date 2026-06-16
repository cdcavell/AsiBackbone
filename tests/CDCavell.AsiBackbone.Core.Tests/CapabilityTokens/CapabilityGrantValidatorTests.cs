using CDCavell.AsiBackbone.Core.CapabilityTokens;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.CapabilityTokens;

public sealed class CapabilityGrantValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidateAsyncAllowsValidSignedGrantAndConsumesUse()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());
        var useStore = new InMemoryUseStore();

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireProof: true, requireUseCheck: true),
            verifier,
            useStore,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
        Assert.True(result.ShouldAllow);
        Assert.Equal(CapabilityTokenValidationCategory.Valid, result.Category);
        Assert.Equal(VerificationPolicyAction.Allow, result.Action);
        Assert.True(verifier.WasCalled);
        Assert.Equal(1, useStore.GetUseCount("grant-1"));
    }

    [Fact]
    public async Task ValidateAsyncDeniesExpiredGrant()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(expiresUtc: Now.AddMinutes(-1)));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.Expired, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
        Assert.Equal("capability.expired", result.FailureCode);
    }

    [Fact]
    public async Task ValidateAsyncDeniesWrongAudience()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(audience: "other-gateway"));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.WrongAudience, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
    }

    [Fact]
    public async Task ValidateAsyncDeniesWrongScope()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(scopes: ["robotics.read"]));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.WrongScope, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
    }

    [Fact]
    public async Task ValidateAsyncRequiresMissingAcknowledgmentReference()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(acknowledgmentId: null));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireAcknowledgmentReference: true),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.MissingAcknowledgmentReference, result.Category);
        Assert.Equal(VerificationPolicyAction.RequireAcknowledgment, result.Action);
    }

    [Fact]
    public async Task ValidateAsyncDeniesRepeatedUseWhenSingleUseIsConfigured()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var useStore = new InMemoryUseStore();
        CapabilityGrantValidationOptions options = CreateOptions(requireUseCheck: true);

        CapabilityGrantValidationResult first = await CapabilityGrantValidator.ValidateAsync(
            grant,
            options,
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);
        CapabilityGrantValidationResult second = await CapabilityGrantValidator.ValidateAsync(
            grant,
            options,
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(first.IsValid);
        Assert.False(second.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.ReuseLimitExceeded, second.Category);
        Assert.Equal(VerificationPolicyAction.Deny, second.Action);
    }

    [Fact]
    public async Task ValidateAsyncDeniesStoppedGrantAsRevoked()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var useStore = new InMemoryUseStore(stoppedGrantIds: ["grant-1"]);

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireUseCheck: true),
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.Revoked, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
    }

    [Fact]
    public async Task ValidateAsyncDeniesCancelledGrant()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var useStore = new InMemoryUseStore(cancelledGrantIds: ["grant-1"]);

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireUseCheck: true),
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(CapabilityTokenValidationCategory.Cancelled, result.Category);
        Assert.Equal(VerificationPolicyAction.Deny, result.Action);
    }

    private static CapabilityGrantValidationOptions CreateOptions(
        bool requireProof = false,
        bool requireAcknowledgmentReference = false,
        bool requireUseCheck = false)
    {
        return CapabilityGrantValidationOptions.Create(
            issuer: "issuer-1",
            audience: "gateway-1",
            scopes: ["robotics.execute"],
            validationUtc: Now,
            policyVersion: "policy-v1",
            policyHash: "policy-hash",
            acknowledgmentId: "ack-1",
            requireProof: requireProof,
            requireAcknowledgmentReference: requireAcknowledgmentReference,
            requireUseCheck: requireUseCheck,
            maxUseCount: 1);
    }

    private static CapabilityTokenGrant CreateGrant(
        string audience = "gateway-1",
        IEnumerable<string>? scopes = null,
        DateTimeOffset? expiresUtc = null,
        string? acknowledgmentId = "ack-1")
    {
        return CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: "issuer-1",
            audience: audience,
            scopes: scopes ?? ["robotics.execute"],
            issuedUtc: Now.AddMinutes(-5),
            expiresUtc: expiresUtc ?? Now.AddMinutes(5),
            acknowledgmentId: acknowledgmentId,
            handshakeId: "handshake-1",
            policyVersion: "policy-v1",
            policyHash: "policy-hash",
            gatewayBinding: "gateway-1",
            resourceBinding: "robot-arm-1");
    }

    private static SignedGovernanceArtifact<CapabilityTokenGrant> CreateSignedGrant(CapabilityTokenGrant grant)
    {
        CanonicalPayload payload = CanonicalPayload.Create(
            CanonicalArtifactTypes.CapabilityTokenGrant,
            grant.TokenId,
            grant.SchemaVersion,
            CanonicalPayloadOptions.DefaultCanonicalizationVersion,
            new Dictionary<string, object?>
            {
                ["audience"] = grant.Audience,
                ["expiresUtc"] = grant.ExpiresUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["issuer"] = grant.Issuer,
                ["scopes"] = grant.Scopes.ToArray()
            });
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);
        var signingMetadata = SigningMetadata.Create(
            signingHash: hash.HashValue,
            hashAlgorithm: hash.HashAlgorithm,
            signature: "fake-signature",
            signatureAlgorithm: "FAKE-SIGNATURE-V1",
            keyId: "key-1",
            keyVersion: "v1",
            provider: "fake-provider",
            signedUtc: Now);

        return SignedGovernanceArtifacts.FromSigningMetadata(grant, payload, hash, signingMetadata);
    }

    private sealed class StubVerificationService(SignatureVerificationResult result) : IAsiBackboneSignatureVerificationService
    {
        public bool WasCalled { get; private set; }

        public ValueTask<SignatureVerificationResult> VerifyAsync(SignatureVerificationRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class InMemoryUseStore(
        IEnumerable<string>? stoppedGrantIds = null,
        IEnumerable<string>? cancelledGrantIds = null) : ICapabilityGrantUseStore
    {
        private readonly HashSet<string> _stoppedGrantIds = new(stoppedGrantIds ?? [], StringComparer.Ordinal);
        private readonly HashSet<string> _cancelledGrantIds = new(cancelledGrantIds ?? [], StringComparer.Ordinal);
        private readonly Dictionary<string, int> _useCounts = new(StringComparer.Ordinal);

        public int GetUseCount(string grantId)
        {
            return _useCounts.TryGetValue(grantId, out int useCount) ? useCount : 0;
        }

        public ValueTask<CapabilityGrantUseResult> TryConsumeAsync(
            CapabilityTokenGrant grant,
            int maxUseCount,
            DateTimeOffset usedUtc,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(grant);
            cancellationToken.ThrowIfCancellationRequested();

            if (_stoppedGrantIds.Contains(grant.TokenId))
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.Stopped());
            }

            if (_cancelledGrantIds.Contains(grant.TokenId))
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.Cancelled());
            }

            int currentCount = GetUseCount(grant.TokenId);
            if (currentCount >= maxUseCount)
            {
                return ValueTask.FromResult(CapabilityGrantUseResult.UseLimitExceeded(currentCount));
            }

            int nextCount = currentCount + 1;
            _useCounts[grant.TokenId] = nextCount;
            return ValueTask.FromResult(CapabilityGrantUseResult.Accepted(nextCount));
        }
    }
}
