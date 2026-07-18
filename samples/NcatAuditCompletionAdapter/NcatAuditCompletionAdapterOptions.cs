namespace AsiBackbone.Samples.NcatAuditCompletionAdapter;

/// <summary>
/// Configures source-side delivery behavior for the optional NCAT audit-completion adapter.
/// </summary>
public sealed record NcatAuditCompletionAdapterOptions
{
    /// <summary>
    /// Gets the provider label placed in the governed execution receipt.
    /// </summary>
    public string PersistenceProvider { get; init; } = "NCAT";

    /// <summary>
    /// Gets the optional attempt number at which a persistence failure is returned as dead-lettered.
    /// </summary>
    /// <remarks>
    /// A <see langword="null" /> value keeps lifecycle persistence failures retryable indefinitely.
    /// The source completion entry is never acknowledged as delivered for retryable or dead-letter results.
    /// </remarks>
    public int? DeadLetterAfterAttempts { get; init; }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PersistenceProvider);

        if (DeadLetterAfterAttempts is < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DeadLetterAfterAttempts),
                DeadLetterAfterAttempts,
                "Dead-letter attempt threshold must be at least one when configured.");
        }
    }
}
