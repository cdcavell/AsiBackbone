namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Provides provider-neutral helpers for verifying signed governance artifacts and applying verification policy.
/// </summary>
/// <remarks>
/// The verifier wrapper does not resolve provider-specific keys in Core and does not imply legal evidence, compliance certification, immutable storage, or tamper-evidence.
/// </remarks>
public static class GovernanceArtifactVerifier
{
    /// <summary>
    /// Verifies a signed governance artifact and maps the result to a host-facing policy outcome.
    /// </summary>
    public static async ValueTask<VerificationPolicyOutcome> VerifyAsync<TArtifact>(
        SignedGovernanceArtifact<TArtifact> artifact,
        IAsiBackboneSignatureVerificationService verificationService,
        VerificationPolicyOptions? options = null,
        VerificationPolicyContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(verificationService);
        cancellationToken.ThrowIfCancellationRequested();

        VerificationPolicyContext effectiveContext = context ?? VerificationPolicyContext.Default;
        SignatureVerificationResult? preflightResult = ValidateBeforeProvider(artifact, effectiveContext);

        if (preflightResult is not null)
        {
            return VerificationPolicyEvaluator.Evaluate(artifact, preflightResult, options);
        }

        try
        {
            SignatureVerificationResult verificationResult = await verificationService
                .VerifyAsync(
                    new SignatureVerificationRequest(
                        artifact.SigningHash,
                        artifact.SigningMetadata,
                        purpose: effectiveContext.Purpose ?? artifact.ArtifactType,
                        metadata: effectiveContext.Metadata),
                    cancellationToken)
                .ConfigureAwait(false);

            return VerificationPolicyEvaluator.Evaluate(artifact, verificationResult, options);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or TimeoutException)
        {
            SignatureVerificationResult providerUnavailableResult = SignatureVerificationResult.Failed(
                "signature.provider-unavailable",
                exception.GetType().Name);

            return VerificationPolicyEvaluator.Evaluate(artifact, providerUnavailableResult, options);
        }
    }

    private static SignatureVerificationResult? ValidateBeforeProvider<TArtifact>(
        SignedGovernanceArtifact<TArtifact> artifact,
        VerificationPolicyContext context)
    {
        SigningMetadata metadata = artifact.SigningMetadata;

        if (artifact.HasNoSignature || !metadata.HasSignature)
        {
            return SignatureVerificationResult.MissingSignature("The governance artifact does not carry signature metadata.");
        }

        if (string.IsNullOrWhiteSpace(metadata.SigningHash))
        {
            return SignatureVerificationResult.MissingSignature("The governance artifact does not carry the hash that was signed.");
        }

        if (!string.Equals(metadata.SigningHash, artifact.SigningHash, StringComparison.Ordinal))
        {
            return SignatureVerificationResult.Failed(
                "signature.hash-mismatch",
                "The signing metadata hash does not match the canonical artifact hash.");
        }

        if (metadata.HashAlgorithm is not null
            && !string.Equals(metadata.HashAlgorithm, artifact.HashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return SignatureVerificationResult.Failed(
                "signature.hash-algorithm-unsupported",
                "The signing metadata hash algorithm does not match the canonical artifact hash algorithm.");
        }

        if (context.RequiredHashAlgorithm is not null
            && !string.Equals(context.RequiredHashAlgorithm, artifact.HashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return SignatureVerificationResult.Failed(
                "signature.hash-algorithm-unsupported",
                "The canonical artifact hash algorithm does not match the required verification policy algorithm.");
        }

        if (!MatchesCanonicalMetadata(metadata, "artifact_id", artifact.ArtifactId)
            || !MatchesCanonicalMetadata(metadata, "artifact_type", artifact.ArtifactType)
            || !MatchesCanonicalMetadata(metadata, "canonicalization_version", artifact.CanonicalHash.CanonicalizationVersion)
            || !MatchesCanonicalMetadata(metadata, "payload_schema_version", artifact.CanonicalHash.PayloadSchemaVersion))
        {
            return SignatureVerificationResult.Failed(
                "signature.canonicalization-mismatch",
                "The signing metadata canonical artifact descriptors do not match the artifact being verified.");
        }

        if (context.ExpectedKeyId is not null
            && !string.Equals(context.ExpectedKeyId, metadata.KeyId, StringComparison.Ordinal))
        {
            return SignatureVerificationResult.Failed(
                "signature.key-version-unknown",
                "The signing key identifier does not match the verification policy expectation.");
        }

        if (context.ExpectedKeyVersion is not null
            && !string.Equals(context.ExpectedKeyVersion, metadata.KeyVersion, StringComparison.Ordinal))
        {
            return SignatureVerificationResult.Failed(
                "signature.key-version-unknown",
                "The signing key version does not match the verification policy expectation.");
        }

        if (context.RequiredProvider is not null
            && !string.Equals(context.RequiredProvider, metadata.Provider, StringComparison.Ordinal))
        {
            return SignatureVerificationResult.Failed(
                "signature.provider-unavailable",
                "The signing provider does not match the required verification policy provider.");
        }

        if (!MatchesOptionalPolicyMetadata(metadata, "policy_version", context.ExpectedPolicyVersion)
            || !MatchesOptionalPolicyMetadata(metadata, "policy_hash", context.ExpectedPolicyHash))
        {
            return SignatureVerificationResult.Failed(
                "signature.canonicalization-mismatch",
                "The signing metadata policy context does not match the verification policy expectation.");
        }

        return null;
    }

    private static bool MatchesCanonicalMetadata(SigningMetadata metadata, string key, string expectedValue)
    {
        return !metadata.Metadata.TryGetValue(key, out string? value)
            || string.Equals(value, expectedValue, StringComparison.Ordinal);
    }

    private static bool MatchesOptionalPolicyMetadata(SigningMetadata metadata, string key, string? expectedValue)
    {
        return expectedValue is null
            || (metadata.Metadata.TryGetValue(key, out string? value)
                && string.Equals(value, expectedValue, StringComparison.Ordinal));
    }
}
