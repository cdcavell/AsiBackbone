namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Sanitizes governance metadata before it reaches durable storage, signing, telemetry, or provider emission.
/// </summary>
public interface IGovernanceMetadataSanitizer
{
    /// <summary>
    /// Normalizes, classifies, sanitizes, and budget-validates governance metadata.
    /// </summary>
    /// <param name="metadata">The caller-owned governance metadata collection.</param>
    /// <param name="cancellationToken">A token that can cancel asynchronous classification.</param>
    /// <returns>A provider-neutral sanitation result.</returns>
    ValueTask<GovernanceMetadataSanitizationResult> SanitizeAsync(
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken = default);
}
