using System.Reflection;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Signing;

public sealed class VerificationPolicyHandlingTests
{
    public static TheoryData<string, SignatureVerificationCategory, VerificationPolicyAction, string> PreflightFailureCases => new()
    {
        { "missing-signing-hash", SignatureVerificationCategory.MissingSignature, VerificationPolicyAction.RequireAcknowledgment, "signature.missing" },
        { "signing-hash-mismatch", SignatureVerificationCategory.HashMismatch, VerificationPolicyAction.Deny, "signature.hash-mismatch" },
        { "metadata-hash-algorithm-mismatch", SignatureVerificationCategory.UnsupportedAlgorithm, VerificationPolicyAction.Deny, "signature.hash-algorithm-unsupported" },
        { "required-hash-algorithm-mismatch", SignatureVerificationCategory.UnsupportedAlgorithm, VerificationPolicyAction.Deny, "signature.hash-algorithm-unsupported" },
        { "canonical-artifact-id-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "canonical-artifact-type-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "canonicalization-version-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "payload-schema-version-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "expected-key-id-mismatch", SignatureVerificationCategory.UnknownKeyVersion, VerificationPolicyAction.Escalate, "signature.key-version-unknown" },
        { "required-provider-mismatch", SignatureVerificationCategory.ProviderUnavailable, VerificationPolicyAction.Defer, "signature.provider-unavailable" },
        { "expected-policy-version-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" },
        { "expected-policy-hash-mismatch", SignatureVerificationCategory.CanonicalizationMismatch, VerificationPolicyAction.Escalate, "signature.canonicalization-mismatch" }
    };

    public static TheoryData<SignatureVerificationResult, SignatureVerificationCategory> CategoryMappingCases => new()
    {
        { SignatureVerificationResult.MissingSignature("Missing signature."), SignatureVerificationCategory.MissingSignature },
        { SignatureVerificationResult.Failed("proof.missing", "Missing proof."), SignatureVerificationCategory.MissingSignature },
        { SignatureVerificationResult.Failed("signature.hash-mismatch", "Hash mismatch."), SignatureVerificationCategory.HashMismatch },
        { SignatureVerificationResult.Failed("signature.canonicalization-mismatch", "Canonicalization mismatch."), SignatureVerificationCategory.CanonicalizationMismatch },
        { SignatureVerificationResult.Failed("payload-schema.mismatch", "Payload schema mismatch."), SignatureVerificationCategory.CanonicalizationMismatch },
        { SignatureVerificationResult.Failed("artifact.mismatch", "Artifact mismatch."), SignatureVerificationCategory.CanonicalizationMismatch },
        { SignatureVerificationResult.Failed("signature.unsupported", "Unsupported signature."), SignatureVerificationCategory.UnsupportedAlgorithm },
        { SignatureVerificationResult.Failed("signature.algorithm-unsupported", "Unsupported algorithm."), SignatureVerificationCategory.UnsupportedAlgorithm },
        { SignatureVerificationResult.Failed("signature.revoked", "Revoked key."), SignatureVerificationCategory.RevokedKey },
        { SignatureVerificationResult.Failed("signature.disabled", "Disabled key."), SignatureVerificationCategory.RevokedKey },
        { SignatureVerificationResult.Failed("signature.unknown-key", "Unknown key."), SignatureVerificationCategory.UnknownKeyVersion },
        { SignatureVerificationResult.Failed("signature.key-version-unknown", "Unknown key version."), SignatureVerificationCategory.UnknownKeyVersion },
        { SignatureVerificationResult.Failed("signature.key.mismatch", "Key mismatch."), SignatureVerificationCategory.UnknownKeyVersion },
        { SignatureVerificationResult.Failed("signature.key-mismatch", "Key mismatch."), SignatureVerificationCategory.UnknownKeyVersion },
        { SignatureVerificationResult.Failed("signature.provider-unavailable", "Provider unavailable."), SignatureVerificationCategory.ProviderUnavailable },
        { SignatureVerificationResult.Failed("provider.unavailable", "Provider unavailable."), SignatureVerificationCategory.ProviderUnavailable },
        { SignatureVerificationResult.Failed("provider.timeout", "Provider timeout."), SignatureVerificationCategory.ProviderUnavailable },
        { SignatureVerificationResult.Failed("provider.network", "Provider network failure."), SignatureVerificationCategory.ProviderUnavailable },
        { SignatureVerificationResult.Failed("signature.invalid", "Invalid signature."), SignatureVerificationCategory.InvalidSignature },
        { SignatureVerificationResult.Failed("signature.malformed", "Malformed signature."), SignatureVerificationCategory.InvalidSignature },
        { SignatureVerificationResult.Failed("signature.bad", "Bad signature."), SignatureVerificationCategory.InvalidSignature },
        { SignatureVerificationResult.Failed("verification.failure", "Verification failed."), SignatureVerificationCategory.Failed }
    };

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
        Assert.True(outcome.SafeMetadata.ContainsKey("signed_utc"));
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
            CreateProviderException("invalid-operation"));

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

    [Theory]
    [MemberData(nameof(PreflightFailureCases))]
    public async Task VerifyAsyncReturnsExpectedPreflightFailures(
        string scenario,
        SignatureVerificationCategory expectedCategory,
        VerificationPolicyAction expectedAction,
        string expectedFailureCode)
    {
        (SignedGovernanceArtifact<string> artifact, VerificationPolicyContext? context) = CreatePreflightScenario(scenario);
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            context: context,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(outcome.IsVerified);
        Assert.Equal(expectedCategory, outcome.Category);
        Assert.Equal(expectedAction, outcome.Action);
        Assert.Equal(expectedFailureCode, outcome.FailureCode);
        Assert.Equal(expectedFailureCode, outcome.SafeMetadata["failure_code"]);
        Assert.False(verifier.WasCalled);
    }

    [Theory]
    [InlineData("invalid-operation")]
    [InlineData("not-supported")]
    [InlineData("timeout")]
    public async Task VerifyAsyncMapsProviderExceptionsToDefer(string scenario)
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact();
        var verifier = new StubVerificationService(
            SignatureVerificationResult.Verified(),
            CreateProviderException(scenario));

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

    [Theory]
    [MemberData(nameof(CategoryMappingCases))]
    public void CategorizeMapsFailureCodeAndStatusPatterns(
        SignatureVerificationResult verificationResult,
        SignatureVerificationCategory expectedCategory)
    {
        SignatureVerificationCategory category = VerificationPolicyEvaluator.Categorize(verificationResult);

        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public async Task VerifyAsyncBuildsSafeMetadataWithSignedUtcAndFiltersUnsafeCustomKeys()
    {
        SignedGovernanceArtifact<string> artifact = CreateSignedArtifact(
            keyId: " ",
            keyVersion: " ",
            signatureAlgorithm: null,
            provider: " ",
            signedUtc: null,
            includeSignedUtc: false,
            metadataOverrides: new Dictionary<string, string>
            {
                [" custom_context "] = " context-value ",
                ["signature_hint"] = "do-not-log",
                ["secret_name"] = "do-not-log",
                ["access_token"] = "do-not-log",
                ["credential_id"] = "do-not-log",
                ["private_key_ref"] = "do-not-log"
            });
        var verifier = new StubVerificationService(SignatureVerificationResult.Verified());

        VerificationPolicyOutcome outcome = await GovernanceArtifactVerifier.VerifyAsync(
            artifact,
            verifier,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(outcome.IsVerified);
        Assert.Equal("context-value", outcome.SafeMetadata["custom_context"]);
        Assert.False(outcome.SafeMetadata.ContainsKey("key_id"));
        Assert.False(outcome.SafeMetadata.ContainsKey("key_version"));
        Assert.False(outcome.SafeMetadata.ContainsKey("provider"));
        Assert.False(outcome.SafeMetadata.ContainsKey("signature_algorithm"));
        Assert.False(outcome.SafeMetadata.ContainsKey("signed_utc"));
        Assert.False(outcome.SafeMetadata.ContainsKey("signature_hint"));
        Assert.False(outcome.SafeMetadata.ContainsKey("secret_name"));
        Assert.False(outcome.SafeMetadata.ContainsKey("access_token"));
        Assert.False(outcome.SafeMetadata.ContainsKey("credential_id"));
        Assert.False(outcome.SafeMetadata.ContainsKey("private_key_ref"));
    }

    private static (SignedGovernanceArtifact<string> Artifact, VerificationPolicyContext? Context) CreatePreflightScenario(string scenario)
    {
        return scenario switch
        {
            "missing-signing-hash" => (CreateUncheckedArtifact(CreateUncheckedSigningMetadata(signingHash: null)), null),
            "signing-hash-mismatch" => (CreateUncheckedArtifact(CreateUncheckedSigningMetadata(signingHash: "deadbeef")), null),
            "metadata-hash-algorithm-mismatch" => (CreateSignedArtifact(hashAlgorithm: "SHA512"), null),
            "required-hash-algorithm-mismatch" => (CreateSignedArtifact(), VerificationPolicyContext.Create(requiredHashAlgorithm: "SHA512")),
            "canonical-artifact-id-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["artifact_id"] = "record-2" }), null),
            "canonical-artifact-type-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["artifact_type"] = "asibackbone.other-artifact" }), null),
            "canonicalization-version-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["canonicalization_version"] = "canonical-json-v0" }), null),
            "payload-schema-version-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["payload_schema_version"] = "test-schema-v2" }), null),
            "expected-key-id-mismatch" => (CreateSignedArtifact(keyId: "key-2"), VerificationPolicyContext.Create(expectedKeyId: "key-1")),
            "required-provider-mismatch" => (CreateSignedArtifact(provider: "other-provider"), VerificationPolicyContext.Create(requiredProvider: "fake-provider")),
            "expected-policy-version-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["policy_version"] = "policy-v2" }), VerificationPolicyContext.Create(expectedPolicyVersion: "policy-v1")),
            "expected-policy-hash-mismatch" => (CreateSignedArtifact(metadataOverrides: new Dictionary<string, string> { ["policy_hash"] = "policy-hash-v2" }), VerificationPolicyContext.Create(expectedPolicyHash: "policy-hash")),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown preflight scenario.")
        };
    }

    private static SignedGovernanceArtifact<string> CreateSignedArtifact(
        string signingHash = "",
        string hashAlgorithm = "",
        string? signature = "fake-signature",
        string? signatureAlgorithm = "FAKE-SIGNATURE-V1",
        string keyId = "key-1",
        string keyVersion = "v1",
        string provider = "fake-provider",
        DateTimeOffset? signedUtc = null,
        bool includeSignedUtc = true,
        IReadOnlyDictionary<string, string>? metadataOverrides = null)
    {
        CanonicalPayload payload = CreatePayload();
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);
        string effectiveSigningHash = string.IsNullOrWhiteSpace(signingHash)
            ? hash.HashValue
            : signingHash;
        string effectiveHashAlgorithm = string.IsNullOrWhiteSpace(hashAlgorithm)
            ? hash.HashAlgorithm
            : hashAlgorithm;
        Dictionary<string, string> metadata = CreateDefaultMetadata(hash, metadataOverrides);

        var signingMetadata = SigningMetadata.Create(
            signingHash: effectiveSigningHash,
            hashAlgorithm: effectiveHashAlgorithm,
            signature: signature,
            signatureAlgorithm: signatureAlgorithm,
            keyId: keyId,
            keyVersion: keyVersion,
            provider: provider,
            signedUtc: includeSignedUtc
                ? signedUtc ?? new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)
                : null,
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

    private static SigningMetadata CreateUncheckedSigningMetadata(string? signingHash)
    {
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(CreatePayload());

        return SigningMetadata.Create(
            signingHash: signingHash,
            hashAlgorithm: hash.HashAlgorithm,
            signature: "fake-signature",
            signatureAlgorithm: "FAKE-SIGNATURE-V1",
            keyId: "key-1",
            keyVersion: "v1",
            provider: "fake-provider",
            signedUtc: new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
            metadata: CreateDefaultMetadata(hash));
    }

    private static SignedGovernanceArtifact<string> CreateUncheckedArtifact(SigningMetadata signingMetadata)
    {
        CanonicalPayload payload = CreatePayload();
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);
        ConstructorInfo constructor = typeof(SignedGovernanceArtifact<string>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(CanonicalPayload), typeof(CanonicalPayloadHash), typeof(SigningMetadata) },
            modifiers: null)
            ?? throw new InvalidOperationException("SignedGovernanceArtifact constructor could not be located.");

        return (SignedGovernanceArtifact<string>)constructor.Invoke(new object?[]
        {
            "artifact",
            payload,
            hash,
            signingMetadata
        });
    }

    private static Dictionary<string, string> CreateDefaultMetadata(
        CanonicalPayloadHash hash,
        IReadOnlyDictionary<string, string>? metadataOverrides = null)
    {
        Dictionary<string, string> metadata = new(hash.ToSigningMetadata().Metadata, StringComparer.Ordinal)
        {
            ["policy_hash"] = "policy-hash",
            ["policy_version"] = "policy-v1"
        };

        if (metadataOverrides is not null)
        {
            foreach (KeyValuePair<string, string> item in metadataOverrides)
            {
                metadata[item.Key] = item.Value;
            }
        }

        return metadata;
    }

    private static Exception CreateProviderException(string scenario)
    {
        return scenario switch
        {
            "invalid-operation" => new InvalidOperationException("Verification provider unavailable."),
            "not-supported" => new NotSupportedException("Verification provider is not supported."),
            "timeout" => new TimeoutException("Verification provider timed out."),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown provider exception scenario.")
        };
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
        Exception? exception = null) : IAsiBackboneSignatureVerificationService
    {
        public bool WasCalled { get; private set; }

        public ValueTask<SignatureVerificationResult> VerifyAsync(
            SignatureVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;

            return exception is not null
                ? throw exception
                : ValueTask.FromResult(result);
        }
    }
}
