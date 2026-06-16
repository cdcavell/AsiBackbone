namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Evaluates provider-neutral signature verification results against host verification policy.
/// </summary>
public static class VerificationPolicyEvaluator
{
    /// <summary>
    /// Evaluates a signed governance artifact and verification result against verification policy.
    /// </summary>
    public static VerificationPolicyOutcome Evaluate<TArtifact>(
        SignedGovernanceArtifact<TArtifact> artifact,
        SignatureVerificationResult verificationResult,
        VerificationPolicyOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(verificationResult);

        return VerificationPolicyOutcome.CreateCore(
            artifact.ArtifactType,
            artifact.ArtifactId,
            artifact.SigningHash,
            artifact.HashAlgorithm,
            artifact.SigningMetadata,
            verificationResult,
            options);
    }

    /// <summary>
    /// Maps a provider-neutral verification result to a stable verification category.
    /// </summary>
    public static SignatureVerificationCategory Categorize(SignatureVerificationResult verificationResult)
    {
        ArgumentNullException.ThrowIfNull(verificationResult);

        if (verificationResult.IsValid)
        {
            return SignatureVerificationCategory.Valid;
        }

        string failureCode = verificationResult.FailureCode ?? string.Empty;
        string status = verificationResult.Status ?? string.Empty;

        if (Matches(status, "MissingSignature") || Matches(failureCode, "missing"))
        {
            return SignatureVerificationCategory.MissingSignature;
        }

        if (Matches(failureCode, "hash"))
        {
            return SignatureVerificationCategory.HashMismatch;
        }

        if (Matches(failureCode, "canonicalization") || Matches(failureCode, "payload-schema") || Matches(failureCode, "artifact"))
        {
            return SignatureVerificationCategory.CanonicalizationMismatch;
        }

        if (Matches(failureCode, "unsupported") || Matches(failureCode, "algorithm"))
        {
            return SignatureVerificationCategory.UnsupportedAlgorithm;
        }

        if (Matches(failureCode, "revoked") || Matches(failureCode, "disabled"))
        {
            return SignatureVerificationCategory.RevokedKey;
        }

        if ((Matches(failureCode, "unknown") && Matches(failureCode, "key"))
            || Matches(failureCode, "key-version")
            || Matches(failureCode, "key.mismatch")
            || Matches(failureCode, "key-mismatch"))
        {
            return SignatureVerificationCategory.UnknownKeyVersion;
        }

        if (Matches(failureCode, "provider-unavailable")
            || Matches(failureCode, "unavailable")
            || Matches(failureCode, "timeout")
            || Matches(failureCode, "network"))
        {
            return SignatureVerificationCategory.ProviderUnavailable;
        }

        if (Matches(failureCode, "invalid") || Matches(failureCode, "malformed") || Matches(failureCode, "signature"))
        {
            return SignatureVerificationCategory.InvalidSignature;
        }

        return SignatureVerificationCategory.Failed;
    }

    private static bool Matches(string value, string pattern)
    {
        return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
