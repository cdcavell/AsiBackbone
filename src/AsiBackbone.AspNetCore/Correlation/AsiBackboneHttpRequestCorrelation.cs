using System.Collections.ObjectModel;
using AsiBackbone.Core.Constraints;

namespace AsiBackbone.AspNetCore.Correlation;

/// <summary>
/// Represents framework-neutral request correlation data resolved from the current ASP.NET Core HTTP request.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AsiBackboneHttpRequestCorrelation" /> class.
/// </remarks>
/// <param name="correlationId">The resolved correlation identifier, when available.</param>
/// <param name="traceId">The resolved trace identifier, when available.</param>
/// <param name="metadata">Safe request metadata resolved from the host.</param>
public sealed class AsiBackboneHttpRequestCorrelation(
    string? correlationId = null,
    string? traceId = null,
    IReadOnlyDictionary<string, string>? metadata = null)
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Gets the request correlation identifier, when supplied by the host or propagated request headers.
    /// </summary>
    public string? CorrelationId { get; } = NormalizeOptional(correlationId);

    /// <summary>
    /// Gets the request trace identifier, when supplied by the host or current activity.
    /// </summary>
    public string? TraceId { get; } = NormalizeOptional(traceId);

    /// <summary>
    /// Gets safe request metadata supplied by the ASP.NET Core adapter.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; } = NormalizeMetadata(metadata);

    /// <summary>
    /// Gets a value indicating whether safe request metadata is available.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates a framework-neutral constraint evaluation context from the resolved request correlation data.
    /// </summary>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <param name="metadata">Optional host-provided metadata to merge with safe request metadata.</param>
    /// <returns>A framework-neutral constraint evaluation context.</returns>
    public AsiBackboneConstraintEvaluationContext ToEvaluationContext(
        string? policyVersion = null,
        string? policyHash = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AsiBackboneConstraintEvaluationContext(
            CorrelationId,
            policyVersion,
            policyHash,
            MergeMetadata(metadata));
    }

    /// <summary>
    /// Merges safe request metadata with host-provided metadata.
    /// </summary>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>A normalized metadata dictionary.</returns>
    public IReadOnlyDictionary<string, string> MergeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if ((metadata is null || metadata.Count == 0) && Metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> merged = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in Metadata)
        {
            AddIfValid(merged, item.Key, item.Value);
        }

        if (metadata is not null)
        {
            foreach (KeyValuePair<string, string> item in metadata)
            {
                AddIfValid(merged, item.Key, item.Value);
            }
        }

        return merged.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(merged);
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

        Dictionary<string, string> normalizedMetadata = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            AddIfValid(normalizedMetadata, item.Key, item.Value);
        }

        return normalizedMetadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }

    private static void AddIfValid(Dictionary<string, string> metadata, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        metadata[key.Trim()] = value?.Trim() ?? string.Empty;
    }
}
