namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Represents the result of optional governance metadata budget validation.
/// </summary>
public sealed class GovernanceMetadataBudgetValidationResult
{
    private GovernanceMetadataBudgetValidationResult(
        IReadOnlyDictionary<string, string> normalizedMetadata,
        IReadOnlyList<string> violations,
        int estimatedSerializedBytes)
    {
        ArgumentNullException.ThrowIfNull(normalizedMetadata);
        ArgumentNullException.ThrowIfNull(violations);

        NormalizedMetadata = normalizedMetadata;
        Violations = violations;
        EstimatedSerializedBytes = estimatedSerializedBytes;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata satisfied the supplied budget.
    /// </summary>
    public bool IsValid => Violations.Count == 0;

    /// <summary>
    /// Gets normalized metadata after trimming keys and values and dropping blank keys.
    /// </summary>
    public IReadOnlyDictionary<string, string> NormalizedMetadata { get; }

    /// <summary>
    /// Gets budget validation violations. Empty when <see cref="IsValid" /> is <see langword="true" />.
    /// </summary>
    public IReadOnlyList<string> Violations { get; }

    /// <summary>
    /// Gets the estimated UTF-8 serialized metadata size used for budget comparison.
    /// </summary>
    public int EstimatedSerializedBytes { get; }

    /// <summary>
    /// Creates a validation result.
    /// </summary>
    public static GovernanceMetadataBudgetValidationResult Create(
        IReadOnlyDictionary<string, string> normalizedMetadata,
        IReadOnlyList<string> violations,
        int estimatedSerializedBytes)
    {
        return new GovernanceMetadataBudgetValidationResult(
            normalizedMetadata,
            violations,
            estimatedSerializedBytes);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when validation failed.
    /// </summary>
    public void ThrowIfInvalid(string? parameterName = null)
    {
        if (IsValid)
        {
            return;
        }

        throw new ArgumentException(
            $"Metadata budget validation failed: {string.Join("; ", Violations)}",
            string.IsNullOrWhiteSpace(parameterName) ? "metadata" : parameterName);
    }
}
