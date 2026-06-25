using System.Collections.ObjectModel;

namespace AsiBackbone.Core.Signing;

/// <summary>
/// Represents provider-neutral signing metadata that can be carried by audit receipts, emission records, or provider adapters without requiring a concrete signing implementation.
/// </summary>
/// <remarks>
/// The metadata stores key references and signature descriptors only. It must not contain raw signing secrets, private keys, connection strings, credentials, or provider-specific secret material.
/// </remarks>
public sealed class SigningMetadata
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private SigningMetadata(
        string? signingHash,
        string? hashAlgorithm,
        string? signature,
        string? signatureAlgorithm,
        string? keyId,
        string? keyVersion,
        string? provider,
        DateTimeOffset? signedUtc,
        IReadOnlyDictionary<string, string> metadata)
    {
        SigningHash = NormalizeOptional(signingHash);
        HashAlgorithm = NormalizeOptional(hashAlgorithm);
        Signature = NormalizeOptional(signature);
        SignatureAlgorithm = NormalizeOptional(signatureAlgorithm);
        KeyId = NormalizeOptional(keyId);
        KeyVersion = NormalizeOptional(keyVersion);
        Provider = NormalizeOptional(provider);
        SignedUtc = signedUtc?.ToUniversalTime();
        Metadata = metadata;
    }

    /// <summary>
    /// Gets metadata with no hash, key reference, signature, provider, or signed timestamp.
    /// </summary>
    public static SigningMetadata NoSignature { get; } = new(
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        EmptyMetadata);

    /// <summary>
    /// Gets the hash that was signed or is intended to be signed, when supplied by the host or signing provider.
    /// </summary>
    public string? SigningHash { get; }

    /// <summary>
    /// Gets the hash algorithm or hash descriptor associated with <see cref="SigningHash" />, when supplied.
    /// </summary>
    public string? HashAlgorithm { get; }

    /// <summary>
    /// Gets the provider-neutral signature value or encoded signature reference, when supplied.
    /// </summary>
    public string? Signature { get; }

    /// <summary>
    /// Gets the provider-neutral signature algorithm descriptor, when supplied.
    /// </summary>
    public string? SignatureAlgorithm { get; }

    /// <summary>
    /// Gets the signing key identifier, when supplied by a host or signing provider.
    /// </summary>
    public string? KeyId { get; }

    /// <summary>
    /// Gets the signing key version, when supplied by a host or signing provider.
    /// </summary>
    public string? KeyVersion { get; }

    /// <summary>
    /// Gets the provider or key-management system descriptor, when supplied.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets the UTC timestamp when the signature was produced, when supplied.
    /// </summary>
    public DateTimeOffset? SignedUtc { get; }

    /// <summary>
    /// Gets additional provider-neutral signing metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether a signature value or signature reference is present.
    /// </summary>
    public bool HasSignature => Signature is not null;

    /// <summary>
    /// Gets a value indicating whether any signing key reference is present.
    /// </summary>
    public bool HasKeyReference => KeyId is not null || KeyVersion is not null;

    /// <summary>
    /// Gets a value indicating whether this metadata represents a signed artifact.
    /// </summary>
    public bool IsSigned => Signature is not null && SignatureAlgorithm is not null && KeyId is not null;

    /// <summary>
    /// Gets a value indicating whether additional signing metadata is present.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates provider-neutral signing metadata.
    /// </summary>
    public static SigningMetadata Create(
        string? signingHash = null,
        string? hashAlgorithm = null,
        string? signature = null,
        string? signatureAlgorithm = null,
        string? keyId = null,
        string? keyVersion = null,
        string? provider = null,
        DateTimeOffset? signedUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new SigningMetadata(
            signingHash,
            hashAlgorithm,
            signature,
            signatureAlgorithm,
            keyId,
            keyVersion,
            provider,
            signedUtc,
            NormalizeMetadata(metadata));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> normalizedMetadata = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            normalizedMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return normalizedMetadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }
}
