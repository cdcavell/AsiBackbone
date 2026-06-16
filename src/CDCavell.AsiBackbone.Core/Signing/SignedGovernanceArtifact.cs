namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents an AsiBackbone governance artifact together with its canonical payload, canonical hash,
/// and optional provider-neutral signing metadata.
/// </summary>
/// <typeparam name="TArtifact">The governance artifact type.</typeparam>
/// <remarks>
/// This type preserves the boundary between unsigned, signing-ready, and signed artifacts. A signed artifact
/// has provider metadata attached, but verification, immutable storage, hash chaining, and external anchoring
/// remain separate host or provider responsibilities.
/// </remarks>
public sealed class SignedGovernanceArtifact<TArtifact>
{
    private SignedGovernanceArtifact(
        TArtifact artifact,
        CanonicalPayload canonicalPayload,
        CanonicalPayloadHash canonicalHash,
        SigningMetadata signingMetadata)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(canonicalPayload);
        ArgumentNullException.ThrowIfNull(canonicalHash);
        ArgumentNullException.ThrowIfNull(signingMetadata);

        if (!string.Equals(canonicalPayload.ArtifactType, canonicalHash.ArtifactType, StringComparison.Ordinal)
            || !string.Equals(canonicalPayload.ArtifactId, canonicalHash.ArtifactId, StringComparison.Ordinal)
            || !string.Equals(canonicalPayload.PayloadSchemaVersion, canonicalHash.PayloadSchemaVersion, StringComparison.Ordinal)
            || !string.Equals(canonicalPayload.CanonicalizationVersion, canonicalHash.CanonicalizationVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException("The canonical payload and canonical hash must describe the same governance artifact.", nameof(canonicalHash));
        }

        Artifact = artifact;
        CanonicalPayload = canonicalPayload;
        CanonicalHash = canonicalHash;
        SigningMetadata = signingMetadata;
    }

    /// <summary>
    /// Gets the original governance artifact.
    /// </summary>
    public TArtifact Artifact { get; }

    /// <summary>
    /// Gets the deterministic canonical payload used for hashing and signing.
    /// </summary>
    public CanonicalPayload CanonicalPayload { get; }

    /// <summary>
    /// Gets the canonical payload hash metadata.
    /// </summary>
    public CanonicalPayloadHash CanonicalHash { get; }

    /// <summary>
    /// Gets provider-neutral signing metadata. This may be empty, signing-ready, failed, or signed metadata.
    /// </summary>
    public SigningMetadata SigningMetadata { get; }

    /// <summary>
    /// Gets the stable artifact type bound into the canonical payload and signing metadata.
    /// </summary>
    public string ArtifactType => CanonicalHash.ArtifactType;

    /// <summary>
    /// Gets the stable artifact identifier bound into the canonical payload and signing metadata.
    /// </summary>
    public string ArtifactId => CanonicalHash.ArtifactId;

    /// <summary>
    /// Gets the hash algorithm used to compute <see cref="SigningHash" />.
    /// </summary>
    public string HashAlgorithm => CanonicalHash.HashAlgorithm;

    /// <summary>
    /// Gets the canonical payload hash value that is signed or made signing-ready.
    /// </summary>
    public string SigningHash => CanonicalHash.HashValue;

    /// <summary>
    /// Gets a value indicating whether provider signing metadata includes a signature.
    /// </summary>
    public bool IsSigned => SigningMetadata.IsSigned;

    /// <summary>
    /// Gets a value indicating whether hash metadata is present but no signature has been attached.
    /// </summary>
    public bool IsSigningReady => !SigningMetadata.HasSignature && SigningMetadata.SigningHash is not null;

    /// <summary>
    /// Gets a value indicating whether the artifact carries no signing metadata.
    /// </summary>
    public bool IsUnsigned => !SigningMetadata.HasSignature && SigningMetadata.SigningHash is null;

    /// <summary>
    /// Creates an unsigned artifact wrapper. The canonical payload and hash are preserved, but no signing metadata is attached.
    /// </summary>
    public static SignedGovernanceArtifact<TArtifact> Unsigned(
        TArtifact artifact,
        CanonicalPayload canonicalPayload,
        CanonicalPayloadHash canonicalHash)
    {
        return new SignedGovernanceArtifact<TArtifact>(
            artifact,
            canonicalPayload,
            canonicalHash,
            SigningMetadata.NoSignature);
    }

    /// <summary>
    /// Creates a signing-ready artifact wrapper. Hash metadata is attached without implying that a signature exists.
    /// </summary>
    public static SignedGovernanceArtifact<TArtifact> SigningReady(
        TArtifact artifact,
        CanonicalPayload canonicalPayload,
        CanonicalPayloadHash canonicalHash,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new SignedGovernanceArtifact<TArtifact>(
            artifact,
            canonicalPayload,
            canonicalHash,
            canonicalHash.ToSigningMetadata(metadata));
    }

    /// <summary>
    /// Creates an artifact wrapper from signing metadata returned by a host or provider package.
    /// </summary>
    public static SignedGovernanceArtifact<TArtifact> FromSigningMetadata(
        TArtifact artifact,
        CanonicalPayload canonicalPayload,
        CanonicalPayloadHash canonicalHash,
        SigningMetadata signingMetadata)
    {
        ArgumentNullException.ThrowIfNull(signingMetadata);

        return new SignedGovernanceArtifact<TArtifact>(
            artifact,
            canonicalPayload,
            canonicalHash,
            MergeCanonicalHashMetadata(canonicalHash, signingMetadata));
    }

    private static SigningMetadata MergeCanonicalHashMetadata(
        CanonicalPayloadHash canonicalHash,
        SigningMetadata signingMetadata)
    {
        if (signingMetadata.SigningHash is not null
            && !string.Equals(signingMetadata.SigningHash, canonicalHash.HashValue, StringComparison.Ordinal))
        {
            throw new ArgumentException("Signing metadata hash must match the canonical payload hash.", nameof(signingMetadata));
        }

        Dictionary<string, string> metadata = new(canonicalHash.ToSigningMetadata().Metadata, StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in signingMetadata.Metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            metadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return SigningMetadata.Create(
            signingHash: canonicalHash.HashValue,
            hashAlgorithm: string.IsNullOrWhiteSpace(signingMetadata.HashAlgorithm)
                ? canonicalHash.HashAlgorithm
                : signingMetadata.HashAlgorithm,
            signature: signingMetadata.Signature,
            signatureAlgorithm: signingMetadata.SignatureAlgorithm,
            keyId: signingMetadata.KeyId,
            keyVersion: signingMetadata.KeyVersion,
            provider: signingMetadata.Provider,
            signedUtc: signingMetadata.SignedUtc,
            metadata: metadata);
    }
}
