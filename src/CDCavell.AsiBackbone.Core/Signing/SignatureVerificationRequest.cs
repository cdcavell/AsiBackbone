using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents a provider-neutral request to verify signing metadata against a precomputed artifact hash.
/// </summary>
public sealed class SignatureVerificationRequest
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureVerificationRequest" /> class.
    /// </summary>
    public SignatureVerificationRequest(
        string signingHash,
        SigningMetadata signingMetadata,
        string? purpose = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingHash);
        ArgumentNullException.ThrowIfNull(signingMetadata);

        SigningHash = signingHash.Trim();
        SigningMetadata = signingMetadata;
        Purpose = NormalizeOptional(purpose);
        Metadata = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// Gets the precomputed artifact hash expected to have been signed.
    /// </summary>
    public string SigningHash { get; }

    /// <summary>
    /// Gets the provider-neutral signing metadata to verify.
    /// </summary>
    public SigningMetadata SigningMetadata { get; }

    /// <summary>
    /// Gets the host-defined verification purpose, when supplied.
    /// </summary>
    public string? Purpose { get; }

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
