namespace AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Represents a provider-neutral managed-key signing failure.
/// </summary>
public sealed class ManagedKeySigningException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedKeySigningException" /> class.
    /// </summary>
    public ManagedKeySigningException(
        string failureCode,
        string? message = null,
        bool isRetryable = false,
        Exception? innerException = null)
        : base(string.IsNullOrWhiteSpace(message) ? failureCode : message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        FailureCode = failureCode.Trim();
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// Gets the provider-neutral failure code.
    /// </summary>
    public string FailureCode { get; }

    /// <summary>
    /// Gets a value indicating whether retry may be appropriate.
    /// </summary>
    public bool IsRetryable { get; }
}
