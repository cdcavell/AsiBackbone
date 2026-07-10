namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Provides normalized governance metadata to a provider-neutral metadata classifier.
/// </summary>
public sealed class GovernanceMetadataClassificationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GovernanceMetadataClassificationContext" /> class.
    /// </summary>
    /// <param name="key">The normalized metadata key being classified.</param>
    /// <param name="value">The normalized metadata value being classified.</param>
    /// <param name="metadata">The complete normalized metadata collection available for contextual classification.</param>
    public GovernanceMetadataClassificationContext(
        string key,
        string value,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(metadata);

        Key = key;
        Value = value;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the normalized metadata key being classified.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the normalized metadata value being classified.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the complete normalized metadata collection available for contextual classification.
    /// </summary>
    /// <remarks>
    /// Classifiers must treat this collection as read-only and must not place raw sensitive values into returned reason text.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
