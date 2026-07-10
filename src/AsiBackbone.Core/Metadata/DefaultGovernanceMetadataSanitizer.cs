using AsiBackbone.Core.Results;

namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Provides the default provider-neutral governance metadata classification, sanitation, and budget-validation pipeline.
/// </summary>
/// <remarks>
/// The pipeline normalizes caller-owned metadata into a new collection, applies classifiers in registration order,
/// removes or redacts values as directed, and then applies <see cref="GovernanceMetadataBudgetValidator" /> to the
/// sanitized collection. Classifier exceptions are not converted into permissive results; they stop the pipeline so
/// hosts do not accidentally persist or emit metadata that was not successfully classified.
/// </remarks>
public sealed class DefaultGovernanceMetadataSanitizer : IGovernanceMetadataSanitizer
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        GovernanceMetadataBudgetValidator.Normalize(null);

    private readonly IReadOnlyList<IGovernanceMetadataClassifier> classifiers;
    private readonly GovernanceMetadataBudget budget;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultGovernanceMetadataSanitizer" /> class.
    /// </summary>
    /// <param name="classifiers">The host-owned classifiers applied in registration order.</param>
    /// <param name="budget">The metadata budget applied after classification and sanitation.</param>
    public DefaultGovernanceMetadataSanitizer(
        IEnumerable<IGovernanceMetadataClassifier>? classifiers = null,
        GovernanceMetadataBudget? budget = null)
    {
        IGovernanceMetadataClassifier[] resolvedClassifiers = classifiers?.ToArray() ?? [];

        foreach (IGovernanceMetadataClassifier classifier in resolvedClassifiers)
        {
            ArgumentNullException.ThrowIfNull(classifier);
        }

        this.classifiers = resolvedClassifiers;
        this.budget = budget ?? GovernanceMetadataBudget.Recommended;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceMetadataSanitizationResult> SanitizeAsync(
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<string, string> normalizedMetadata =
            GovernanceMetadataBudgetValidator.Normalize(metadata);
        Dictionary<string, string> sanitizedMetadata = new(normalizedMetadata.Count, StringComparer.Ordinal);
        List<OperationReason> reasons = [];
        GovernanceMetadataSanitizationAction overallAction = GovernanceMetadataSanitizationAction.Allow;

        foreach (KeyValuePair<string, string> item in normalizedMetadata)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GovernanceMetadataSanitizationAction entryAction = GovernanceMetadataSanitizationAction.Allow;
            string replacementValue = item.Value;
            var context = new GovernanceMetadataClassificationContext(
                item.Key,
                item.Value,
                normalizedMetadata);

            foreach (IGovernanceMetadataClassifier classifier in classifiers)
            {
                GovernanceMetadataClassificationResult classification = await classifier
                    .ClassifyAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                ArgumentNullException.ThrowIfNull(classification);

                entryAction = GetStricterAction(entryAction, classification.Action);

                if (classification.Reason is not null)
                {
                    reasons.Add(classification.Reason);
                }

                if (classification.Action is GovernanceMetadataSanitizationAction.Redact)
                {
                    replacementValue = classification.ReplacementValue
                        ?? GovernanceMetadataClassificationResult.DefaultRedactedValue;
                }
            }

            overallAction = GetStricterAction(overallAction, entryAction);

            if (entryAction is GovernanceMetadataSanitizationAction.Allow
                or GovernanceMetadataSanitizationAction.Warn)
            {
                sanitizedMetadata[item.Key] = item.Value;
            }
            else if (entryAction is GovernanceMetadataSanitizationAction.Redact)
            {
                sanitizedMetadata[item.Key] = replacementValue;
            }
        }

        GovernanceMetadataBudgetValidationResult budgetValidation =
            GovernanceMetadataBudgetValidator.Validate(sanitizedMetadata, budget);

        if (!budgetValidation.IsValid)
        {
            overallAction = GovernanceMetadataSanitizationAction.Deny;
            reasons.Add(OperationReason.Create(
                GovernanceMetadataSanitizationReasonCodes.BudgetViolation,
                "Governance metadata failed the configured shape or reserved-key budget."));
        }

        IReadOnlyDictionary<string, string> forwardMetadata =
            overallAction is GovernanceMetadataSanitizationAction.Deny
                ? EmptyMetadata
                : budgetValidation.NormalizedMetadata;

        IReadOnlyList<OperationReason> resultReasons = reasons.Count == 0
            ? []
            : reasons.AsReadOnly();

        return GovernanceMetadataSanitizationResult.Create(
            overallAction,
            forwardMetadata,
            resultReasons,
            budgetValidation);
    }

    private static GovernanceMetadataSanitizationAction GetStricterAction(
        GovernanceMetadataSanitizationAction current,
        GovernanceMetadataSanitizationAction candidate)
    {
        return !Enum.IsDefined(candidate)
            ? throw new ArgumentOutOfRangeException(
                nameof(candidate),
                candidate,
                "Metadata sanitation action must be defined.")
            : candidate > current ? candidate : current;
    }
}
