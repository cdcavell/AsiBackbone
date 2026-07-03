using System.Collections.ObjectModel;

namespace AsiBackbone.Core.Emissions;

/// <summary>
/// Represents the provider-neutral result of a governance emission attempt.
/// </summary>
public sealed class GovernanceEmissionResult
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private GovernanceEmissionResult(
        GovernanceEmissionStatus status,
        string? providerName,
        string? providerRecordId,
        DateTimeOffset? retryAfterUtc,
        GovernanceEmissionError? error,
        IReadOnlyDictionary<string, string> metadata)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Emission status must be defined.");
        }

        Status = status;
        ProviderName = NormalizeOptional(providerName);
        ProviderRecordId = NormalizeOptional(providerRecordId);
        RetryAfterUtc = retryAfterUtc?.ToUniversalTime();
        Error = error;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the provider-neutral result status.
    /// </summary>
    public GovernanceEmissionStatus Status { get; }

    /// <summary>
    /// Gets the provider name that handled or attempted the emission, when available.
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the provider-side record identifier, when one is returned and safe to keep.
    /// </summary>
    public string? ProviderRecordId { get; }

    /// <summary>
    /// Gets the UTC retry timestamp for deferred or retryable outcomes, when supplied.
    /// </summary>
    public DateTimeOffset? RetryAfterUtc { get; }

    /// <summary>
    /// Gets provider-neutral error information, when the emission did not deliver successfully.
    /// </summary>
    public GovernanceEmissionError? Error { get; }

    /// <summary>
    /// Gets minimized provider-neutral result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the emission was delivered successfully.
    /// </summary>
    public bool IsSuccess => Status is GovernanceEmissionStatus.Delivered;

    /// <summary>
    /// Gets a value indicating whether the emission should be retried according to provider-neutral status or error metadata.
    /// </summary>
    public bool ShouldRetry => Status is GovernanceEmissionStatus.Deferred or GovernanceEmissionStatus.RetryableFailure
        || Error?.IsRetryable == true;

    /// <summary>
    /// Gets a value indicating whether the result is terminal and should not be retried automatically.
    /// </summary>
    public bool IsTerminal => Status is GovernanceEmissionStatus.Delivered or GovernanceEmissionStatus.DeadLettered;

    /// <summary>
    /// Gets a value indicating whether result metadata is present.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates a pending emission result.
    /// </summary>
    public static GovernanceEmissionResult Pending(
        string? providerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.Pending,
            providerName,
            providerRecordId: null,
            retryAfterUtc: null,
            error: null,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a delivered emission result.
    /// </summary>
    public static GovernanceEmissionResult Delivered(
        string? providerName = null,
        string? providerRecordId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return DeliveredFromNormalizedMetadata(
            providerName,
            providerRecordId,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a deferred emission result.
    /// </summary>
    public static GovernanceEmissionResult Deferred(
        GovernanceEmissionError? error = null,
        DateTimeOffset? retryAfterUtc = null,
        string? providerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.Deferred,
            providerName ?? error?.ProviderName,
            providerRecordId: null,
            retryAfterUtc,
            error,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a failed emission result.
    /// </summary>
    public static GovernanceEmissionResult Failed(
        GovernanceEmissionError error,
        string? providerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.Failed,
            providerName ?? error.ProviderName,
            providerRecordId: null,
            retryAfterUtc: null,
            error,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a retryable failure emission result.
    /// </summary>
    public static GovernanceEmissionResult RetryableFailure(
        GovernanceEmissionError error,
        DateTimeOffset? retryAfterUtc = null,
        string? providerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.RetryableFailure,
            providerName ?? error.ProviderName,
            providerRecordId: null,
            retryAfterUtc,
            error,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a dead-letter emission result.
    /// </summary>
    public static GovernanceEmissionResult DeadLettered(
        GovernanceEmissionError error,
        string? providerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.DeadLettered,
            providerName ?? error.ProviderName,
            providerRecordId: null,
            retryAfterUtc: null,
            error,
            NormalizeMetadata(metadata));
    }

    internal static GovernanceEmissionResult DeliveredFromNormalizedMetadata(
        string? providerName = null,
        string? providerRecordId = null,
        IReadOnlyDictionary<string, string>? normalizedMetadata = null)
    {
        return new GovernanceEmissionResult(
            GovernanceEmissionStatus.Delivered,
            providerName,
            providerRecordId,
            retryAfterUtc: null,
            error: null,
            normalizedMetadata ?? EmptyMetadata);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(
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
}
