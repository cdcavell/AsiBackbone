using AsiBackbone.Core.Results;

namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Represents the provider-neutral result of governance metadata sanitation and budget validation.
/// </summary>
public sealed class GovernanceMetadataSanitizationResult
{
    private GovernanceMetadataSanitizationResult(
        GovernanceMetadataSanitizationAction action,
        IReadOnlyDictionary<string, string> sanitizedMetadata,
        IReadOnlyList<OperationReason> reasons,
        GovernanceMetadataBudgetValidationResult budgetValidation)
    {
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Metadata sanitation action must be defined.");
        }

        ArgumentNullException.ThrowIfNull(sanitizedMetadata);
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(budgetValidation);

        Action = action;
        SanitizedMetadata = sanitizedMetadata;
        Reasons = reasons;
        BudgetValidation = budgetValidation;
    }

    /// <summary>
    /// Gets the strongest action produced by classification, sanitation, and budget validation.
    /// </summary>
    public GovernanceMetadataSanitizationAction Action { get; }

    /// <summary>
    /// Gets metadata that may be passed forward when <see cref="CanProceed" /> is <see langword="true" />.
    /// </summary>
    /// <remarks>
    /// This collection is empty when sanitation is denied so callers cannot accidentally persist or emit a denied payload.
    /// </remarks>
    public IReadOnlyDictionary<string, string> SanitizedMetadata { get; }

    /// <summary>
    /// Gets stable machine-readable reasons produced by classifiers or budget validation.
    /// </summary>
    public IReadOnlyList<OperationReason> Reasons { get; }

    /// <summary>
    /// Gets the budget validation result evaluated after classifier redaction and dropping are complete.
    /// </summary>
    public GovernanceMetadataBudgetValidationResult BudgetValidation { get; }

    /// <summary>
    /// Gets a value indicating whether the sanitized metadata may continue to downstream processing.
    /// </summary>
    public bool CanProceed => Action is not GovernanceMetadataSanitizationAction.Deny;

    /// <summary>
    /// Gets a value indicating whether sanitation denied the metadata collection.
    /// </summary>
    public bool IsDenied => !CanProceed;

    /// <summary>
    /// Creates a governance metadata sanitation result.
    /// </summary>
    /// <param name="action">The strongest sanitation action.</param>
    /// <param name="sanitizedMetadata">The safe metadata available for downstream processing.</param>
    /// <param name="reasons">The classifier and budget reasons.</param>
    /// <param name="budgetValidation">The post-sanitation budget validation result.</param>
    /// <returns>A governance metadata sanitation result.</returns>
    public static GovernanceMetadataSanitizationResult Create(
        GovernanceMetadataSanitizationAction action,
        IReadOnlyDictionary<string, string> sanitizedMetadata,
        IReadOnlyList<OperationReason> reasons,
        GovernanceMetadataBudgetValidationResult budgetValidation)
    {
        return new GovernanceMetadataSanitizationResult(
            action,
            sanitizedMetadata,
            reasons,
            budgetValidation);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when sanitation denied the metadata collection.
    /// </summary>
    /// <param name="parameterName">The optional caller parameter name associated with the metadata.</param>
    public void ThrowIfDenied(string? parameterName = null)
    {
        if (CanProceed)
        {
            return;
        }

        string message = Reasons.Count == 0
            ? "Governance metadata sanitation denied the metadata collection."
            : $"Governance metadata sanitation denied the metadata collection: {string.Join("; ", Reasons.Select(reason => reason.Message))}";

        throw new ArgumentException(
            message,
            string.IsNullOrWhiteSpace(parameterName) ? "metadata" : parameterName);
    }
}
