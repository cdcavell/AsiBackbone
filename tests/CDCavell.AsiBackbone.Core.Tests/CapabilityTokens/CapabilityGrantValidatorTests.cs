using System.Reflection;
using System.Runtime.CompilerServices;
using CDCavell.AsiBackbone.Core.CapabilityTokens;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.CapabilityTokens;

public sealed class CapabilityGrantValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, CapabilityTokenValidationCategory, VerificationPolicyAction, string> MetadataFailureCases => new()
    {
        { "wrong-issuer", CapabilityTokenValidationCategory.WrongIssuer, VerificationPolicyAction.Deny, "capability.issuer-mismatch" },
        { "not-yet-valid", CapabilityTokenValidationCategory.NotYetValid, VerificationPolicyAction.Defer, "capability.not-yet-valid" },
        { "policy-version-mismatch", CapabilityTokenValidationCategory.PolicyMismatch, VerificationPolicyAction.Deny, "capability.policy-mismatch" },
        { "policy-hash-mismatch", CapabilityTokenValidationCategory.PolicyMismatch, VerificationPolicyAction.Deny, "capability.policy-mismatch" },
        { "acknowledgment-mismatch", CapabilityTokenValidationCategory.AcknowledgmentMismatch, VerificationPolicyAction.Deny, "capability.acknowledgment-mismatch" },
        { "handshake-mismatch", CapabilityTokenValidationCategory.HandshakeMismatch, VerificationPolicyAction.Deny, "capability.handshake-mismatch" },
        { "gateway-mismatch", CapabilityTokenValidationCategory.GatewayMismatch, VerificationPolicyAction.Deny, "capability.gateway-mismatch" },
        { "resource-mismatch", CapabilityTokenValidationCategory.ResourceMismatch, VerificationPolicyAction.Deny, "capability.resource-mismatch" }
    };

    public static TheoryData<string, CapabilityTokenValidationCategory, VerificationPolicyAction, string> ProofFailureCases => new()
    {
        { "missing-signature", CapabilityTokenValidationCategory.MissingProof, VerificationPolicyAction.RequireAcknowledgment, "signature.missing" },
        { "invalid-signature", CapabilityTokenValidationCategory.InvalidProof, VerificationPolicyAction.Deny, "signature.invalid" },
        { "hash-mismatch", CapabilityTokenValidationCategory.InvalidProof, VerificationPolicyAction.Deny, "signature.hash-mismatch" },
        { "revoked-key", CapabilityTokenValidationCategory.Revoked, VerificationPolicyAction.Deny, "signature.revoked" },
        { "unsupported-algorithm", CapabilityTokenValidationCategory.InvalidProof, VerificationPolicyAction.Deny, "signature.algorithm-unsupported" },
        { "provider-unavailable", CapabilityTokenValidationCategory.Failed, VerificationPolicyAction.Defer, "signature.provider-unavailable" },
        { "unknown-key-version", CapabilityTokenValidationCategory.Failed, VerificationPolicyAction.Escalate, "signature.key-version-unknown" },
        { "canonicalization-mismatch", CapabilityTokenValidationCategory.Failed, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "failed", CapabilityTokenValidationCategory.Failed, VerificationPolicyAction.Escalate, "verification.failure" }
    };

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

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.Expired,
            VerificationPolicyAction.Deny,
            "capability.expired");
    }

    [Fact]
    public async Task ValidateAsyncDeniesWrongAudience()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(audience: "other-gateway"));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.WrongAudience,
            VerificationPolicyAction.Deny,
            "capability.audience-mismatch");
    }

    [Fact]
    public async Task ValidateAsyncDeniesWrongScope()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(scopes: ["robotics.read"]));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.WrongScope,
            VerificationPolicyAction.Deny,
            "capability.scope-missing");
    }

    [Theory]
    [MemberData(nameof(MetadataFailureCases))]
    public async Task ValidateAsyncReturnsExpectedMetadataFailures(
        string scenario,
        CapabilityTokenValidationCategory expectedCategory,
        VerificationPolicyAction expectedAction,
        string expectedFailureCode)
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrantForMetadataFailure(scenario));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(result, expectedCategory, expectedAction, expectedFailureCode);
    }

    [Fact]
    public async Task ValidateAsyncDeniesWhenAnyRequiredScopeIsMissingFromMultipleRequiredScopes()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(scopes: ["robotics.execute", "robotics.read"]));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(scopes: ["robotics.execute", "robotics.admin"]),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.WrongScope,
            VerificationPolicyAction.Deny,
            "capability.scope-missing");
    }

    [Fact]
    public async Task ValidateAsyncAllowsWhenAllRequiredScopesArePresent()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(scopes: ["robotics.audit", "robotics.execute", "robotics.read"]));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(scopes: ["robotics.audit", "robotics.execute"]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
        Assert.True(result.ShouldAllow);
        Assert.Equal(CapabilityTokenValidationCategory.Valid, result.Category);
        Assert.Equal(VerificationPolicyAction.Allow, result.Action);
    }

    [Fact]
    public async Task ValidateAsyncRequiresMissingAcknowledgmentReference()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant(acknowledgmentId: null));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireAcknowledgmentReference: true),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.MissingAcknowledgmentReference,
            VerificationPolicyAction.RequireAcknowledgment,
            "capability.acknowledgment-missing");
    }

    [Fact]
    public async Task ValidateAsyncDeniesProofRequiredWithoutVerifier()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireProof: true),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.MissingProof,
            VerificationPolicyAction.Deny,
            "capability.proof-verifier-missing");
    }

    [Theory]
    [MemberData(nameof(ProofFailureCases))]
    public async Task ValidateAsyncMapsProofFailureCategories(
        string scenario,
        CapabilityTokenValidationCategory expectedCategory,
        VerificationPolicyAction expectedAction,
        string expectedFailureCode)
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var verifier = new StubVerificationService(CreateProofFailure(scenario));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireProof: true),
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(verifier.WasCalled);
        AssertFailure(result, expectedCategory, expectedAction, expectedFailureCode);
    }

    [Fact]
    public async Task ValidateAsyncDefersWhenUseCheckIsRequiredWithoutUseStore()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireUseCheck: true),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.ReplayStoreUnavailable,
            VerificationPolicyAction.Defer,
            "capability.use-store-missing");
    }

    [Theory]
    [InlineData(null, "capability.use-store-unavailable")]
    [InlineData("capability.custom-use-store-unavailable", "capability.custom-use-store-unavailable")]
    public async Task ValidateAsyncDefersWhenUseStoreReturnsUnavailable(string? customFailureCode, string expectedFailureCode)
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        CapabilityGrantUseResult useResult = customFailureCode is null
            ? CapabilityGrantUseResult.Unavailable("Use store offline.")
            : CreateSyntheticUseResult(GrantUseState.Unavailable, failureCode: customFailureCode, failureMessage: "Use store offline.");
        var useStore = new FixedUseStore(useResult);

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireUseCheck: true),
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.ReplayStoreUnavailable,
            VerificationPolicyAction.Defer,
            expectedFailureCode);
        Assert.Equal("Use store offline.", result.FailureMessage);
    }

    [Fact]
    public async Task ValidateAsyncEscalatesUnknownUseStoreState()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var useStore = new FixedUseStore(CreateSyntheticUseResult((GrantUseState)999));

        CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
            grant,
            CreateOptions(requireUseCheck: true),
            useStore: useStore,
            cancellationToken: TestContext.Current.CancellationToken);

        AssertFailure(
            result,
            CapabilityTokenValidationCategory.Failed,
            VerificationPolicyAction.Escalate,
            "capability.validation-failed");
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

    [Fact]
    public async Task ValidateAsyncThrowsWhenTokenIsAlreadyCanceled()
    {
        SignedGovernanceArtifact<CapabilityTokenGrant> grant = CreateSignedGrant(CreateGrant());
        var cancellationToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await CapabilityGrantValidator.ValidateAsync(
                grant,
                CreateOptions(),
                cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task ValidateAsyncThrowsWhenSignedGrantIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await CapabilityGrantValidator.ValidateAsync(
                null!,
                CreateOptions(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    private static CapabilityGrantValidationOptions CreateOptions(
        bool requireProof = false,
        bool requireAcknowledgmentReference = false,
        bool requireUseCheck = false,
        IEnumerable<string>? scopes = null,
        string issuer = "issuer-1",
        string audience = "gateway-1",
        string policyVersion = "policy-v1",
        string policyHash = "policy-hash",
        string acknowledgmentId = "ack-1",
        string handshakeId = "handshake-1",
        string gatewayBinding = "gateway-1",
        string resourceBinding = "robot-arm-1")
    {
        return CapabilityGrantValidationOptions.Create(
            issuer: issuer,
            audience: audience,
            scopes: scopes ?? ["robotics.execute"],
            validationUtc: Now,
            policyVersion: policyVersion,
            policyHash: policyHash,
            acknowledgmentId: acknowledgmentId,
            handshakeId: handshakeId,
            gatewayBinding: gatewayBinding,
            resourceBinding: resourceBinding,
            requireProof: requireProof,
            requireAcknowledgmentReference: requireAcknowledgmentReference,
            requireUseCheck: requireUseCheck,
            maxUseCount: 1);
    }

    private static CapabilityTokenGrant CreateGrant(
        string issuer = "issuer-1",
        string audience = "gateway-1",
        IEnumerable<string>? scopes = null,
        DateTimeOffset? notBeforeUtc = null,
        DateTimeOffset? expiresUtc = null,
        string? acknowledgmentId = "ack-1",
        string? handshakeId = "handshake-1",
        string? policyVersion = "policy-v1",
        string? policyHash = "policy-hash",
        string? gatewayBinding = "gateway-1",
        string? resourceBinding = "robot-arm-1")
    {
        return CapabilityTokenGrant.Create(
            tokenId: "grant-1",
            issuer: issuer,
            audience: audience,
            scopes: scopes ?? ["robotics.execute"],
            issuedUtc: Now.AddMinutes(-5),
            expiresUtc: expiresUtc ?? Now.AddMinutes(5),
            notBeforeUtc: notBeforeUtc,
            acknowledgmentId: acknowledgmentId,
            handshakeId: handshakeId,
            policyVersion: policyVersion,
            policyHash: policyHash,
            gatewayBinding: gatewayBinding,
            resourceBinding: resourceBinding);
    }

    private static CapabilityTokenGrant CreateGrantForMetadataFailure(string scenario)
    {
        return scenario switch
        {
            "wrong-issuer" => CreateGrant(issuer: "other-issuer"),
            "not-yet-valid" => CreateGrant(notBeforeUtc: Now.AddMinutes(1)),
            "policy-version-mismatch" => CreateGrant(policyVersion: "policy-v2"),
            "policy-hash-mismatch" => CreateGrant(policyHash: "policy-hash-v2"),
            "acknowledgment-mismatch" => CreateGrant(acknowledgmentId: "ack-2"),
            "handshake-mismatch" => CreateGrant(handshakeId: "handshake-2"),
            "gateway-mismatch" => CreateGrant(gatewayBinding: "gateway-2"),
            "resource-mismatch" => CreateGrant(resourceBinding: "robot-arm-2"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown metadata failure scenario.")
        };
    }

    private static SignatureVerificationResult CreateProofFailure(string scenario)
    {
        return scenario switch
        {
            "missing-signature" => SignatureVerificationResult.MissingSignature("Signature metadata missing."),
            "invalid-signature" => SignatureVerificationResult.Failed("signature.invalid", "Invalid signature."),
            "hash-mismatch" => SignatureVerificationResult.Failed("signature.hash-mismatch", "Hash mismatch."),
            "revoked-key" => SignatureVerificationResult.Failed("signature.revoked", "Revoked key."),
            "unsupported-algorithm" => SignatureVerificationResult.Failed("signature.algorithm-unsupported", "Unsupported algorithm."),
            "provider-unavailable" => SignatureVerificationResult.Failed("signature.provider-unavailable", "Provider unavailable."),
            "unknown-key-version" => SignatureVerificationResult.Failed("signature.key-version-unknown", "Unknown key version."),
            "canonicalization-mismatch" => SignatureVerificationResult.Failed("signature.canonicalization-mismatch", "Canonicalization mismatch."),
            "failed" => SignatureVerificationResult.Failed("verification.failure", "Verification failed."),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown proof failure scenario.")
        };
    }

    private static SignedGovernanceArtifact<CapabilityTokenGrant> CreateSignedGrant(CapabilityTokenGrant grant)
    {
        var payload = CanonicalPayload.Create(
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

    private static void AssertFailure(
        CapabilityGrantValidationResult result,
        CapabilityTokenValidationCategory category,
        VerificationPolicyAction action,
        string failureCode)
    {
        Assert.False(result.IsValid);
        Assert.False(result.ShouldAllow);
        Assert.Equal(category, result.Category);
        Assert.Equal(action, result.Action);
        Assert.Equal(failureCode, result.FailureCode);
    }

    private static CapabilityGrantUseResult CreateSyntheticUseResult(
        GrantUseState state,
        int useCount = 0,
        string? failureCode = null,
        string? failureMessage = null)
    {
        var result = (CapabilityGrantUseResult)RuntimeHelpers.GetUninitializedObject(typeof(CapabilityGrantUseResult));
        SetBackingField(result, nameof(CapabilityGrantUseResult.State), state);
        SetBackingField(result, nameof(CapabilityGrantUseResult.UseCount), useCount);
        SetBackingField(result, nameof(CapabilityGrantUseResult.FailureCode), failureCode);
        SetBackingField(result, nameof(CapabilityGrantUseResult.FailureMessage), failureMessage);
        return result;
    }

    private static void SetBackingField<T>(CapabilityGrantUseResult result, string propertyName, T value)
    {
        FieldInfo field = typeof(CapabilityGrantUseResult).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");

        field.SetValue(result, value);
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

    private sealed class FixedUseStore(CapabilityGrantUseResult result) : ICapabilityGrantUseStore
    {
        public ValueTask<CapabilityGrantUseResult> TryConsumeAsync(
            CapabilityTokenGrant grant,
            int maxUseCount,
            DateTimeOffset usedUtc,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(grant);
            cancellationToken.ThrowIfCancellationRequested();
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
