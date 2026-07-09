using System.Collections.ObjectModel;
using System.Text;

namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Provides optional normalization and budget validation helpers for host-owned governance metadata.
/// </summary>
/// <remarks>
/// This helper does not classify, redact, encrypt, or sanitize sensitive values. Hosts should treat
/// successful validation as a bounded-shape check only and still apply their own privacy, DLP,
/// retention, and signing policies before durable storage or emission.
/// </remarks>
public static class GovernanceMetadataBudgetValidator
{
    private const int EmptySerializedObjectBytes = 2;
    private const int SerializedEntryOverheadBytes = 6;

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private static readonly string[] EmptyViolations = [];

    /// <summary>
    /// Normalizes metadata by trimming keys and values, removing blank keys, and using ordinal key comparison.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Normalize(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> normalizedMetadata = new(metadata.Count, StringComparer.Ordinal);

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

    /// <summary>
    /// Validates metadata against the recommended budget or a host-supplied budget.
    /// </summary>
    public static GovernanceMetadataBudgetValidationResult Validate(
        IReadOnlyDictionary<string, string>? metadata,
        GovernanceMetadataBudget? budget = null)
    {
        GovernanceMetadataBudget activeBudget = budget ?? GovernanceMetadataBudget.Recommended;
        IReadOnlyDictionary<string, string> normalizedMetadata = Normalize(metadata);
        List<string>? violations = null;

        if (normalizedMetadata.Count > activeBudget.MaxCount)
        {
            AddViolation(
                ref violations,
                $"Metadata count {normalizedMetadata.Count} exceeds maximum metadata count {activeBudget.MaxCount}.");
        }

        foreach (KeyValuePair<string, string> item in normalizedMetadata)
        {
            if (item.Key.Length > activeBudget.MaxKeyLength)
            {
                AddViolation(
                    ref violations,
                    $"Metadata key '{item.Key}' length {item.Key.Length} exceeds maximum key length {activeBudget.MaxKeyLength}.");
            }

            if (item.Value.Length > activeBudget.MaxValueLength)
            {
                AddViolation(
                    ref violations,
                    $"Metadata value for key '{item.Key}' length {item.Value.Length} exceeds maximum value length {activeBudget.MaxValueLength}.");
            }

            string? reservedFragment = FindReservedKeyFragment(item.Key, activeBudget);
            if (reservedFragment is not null)
            {
                AddViolation(
                    ref violations,
                    $"Metadata key '{item.Key}' matches reserved or discouraged key fragment '{reservedFragment}'. Store opaque references, hashes, or classifications instead of secrets, credentials, tokens, connection strings, or raw PII.");
            }
        }

        int estimatedSerializedBytes = EstimateSerializedSizeBytesCore(normalizedMetadata);
        if (estimatedSerializedBytes > activeBudget.MaxSerializedBytes)
        {
            AddViolation(
                ref violations,
                $"Estimated serialized metadata size {estimatedSerializedBytes} bytes exceeds maximum serialized metadata size {activeBudget.MaxSerializedBytes} bytes.");
        }

        return GovernanceMetadataBudgetValidationResult.Create(
            normalizedMetadata,
            violations is null ? EmptyViolations : Array.AsReadOnly([.. violations]),
            estimatedSerializedBytes);
    }

    /// <summary>
    /// Normalizes and validates metadata, throwing when the supplied metadata exceeds the budget.
    /// </summary>
    public static IReadOnlyDictionary<string, string> NormalizeAndValidate(
        IReadOnlyDictionary<string, string>? metadata,
        GovernanceMetadataBudget? budget = null,
        string? parameterName = null)
    {
        GovernanceMetadataBudgetValidationResult result = Validate(metadata, budget);
        result.ThrowIfInvalid(parameterName ?? nameof(metadata));
        return result.NormalizedMetadata;
    }

    /// <summary>
    /// Estimates the UTF-8 serialized size of normalized metadata for budget comparison.
    /// </summary>
    public static int EstimateSerializedSizeBytes(IReadOnlyDictionary<string, string>? metadata)
    {
        return EstimateSerializedSizeBytesCore(Normalize(metadata));
    }

    private static int EstimateSerializedSizeBytesCore(IReadOnlyDictionary<string, string> normalizedMetadata)
    {
        if (normalizedMetadata.Count == 0)
        {
            return EmptySerializedObjectBytes;
        }

        int totalBytes = EmptySerializedObjectBytes;

        foreach (KeyValuePair<string, string> item in normalizedMetadata)
        {
            totalBytes += SerializedEntryOverheadBytes;
            totalBytes += Encoding.UTF8.GetByteCount(item.Key);
            totalBytes += Encoding.UTF8.GetByteCount(item.Value);
        }

        return totalBytes;
    }

    private static string? FindReservedKeyFragment(string key, GovernanceMetadataBudget budget)
    {
        if (budget.ReservedKeyFragments.Count == 0)
        {
            return null;
        }

        string normalizedKey = GovernanceMetadataBudget.NormalizeKeyForComparison(key);

        foreach (string reservedFragment in budget.ReservedKeyFragments)
        {
            if (normalizedKey.Contains(reservedFragment, StringComparison.Ordinal))
            {
                return reservedFragment;
            }
        }

        return null;
    }

    private static void AddViolation(ref List<string>? violations, string violation)
    {
        violations ??= [];
        violations.Add(violation);
    }
}
