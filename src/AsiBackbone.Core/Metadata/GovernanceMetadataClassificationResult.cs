using AsiBackbone.Core.Results;

namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Represents the provider-neutral classification result for one governance metadata entry.
/// </summary>
public sealed class GovernanceMetadataClassificationResult
{
    /// <summary>
    /// Gets the default replacement value used by <see cref="Redact(string, string, string)" />.
    /// </summary>
    public const string DefaultRedactedValue = "[REDACTED]";

    private GovernanceMetadataClassificationResult(
        GovernanceMetadataSanitizationAction action,
        OperationReason? reason,
        string? replacementValue)
    {
        Action = action;
        Reason = reason;
        ReplacementValue = replacementValue;
    }

    /// <summary>
    /// Gets the action requested by the classifier.
    /// </summary>
    public GovernanceMetadataSanitizationAction Action { get; }

    /// <summary>
    /// Gets the stable machine-readable reason associated with a non-allow action.
    /// </summary>
    public OperationReason? Reason { get; }

    /// <summary>
    /// Gets the safe replacement value used when <see cref="Action" /> is <see cref="GovernanceMetadataSanitizationAction.Redact" />.
    /// </summary>
    public string? ReplacementValue { get; }

    /// <summary>
    /// Creates an allow result.
    /// </summary>
    /// <returns>An allow classification result.</returns>
    public static GovernanceMetadataClassificationResult Allow()
    {
        return new GovernanceMetadataClassificationResult(
            GovernanceMetadataSanitizationAction.Allow,
            reason: null,
            replacementValue: null);
    }

    /// <summary>
    /// Creates a warning result that permits the original value to continue.
    /// </summary>
    /// <param name="reasonCode">The stable machine-readable warning code.</param>
    /// <param name="reasonMessage">The curated warning message.</param>
    /// <returns>A warning classification result.</returns>
    public static GovernanceMetadataClassificationResult Warn(
        string reasonCode,
        string reasonMessage)
    {
        return CreateWithReason(
            GovernanceMetadataSanitizationAction.Warn,
            reasonCode,
            reasonMessage,
            replacementValue: null);
    }

    /// <summary>
    /// Creates a redaction result with a safe replacement value.
    /// </summary>
    /// <param name="reasonCode">The stable machine-readable redaction code.</param>
    /// <param name="reasonMessage">The curated redaction message.</param>
    /// <param name="replacementValue">The safe value that replaces the classified value.</param>
    /// <returns>A redaction classification result.</returns>
    public static GovernanceMetadataClassificationResult Redact(
        string reasonCode,
        string reasonMessage,
        string replacementValue = DefaultRedactedValue)
    {
        ArgumentNullException.ThrowIfNull(replacementValue);

        return CreateWithReason(
            GovernanceMetadataSanitizationAction.Redact,
            reasonCode,
            reasonMessage,
            replacementValue);
    }

    /// <summary>
    /// Creates a drop result that removes the metadata entry.
    /// </summary>
    /// <param name="reasonCode">The stable machine-readable drop code.</param>
    /// <param name="reasonMessage">The curated drop message.</param>
    /// <returns>A drop classification result.</returns>
    public static GovernanceMetadataClassificationResult Drop(
        string reasonCode,
        string reasonMessage)
    {
        return CreateWithReason(
            GovernanceMetadataSanitizationAction.Drop,
            reasonCode,
            reasonMessage,
            replacementValue: null);
    }

    /// <summary>
    /// Creates a deny result that prevents the metadata collection from continuing.
    /// </summary>
    /// <param name="reasonCode">The stable machine-readable denial code.</param>
    /// <param name="reasonMessage">The curated denial message.</param>
    /// <returns>A denial classification result.</returns>
    public static GovernanceMetadataClassificationResult Deny(
        string reasonCode,
        string reasonMessage)
    {
        return CreateWithReason(
            GovernanceMetadataSanitizationAction.Deny,
            reasonCode,
            reasonMessage,
            replacementValue: null);
    }

    private static GovernanceMetadataClassificationResult CreateWithReason(
        GovernanceMetadataSanitizationAction action,
        string reasonCode,
        string reasonMessage,
        string? replacementValue)
    {
        return new GovernanceMetadataClassificationResult(
            action,
            OperationReason.Create(reasonCode, reasonMessage),
            replacementValue);
    }
}
