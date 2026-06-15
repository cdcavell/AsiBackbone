namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents the provider-neutral result of a signing operation.
/// </summary>
public sealed class SigningResult
{
    private SigningResult(SigningMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        Metadata = metadata;
    }

    /// <summary>
    /// Gets a signing result with no signature metadata.
    /// </summary>
    public static SigningResult NoSignature { get; } = new(SigningMetadata.NoSignature);

    /// <summary>
    /// Gets the signing metadata returned by the signing provider or host.
    /// </summary>
    public SigningMetadata Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the result contains a signed artifact.
    /// </summary>
    public bool IsSigned => Metadata.IsSigned;

    /// <summary>
    /// Creates a signing result from provider-neutral signing metadata.
    /// </summary>
    public static SigningResult FromMetadata(SigningMetadata metadata)
    {
        return new SigningResult(metadata);
    }
}
