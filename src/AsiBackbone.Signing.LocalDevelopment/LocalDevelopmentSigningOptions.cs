namespace AsiBackbone.Signing.LocalDevelopment;

/// <summary>
/// Configures the local-development signing provider.
/// </summary>
/// <remarks>
/// This provider is intended for local development, samples, and tests. It is not a production managed-key provider and does not create tamper-evidence by itself.
/// </remarks>
public sealed class LocalDevelopmentSigningOptions
{
    /// <summary>
    /// Gets the default provider descriptor returned in signing metadata.
    /// </summary>
    public const string DefaultProviderName = "local-development";

    /// <summary>
    /// Gets the default local-development key identifier.
    /// </summary>
    public const string DefaultKeyId = "local-dev-key";

    /// <summary>
    /// Gets the default local-development key version.
    /// </summary>
    public const string DefaultKeyVersion = "dev";

    /// <summary>
    /// Gets the default provider-neutral signature algorithm descriptor.
    /// </summary>
    public const string DefaultSignatureAlgorithm = "RSASSA-PKCS1-v1_5-SHA256-LOCAL-DEV";

    /// <summary>
    /// Gets the default RSA key size for generated local-development keys.
    /// </summary>
    public const int DefaultKeySizeBits = 2048;

    /// <summary>
    /// Gets or sets the provider descriptor returned in signing metadata.
    /// </summary>
    public string ProviderName { get; set; } = DefaultProviderName;

    /// <summary>
    /// Gets or sets the local-development key identifier returned in signing metadata.
    /// </summary>
    public string KeyId { get; set; } = DefaultKeyId;

    /// <summary>
    /// Gets or sets the local-development key version returned in signing metadata.
    /// </summary>
    public string KeyVersion { get; set; } = DefaultKeyVersion;

    /// <summary>
    /// Gets or sets the signature algorithm descriptor returned in signing metadata.
    /// </summary>
    public string SignatureAlgorithm { get; set; } = DefaultSignatureAlgorithm;

    /// <summary>
    /// Gets or sets the generated RSA key size in bits.
    /// </summary>
    public int KeySizeBits { get; set; } = DefaultKeySizeBits;

    /// <summary>
    /// Gets or sets a value indicating whether signing failures should return unsigned metadata with explicit failure details instead of throwing during normal signing flow.
    /// </summary>
    public bool ReturnUnsignedOnFailure { get; set; } = true;

    /// <summary>
    /// Creates options for the local-development signing provider.
    /// </summary>
    public static LocalDevelopmentSigningOptions Create(
        string? providerName = null,
        string? keyId = null,
        string? keyVersion = null,
        string? signatureAlgorithm = null,
        int keySizeBits = DefaultKeySizeBits,
        bool returnUnsignedOnFailure = true)
    {
        return new LocalDevelopmentSigningOptions
        {
            ProviderName = string.IsNullOrWhiteSpace(providerName)
                ? DefaultProviderName
                : providerName.Trim(),
            KeyId = string.IsNullOrWhiteSpace(keyId)
                ? DefaultKeyId
                : keyId.Trim(),
            KeyVersion = string.IsNullOrWhiteSpace(keyVersion)
                ? DefaultKeyVersion
                : keyVersion.Trim(),
            SignatureAlgorithm = string.IsNullOrWhiteSpace(signatureAlgorithm)
                ? DefaultSignatureAlgorithm
                : signatureAlgorithm.Trim(),
            KeySizeBits = keySizeBits,
            ReturnUnsignedOnFailure = returnUnsignedOnFailure
        };
    }
}
