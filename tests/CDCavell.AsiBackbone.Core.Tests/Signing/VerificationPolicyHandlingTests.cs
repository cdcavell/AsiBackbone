using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Signing;

public sealed class VerificationPolicyHandlingTests
{
    [Fact]
    public async Task VerifyAsyncMapsValidSignatureToAllow()
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact();
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(outcome.IsVerified);
        Assert.True(outcome.ShouldAllow);
        Assert.Equal(SignatureVerificationCategory.Valid, outcome.Category);
        Assert.Equal(VerificationPolicyAction.Allow, outcome.Action);
        Assert.Equal("Verified", outcome.Status);
        Assert.Equal("record-1", outcome.ArtifactId);
        Assert.Equal("asibackbone.test-artifact", outcome.ArtifactType);
        Assert.Equal("key-1", outcome.KeyId);
        Assert.Equal("v1", outcome.KeyVersion);
        Assert.False(outcome.SafeMetadata.ContainsKey("signature"));
    }

    [Fact]
    public async Task VerifyAsyncMapsInvalidSignatureToDeny()
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact();
        var verifier = new StubVerificationService(SignatureVerificationResult.Failed(
            "signature.invalid",
            "The signature did not verify."));

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(outcome.IsVerified);
        Assert.False(outcome.ShouldAllow);
        Assert.Equal(SignatureVerificationCategory.InvalidSignature, outcome.Category);
        Assert.Equal(VerificationPolicyAction.Deny, outcome.Action);
        Assert.Equal("signature.invalid", outcome.FailureCode);
    }

    [Fact]
    public async Task VerifyAsyncMapsMissingSignatureToRequireAcknowledgment()
    {
        SignedGovernanceArtifact<string> artifact = CreateArtifactWithoutSignature();
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(outcome.IsVerified);
        Assert.Equal(SignatureVerificationCategory.MissingSignature, outcome.Category);
        Assert.Equal(VerificationPolicyAction.RequireAcknowledgment, outcome.Action);
        Assert.Equal("signature.missing", outcome.FailureCode);
        Assert.False(verifier.WasCalled);
    }

    [Fact]
    public async Task VerifyAsyncMapsUnknownKeyVersionToEscalate()
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact(keyVersion: "retired");
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());
        var context = VerificationPolicyContext.Create(expectedKeyVersion: "v1");

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            context: context,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(outcome.IsVerified);
        Assert.Equal(SignatureVerificationCategory.UnknownKeyVersion, outcome.Category);
        Assert.Equal(VerificationPolicyAction.Escalate, outcome.Action);
        Assert.Equal("signature.key-version-unknown", outcome.FailureCode);
        Assert.False(verifier.WasCalled);
    }

    [Fact]
    public async Task VerifyAsyncMapsProviderUnavailableToDefer()
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact();
        var verifier = new StubVerificationService(
            SignatureVerificationResult.Verified(),
            throwProviderUnavailable: true);

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(outcome.IsVerified);
        Assert.Equal(SignatureVerificationCategory.ProviderUnavailable, outcome.Category);
        Assert.Equal(VerificationPolicyAction.Defer, outcome.Action);
        Assert.Equal("signature.provider-unavailable", outcome.FailureCode);
        Assert.True(verifier.WasCalled);
    }

    [Fact]
    public async Task VerifyAsyncSupportsHostPolicyOverrides()
    {
        SignedGovernanceArtifact<string> artifact = CreateArtifactWithoutSignature();
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());
        var options = VerificationPolicyOptions.Create(new Dictionary<SignatureVerificationCategory, VerificationPolicyAction>
        {
            [SignatureVerificationCategory.MissingSignature] = VerificationPolicyAction.DeadLetter
        });

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(SignatureVerificationCategory.MissingSignature, outcome.Category);
        Assert.Equal(VerificationPolicyAction.DeadLetter, outcome.Action);
    }

    private static SignedGovernanceArtifact<string> CreateSignedArtifact(
        string signingHash = "",
        string keyVersion = "v1")
    {
        CanonicalPayload payload = CreatePayload();
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);
        string effectiveSigningHash = string.IsNullOrWhiteSpace(signingHash)
            ? hash.HashValue
            : signingHash;
        Dictionary<string, string> metadata = new(hash.ToSigningMetadata().Metadata, StringComparer.Ordinal)
        {
            ["policy_hash"] = "policy-hash",
            ["policy_version"] = "policy-v1"
        };
        var signingMetadata = SigningMetadata.Create(
            signingHash: effectiveSigningHash,
            hashAlgorithm: hash.HashAlgorithm,
            signature: "fake-signature",
            signatureAlgorithm: "FAKE-SIGNATURE-V1",
            keyId: "key-1",
            keyVersion: keyVersion,
            provider: "fake-provider",
            signedUtc: new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
            metadata: metadata);

        return SignedGovernanceArtifacts.FromSigningMetadata(
            "artifact",
            payload,
            hash,
            signingMetadata);
    }

    private static SignedGovernanceArtifact<string> CreateArtifactWithoutSignature()
    {
        CanonicalPayload payload = CreatePayload();
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);

        return SignedGovernanceArtifacts.WithoutSignature(
            "artifact",
            payload,
            hash);
    }

    private static CanonicalPayload CreatePayload()
    {
        return CanonicalPayload.Create(
            "asibackbone.test-artifact",
            "record-1",
            "test-schema-v1",
            CanonicalPayloadOptions.DefaultCanonicalizationVersion,
            new Dictionary<string, object?>
            {
                ["eventId"] = "event-1",
                ["outcome"] = "Allowed"
            });
    }

    private sealed class StubVerificationService(
        SignatureVerificationResult result,
        bool throwProviderUnavailable = false) : IAsiBackboneSignatureVerificationService
    {
        public bool WasCalled { get; private set; }

        public ValueTask<SignatureVerificationResult> VerifyAsync(
            SignatureVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;

            return throwProviderUnavailable
                ? throw new InvalidOperationException("Verification provider unavailable.")
                : ValueTask.FromResult(result);
        }
    }
}
