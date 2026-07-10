namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Represents the strongest provider-neutral action produced while sanitizing governance metadata.
/// </summary>
public enum GovernanceMetadataSanitizationAction
{
    /// <summary>
    /// Allows the metadata value to continue unchanged.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Allows the metadata value to continue while preserving an audit-worthy warning.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Replaces the metadata value with a classifier-supplied safe value.
    /// </summary>
    Redact = 2,

    /// <summary>
    /// Removes the metadata entry before budget validation or downstream use.
    /// </summary>
    Drop = 3,

    /// <summary>
    /// Denies the metadata collection from continuing to durable storage or emission.
    /// </summary>
    Deny = 4
}
