using System.Collections.ObjectModel;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;

namespace AsiBackbone.Core.ThreatModeling;

/// <summary>
/// Represents a structured threat-aware assessment contributed during policy evaluation.
/// </summary>
public sealed class ThreatAssessment
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Defines the minimum confidence value accepted by a threat assessment.
    /// </summary>
    public const double MinimumConfidence = 0.0D;

    /// <summary>
    /// Defines the maximum confidence value accepted by a threat assessment.
    /// </summary>
    public const double MaximumConfidence = 1.0D;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreatAssessment" /> class.
    /// </summary>
    /// <param name="severity">The severity reported by the contributor.</param>
    /// <param name="category">The threat category reported by the contributor.</param>
    /// <param name="reasonCode">The machine-readable reason code.</param>
    /// <param name="description">The human-readable threat description.</param>
    /// <param name="recommendedOutcome">The governance outcome recommended by the contributor.</param>
    /// <param name="confidence">The contributor confidence from 0.0 to 1.0.</param>
    /// <param name="metadata">Optional host-supplied metadata retained on generated operation reasons.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="severity" /> or <paramref name="recommendedOutcome" /> is undefined,
    /// or when <paramref name="confidence" /> is outside the supported range.
    /// </exception>
    public ThreatAssessment(
        ThreatSeverity severity,
        string category,
        string reasonCode,
        string description,
        GovernanceDecisionOutcome recommendedOutcome,
        double confidence = MaximumConfidence,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Threat severity must be defined.");
        }

        if (!Enum.IsDefined(recommendedOutcome))
        {
            throw new ArgumentOutOfRangeException(
                nameof(recommendedOutcome),
                recommendedOutcome,
                "Recommended governance outcome must be defined.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (confidence is < MinimumConfidence or > MaximumConfidence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                $"{nameof(confidence)} must be between {MinimumConfidence} and {MaximumConfidence}.");
        }

        Severity = severity;
        Category = category.Trim();
        ReasonCode = reasonCode.Trim();
        Description = description.Trim();
        RecommendedOutcome = recommendedOutcome;
        Confidence = confidence;
        Metadata = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// Gets the severity reported by the contributor.
    /// </summary>
    public ThreatSeverity Severity { get; }

    /// <summary>
    /// Gets the threat category reported by the contributor.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the machine-readable reason code.
    /// </summary>
    public string ReasonCode { get; }

    /// <summary>
    /// Gets the human-readable threat description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the governance outcome recommended by the contributor.
    /// </summary>
    public GovernanceDecisionOutcome RecommendedOutcome { get; }

    /// <summary>
    /// Gets the contributor confidence from 0.0 to 1.0.
    /// </summary>
    public double Confidence { get; }

    /// <summary>
    /// Gets optional host-supplied metadata retained on generated operation reasons.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the assessment should influence the composed decision.
    /// </summary>
    public bool IsActionable => Severity is not ThreatSeverity.None || RecommendedOutcome is not GovernanceDecisionOutcome.Allowed;

    /// <summary>
    /// Creates a no-threat assessment.
    /// </summary>
    /// <returns>A no-threat assessment that does not influence decision composition.</returns>
    public static ThreatAssessment NoThreat()
    {
        return new ThreatAssessment(
            ThreatSeverity.None,
            ThreatCategories.None,
            "asibackbone.threat.none",
            "No threat indicators were reported.",
            GovernanceDecisionOutcome.Allowed,
            MinimumConfidence);
    }

    /// <summary>
    /// Creates a threat assessment.
    /// </summary>
    /// <param name="severity">The severity reported by the contributor.</param>
    /// <param name="category">The threat category reported by the contributor.</param>
    /// <param name="reasonCode">The machine-readable reason code.</param>
    /// <param name="description">The human-readable threat description.</param>
    /// <param name="recommendedOutcome">The governance outcome recommended by the contributor.</param>
    /// <param name="confidence">The contributor confidence from 0.0 to 1.0.</param>
    /// <param name="metadata">Optional host-supplied metadata retained on generated operation reasons.</param>
    /// <returns>A threat assessment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="severity" /> or <paramref name="recommendedOutcome" /> is undefined,
    /// or when <paramref name="confidence" /> is outside the supported range.
    /// </exception>
    public static ThreatAssessment Create(
        ThreatSeverity severity,
        string category,
        string reasonCode,
        string description,
        GovernanceDecisionOutcome recommendedOutcome,
        double confidence = MaximumConfidence,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new ThreatAssessment(
            severity,
            category,
            reasonCode,
            description,
            recommendedOutcome,
            confidence,
            metadata);
    }

    /// <summary>
    /// Converts the assessment into an operation reason with threat metadata.
    /// </summary>
    /// <param name="contributorName">Optional contributor name to include in metadata.</param>
    /// <param name="effectiveOutcome">Optional effective outcome selected by the evaluator after safety promotion.</param>
    /// <returns>An operation reason representing the assessment.</returns>
    public OperationReason ToOperationReason(
        string? contributorName = null,
        GovernanceDecisionOutcome? effectiveOutcome = null)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["threat.category"] = Category,
            ["threat.severity"] = Severity.ToString(),
            ["threat.recommended_outcome"] = RecommendedOutcome.ToString(),
            ["threat.effective_outcome"] = (effectiveOutcome ?? RecommendedOutcome).ToString(),
            ["threat.confidence"] = Confidence.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(contributorName))
        {
            metadata["threat.contributor"] = contributorName.Trim();
        }

        foreach (KeyValuePair<string, string> item in Metadata)
        {
            metadata[item.Key] = item.Value;
        }

        return OperationReason.Create(ReasonCode, Description, metadata);
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
            : new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }
}
