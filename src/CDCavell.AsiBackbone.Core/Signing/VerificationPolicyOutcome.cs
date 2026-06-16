using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents the host-facing outcome of signature verification policy evaluation.
/// </summary>
public sealed class VerificationPolicyOutcome
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private VerificationPolicyOutcome(
        bool isVerified,
        SignatureVerificationCategory category,
        VerificationPolicyAction action,
        string status,
        string? failureCode,
        string? failureMessage,
        string artifactType,
        string artifactId,
        string signingHash,
        string hashAlgorithm,
        string? keyId,
        string? keyVersion,
        string? signatureAlgorithm,
        string? provider,
        IReadOnlyDictionary<string, string> safeMetadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactType);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashAlgorithm);

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Verification category must be defined.");
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Verification policy action must be defined.");
        }

        IsVerified = isVerified;
        Category = category;
        Action = action;
        Status = status.Trim();
        FailureCode = NormalizeOptional(failureCode);
        FailureMessage = NormalizeOptional(failureMessage);
        ArtifactType = artifactType.Trim();
        ArtifactId = artifactId.Trim();
        SigningHash = signingHash.Trim();
        HashAlgorithm = hashAlgorithm.Trim();
        KeyId = NormalizeOptional(keyId);
        KeyVersion = NormalizeOptional(keyVersion);
        SignatureAlgorithm = NormalizeOptional(signatureAlgorithm);
        Provider = NormalizeOptional(provider);
        SafeMetadata = safeMetadata;
    }

    /// <summary>
    /// Gets a value indicating whether verification succeeded.
    /// </summary>
    public bool IsVerified { get; }

    /// <summary>
    /// Gets the provider-neutral verification category.
    /// </summary>
    public SignatureVerificationCategory Category { get; }

    /// <summary>
    /// Gets the host-facing action selected by verification policy.
    /// </summary>
    public VerificationPolicyAction Action { get; }

    /// <summary>
    /// Gets a value indicating whether the policy selected the allow action.
    /// </summary>
    public bool ShouldAllow => Action is VerificationPolicyAction.Allow;

    /// <summary>
    /// Gets the provider-neutral verification status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the provider-neutral failure code, when verification did not succeed.
    /// </summary>
    public string? FailureCode { get; }

    /// <summary>
    /// Gets the provider-neutral failure message, when verification did not succeed.
    /// </summary>
    public string? FailureMessage { get; }

    /// <summary>
    /// Gets the artifact type that was verified or evaluated.
    /// </summary>
    public string ArtifactType { get; }

    /// <summary>
    /// Gets the artifact identifier that was verified or evaluated.
    /// </summary>
    public string ArtifactId { get; }

    /// <summary>
    /// Gets the signing hash expected for verification.
    /// </summary>
    public string SigningHash { get; }

    /// <summary>
    /// Gets the hash algorithm expected for verification.
    /// </summary>
    public string HashAlgorithm { get; }

    /// <summary>
    /// Gets the signing key identifier, when supplied.
    /// </summary>
    public string? KeyId { get; }

    /// <summary>
    /// Gets the signing key version, when supplied.
    /// </summary>
    public string? KeyVersion { get; }

    /// <summary>
    /// Gets the signature algorithm descriptor, when supplied.
    /// </summary>
    public string? SignatureAlgorithm { get; }

    /// <summary>
    /// Gets the signing provider descriptor, when supplied.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets safe-to-log verification metadata. Signature values and secrets are never included.
    /// </summary>
    public IReadOnlyDictionary<string, string> SafeMetadata { get; }

    /// <summary>
    /// Creates a verification policy outcome.
    /// </summary>
    public static VerificationPolicyOutcome Create(
        SignedGovernanceArtifact<object> artifact,
        SignatureVerificationResult verificationResult,
        VerificationPolicyOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return CreateCore(
            artifact.ArtifactType,
            artifact.ArtifactId,
            artifact.SigningHash,
            artifact.HashAlgorithm,
            artifact.SigningMetadata,
            verificationResult,
            options);
    }

    internal static VerificationPolicyOutcome CreateCore(
        string artifactType,
        string artifactId,
        string signingHash,
        string hashAlgorithm,
        SigningMetadata signingMetadata,
        SignatureVerificationResult verificationResult,
        VerificationPolicyOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(signingMetadata);
        ArgumentNullException.ThrowIfNull(verificationResult);

        VerificationPolicyOptions effectiveOptions = options ?? VerificationPolicyOptions.Default;
        SignatureVerificationCategory category = VerificationPolicyEvaluator.Categorize(verificationResult);
        VerificationPolicyAction action = effectiveOptions.GetAction(category);
        IReadOnlyDictionary<string, string> safeMetadata = BuildSafeMetadata(
            artifactType,
            artifactId,
            signingHash,
            hashAlgorithm,
            signingMetadata,
            verificationResult,
            category,
            action);

        return new VerificationPolicyOutcome(
            verificationResult.IsValid,
            category,
            action,
            verificationResult.Status,
            verificationResult.FailureCode,
            verificationResult.FailureMessage,
            artifactType,
            artifactId,
            signingHash,
            hashAlgorithm,
            signingMetadata.KeyId,
            signingMetadata.KeyVersion,
            signingMetadata.SignatureAlgorithm,
            signingMetadata.Provider,
            safeMetadata);
    }

    private static IReadOnlyDictionary<string, string> BuildSafeMetadata(
        string artifactType,
        string artifactId,
        string signingHash,
        string hashAlgorithm,
        SigningMetadata signingMetadata,
        SignatureVerificationResult verificationResult,
        SignatureVerificationCategory category,
        VerificationPolicyAction action)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["artifact_id"] = artifactId,
            ["artifact_type"] = artifactType,
            ["category"] = category.ToString(),
            ["hash_algorithm"] = hashAlgorithm,
            ["policy_action"] = action.ToString(),
            ["signing_hash"] = signingHash,
            ["status"] = verificationResult.Status
        };

        AddIfPresent(metadata, "failure_code", verificationResult.FailureCode);
        AddIfPresent(metadata, "key_id", signingMetadata.KeyId);
        AddIfPresent(metadata, "key_version", signingMetadata.KeyVersion);
        AddIfPresent(metadata, "provider", signingMetadata.Provider);
        AddIfPresent(metadata, "signature_algorithm", signingMetadata.SignatureAlgorithm);

        if (signingMetadata.SignedUtc.HasValue)
        {
            metadata["signed_utc"] = signingMetadata.SignedUtc.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        foreach (KeyValuePair<string, string> item in signingMetadata.Metadata)
        {
            if (IsSafeSigningMetadataKey(item.Key))
            {
                metadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
            }
        }

        return metadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(metadata);
    }

    private static void AddIfPresent(Dictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static bool IsSafeSigningMetadataKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && !key.Contains("signature", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("token", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("credential", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("private", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
