using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Core.Emissions;

/// <summary>
/// Describes a minimized provider-neutral payload associated with a governance emission envelope.
/// </summary>
/// <remarks>
/// This type intentionally describes payload identity, shape, and safe diagnostics. It does not require raw protected content, provider SDK payloads, or cloud-specific envelopes.
/// </remarks>
public sealed class GovernanceEmissionPayload
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private GovernanceEmissionPayload(
        string payloadType,
        string? schemaVersion,
        string? contentType,
        string? contentHash,
        long? sizeBytes,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadType);

        PayloadType = payloadType.Trim();
        SchemaVersion = NormalizeOptional(schemaVersion);
        ContentType = NormalizeOptional(contentType);
        ContentHash = NormalizeOptional(contentHash);
        SizeBytes = NormalizeSizeBytes(sizeBytes);
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the provider-neutral payload type.
    /// </summary>
    public string PayloadType { get; }

    /// <summary>
    /// Gets the payload schema version, when available.
    /// </summary>
    public string? SchemaVersion { get; }

    /// <summary>
    /// Gets the payload content type, when available.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets a hash of the payload content, when available.
    /// </summary>
    public string? ContentHash { get; }

    /// <summary>
    /// Gets the payload size in bytes, when available.
    /// </summary>
    public long? SizeBytes { get; }

    /// <summary>
    /// Gets minimized provider-neutral payload metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether payload metadata is present.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates a minimized provider-neutral payload descriptor.
    /// </summary>
    public static GovernanceEmissionPayload Create(
        string payloadType,
        string? schemaVersion = null,
        string? contentType = null,
        string? contentHash = null,
        long? sizeBytes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new GovernanceEmissionPayload(
            payloadType,
            schemaVersion,
            contentType,
            contentHash,
            sizeBytes,
            NormalizeMetadata(metadata));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static long? NormalizeSizeBytes(long? sizeBytes)
    {
        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "Payload size must be greater than or equal to zero.");
        }

        return sizeBytes;
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
