using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents a provider-neutral request to sign a precomputed artifact hash.
/// </summary>
/// <remarks>
/// The request is intentionally hash-oriented so production providers can use key-based signing APIs without exposing raw signing secrets to Core.
/// </remarks>
public sealed class SigningRequest
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningRequest" /> class.
    /// </summary>
    public SigningRequest(
        string signingHash,
        string? hashAlgorithm = null,
        string? purpose = null,
        string? keyId = null,
        string? keyVersion = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingHash);

        SigningHash = signingHash.Trim();
        HashAlgorithm = NormalizeOptional(hashAlgorithm);
        Purpose = NormalizeOptional(purpose);
        KeyId = NormalizeOptional(keyId);
        KeyVersion = NormalizeOptional(keyVersion);
        Metadata = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// Gets the precomputed artifact hash to sign.
    /// </summary>
    public string SigningHash { get; }

    /// <summary>
    /// Gets the hash algorithm or descriptor associated with <see cref="SigningHash" />, when supplied.
    /// </summary>
    public string? HashAlgorithm { get; }

    /// <summary>
    /// Gets the host-defined signing purpose, when supplied.
    /// </summary>
    public string? Purpose { get; }

    /// <summary>
    /// Gets the requested signing key identifier, when supplied.
    /// </summary>
    public string? KeyId { get; }

    /// <summary>
    /// Gets the requested signing key version, when supplied.
    /// </summary>
    public string? KeyVersion { get; }

    /// <summary>
    /// Gets additional provider-neutral request metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether metadata is present.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

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
