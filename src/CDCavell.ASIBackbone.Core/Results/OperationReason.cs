namespace CDCavell.AsiBackbone.Core.Results;

/// <summary>
/// Represents a machine-readable reason associated with an operation result.
/// </summary>
public sealed class OperationReason
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationReason"/> class.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    public OperationReason(string code, string message)
        : this(code, message, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationReason"/> class.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="metadata">Optional metadata associated with the reason.</param>
    public OperationReason(
        string code,
        string message,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code.Trim();
        Message = message.Trim();
        Metadata = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// Gets the machine-readable reason code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable reason message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets optional metadata associated with the reason.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this reason has metadata.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates an operation reason.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>An operation reason.</returns>
    public static OperationReason Create(string code, string message)
    {
        return new OperationReason(code, message);
    }

    /// <summary>
    /// Creates an operation reason with metadata.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="metadata">Optional metadata associated with the reason.</param>
    /// <returns>An operation reason.</returns>
    public static OperationReason Create(
        string code,
        string message,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new OperationReason(code, message, metadata);
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
            : normalizedMetadata;
    }
}
