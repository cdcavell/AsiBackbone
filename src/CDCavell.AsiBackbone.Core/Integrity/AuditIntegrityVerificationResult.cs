using System.Collections.ObjectModel;

namespace CDCavell.AsiBackbone.Core.Integrity;

/// <summary>
/// Represents the outcome of provider-neutral audit integrity verification.
/// </summary>
public sealed class AuditIntegrityVerificationResult
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    private AuditIntegrityVerificationResult(
        bool isValid,
        AuditIntegrityVerificationCategory category,
        string status,
        string? failureCode,
        string? failureMessage,
        string? chainId,
        long? sequence,
        string? recordId,
        IReadOnlyDictionary<string, string> safeMetadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Verification category must be defined.");
        }

        IsValid = isValid;
        Category = category;
        Status = status.Trim();
        FailureCode = NormalizeOptional(failureCode);
        FailureMessage = NormalizeOptional(failureMessage);
        ChainId = NormalizeOptional(chainId);
        Sequence = sequence;
        RecordId = NormalizeOptional(recordId);
        SafeMetadata = safeMetadata;
    }

    /// <summary>
    /// Gets a value indicating whether the integrity chain verified successfully.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the provider-neutral verification category.
    /// </summary>
    public AuditIntegrityVerificationCategory Category { get; }

    /// <summary>
    /// Gets a provider-neutral status string.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the failure code when verification did not succeed.
    /// </summary>
    public string? FailureCode { get; }

    /// <summary>
    /// Gets the failure message when verification did not succeed.
    /// </summary>
    public string? FailureMessage { get; }

    /// <summary>
    /// Gets the chain identifier associated with the result, when known.
    /// </summary>
    public string? ChainId { get; }

    /// <summary>
    /// Gets the sequence associated with the result, when known.
    /// </summary>
    public long? Sequence { get; }

    /// <summary>
    /// Gets the record identifier associated with the result, when known.
    /// </summary>
    public string? RecordId { get; }

    /// <summary>
    /// Gets safe-to-log verification metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> SafeMetadata { get; }

    /// <summary>
    /// Creates a valid verification result.
    /// </summary>
    public static AuditIntegrityVerificationResult Valid(string chainId, long linkCount, string tipHash)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["chain_id"] = chainId,
            ["link_count"] = linkCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["tip_hash"] = tipHash
        };

        return new AuditIntegrityVerificationResult(
            true,
            AuditIntegrityVerificationCategory.Valid,
            "Valid",
            null,
            null,
            chainId,
            linkCount,
            null,
            new ReadOnlyDictionary<string, string>(metadata));
    }

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static AuditIntegrityVerificationResult Failed(
        AuditIntegrityVerificationCategory category,
        string failureCode,
        string? failureMessage = null,
        AuditIntegrityLink? link = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        Dictionary<string, string> safeMetadata = new(StringComparer.Ordinal)
        {
            ["category"] = category.ToString(),
            ["failure_code"] = failureCode.Trim()
        };

        if (link is not null)
        {
            safeMetadata["chain_id"] = link.ChainId;
            safeMetadata["record_id"] = link.RecordId;
            safeMetadata["sequence"] = link.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (metadata is not null)
        {
            foreach (KeyValuePair<string, string> item in metadata)
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                {
                    safeMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
                }
            }
        }

        return new AuditIntegrityVerificationResult(
            false,
            category,
            "Failed",
            failureCode,
            failureMessage,
            link?.ChainId,
            link?.Sequence,
            link?.RecordId,
            safeMetadata.Count == 0 ? EmptyMetadata : new ReadOnlyDictionary<string, string>(safeMetadata));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
