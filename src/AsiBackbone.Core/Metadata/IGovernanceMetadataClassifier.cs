namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Classifies normalized governance metadata before durable storage, signing, telemetry, or provider emission.
/// </summary>
/// <remarks>
/// Implementations are host-owned and may use allow-lists, regular expressions, data-classification services,
/// tokenization services, or other deployment-specific privacy and DLP controls.
/// </remarks>
public interface IGovernanceMetadataClassifier
{
    /// <summary>
    /// Classifies one normalized governance metadata entry.
    /// </summary>
    /// <param name="context">The normalized metadata entry and contextual metadata collection.</param>
    /// <param name="cancellationToken">A token that can cancel asynchronous classification.</param>
    /// <returns>The provider-neutral classification result.</returns>
    ValueTask<GovernanceMetadataClassificationResult> ClassifyAsync(
        GovernanceMetadataClassificationContext context,
        CancellationToken cancellationToken = default);
}
