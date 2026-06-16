using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Represents the result returned by a host-owned managed-key signing client.
/// </summary>
public sealed class ManagedKeySignResult
{
    private static readonly ReadOnlyDictionary<string, string> EmptyMetadata =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    private ManagedKeySignResult(
        string signature,
        string signatureAlgorithm,
        string keyId,
        string? keyVersion,
        string? providerOperationId,
        DateTimeOffset signedUtc,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureAlgorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        Signature = signature.Trim();
        SignatureAlgorithm = signatureAlgorithm.Trim();
        KeyId = keyId.Trim();
        KeyVersion = NormalizeOptional(keyVersion);
        ProviderOperationId = NormalizeOptional(providerOperationId);
        SignedUtc = signedUtc.ToUniversalTime();
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the provider-neutral encoded signature value or provider signature reference.
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// Gets the provider-neutral signature algorithm descriptor.
    /// </summary>
    public string SignatureAlgorithm { get; }

    /// <summary>
    /// Gets the managed key identifier or key URI reference used to sign.
    /// </summary>
    public string KeyId { get; }

    /// <summary>
    /// Gets the managed key version used to sign, when supplied by the provider.
    /// </summary>
    public string? KeyVersion { get; }

    /// <summary>
    /// Gets a safe provider operation identifier, when supplied.
    /// </summary>
    public string? ProviderOperationId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the provider completed signing.
    /// </summary>
    public DateTimeOffset SignedUtc { get; }

    /// <summary>
    /// Gets safe provider-neutral metadata returned by the managed-key client.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a successful managed-key sign result.
    /// </summary>
    public static ManagedKeySignResult Create(
        string signature,
        string signatureAlgorithm,
        string keyId,
        string? keyVersion,
        DateTimeOffset signedUtc,
        string? providerOperationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new ManagedKeySignResult(
            signature,
            signatureAlgorithm,
            keyId,
            keyVersion,
            providerOperationId,
            signedUtc,
            NormalizeMetadata(metadata));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> normalized = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (!string.IsNullOrWhiteSpace(item.Key))
            {
                normalized[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
            }
        }

        return normalized.Count == 0 ? EmptyMetadata : new ReadOnlyDictionary<string, string>(normalized);
    }
}
