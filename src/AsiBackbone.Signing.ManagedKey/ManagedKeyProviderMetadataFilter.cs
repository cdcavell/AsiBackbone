using System.Collections.ObjectModel;

namespace AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Minimizes untrusted metadata returned by host-owned managed-key clients.
/// </summary>
internal static class ManagedKeyProviderMetadataFilter
{
    internal const int MaxMetadataCount = 6;
    internal const int MaxKeyLength = 64;
    internal const int MaxValueLength = 256;
    internal const int MaxAggregateLength = 1024;

    private static readonly ReadOnlyDictionary<string, string> EmptyMetadata =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    private static readonly Dictionary<string, string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["provider_region"] = "provider_region",
        ["provider_zone"] = "provider_zone",
        ["provider_service"] = "provider_service",
        ["provider_request_id"] = "provider_request_id",
        ["provider_status_code"] = "provider_status_code",
        ["provider_key_state"] = "provider_key_state"
    };

    /// <summary>
    /// Returns a bounded, allowlisted copy of provider metadata.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Filter(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> filtered = new(StringComparer.Ordinal);
        int aggregateLength = 0;

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (filtered.Count >= MaxMetadataCount)
            {
                break;
            }

            string candidateKey = item.Key.Trim();
            if (candidateKey.Length == 0 || candidateKey.Length > MaxKeyLength)
            {
                continue;
            }

            if (!AllowedKeys.TryGetValue(candidateKey, out string? canonicalKey))
            {
                continue;
            }

            string candidateValue = item.Value?.Trim() ?? string.Empty;
            if (candidateValue.Length > MaxValueLength || ContainsControlCharacter(candidateValue))
            {
                continue;
            }

            int entryLength = canonicalKey.Length + candidateValue.Length;
            if (aggregateLength + entryLength > MaxAggregateLength)
            {
                continue;
            }

            if (filtered.TryAdd(canonicalKey, candidateValue))
            {
                aggregateLength += entryLength;
            }
        }

        return filtered.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(filtered);
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char character in value)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }
}
