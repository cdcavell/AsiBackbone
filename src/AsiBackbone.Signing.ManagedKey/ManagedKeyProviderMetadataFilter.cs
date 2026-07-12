using System.Collections.ObjectModel;

namespace AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Minimizes untrusted metadata returned by host-owned managed-key clients.
/// </summary>
internal static class ManagedKeyProviderMetadataFilter
{
    internal const int MaxMetadataCount = 16;
    internal const int MaxKeyLength = 64;
    internal const int MaxValueLength = 256;
    internal const int MaxAggregateLength = 2048;

    private static readonly ReadOnlyDictionary<string, string> EmptyMetadata =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "provider_region",
        "provider_zone",
        "provider_service",
        "provider_request_id",