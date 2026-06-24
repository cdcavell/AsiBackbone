namespace AsiBackbone.Core.Classification;

/// <summary>
/// Represents provider-neutral DLP, classification, or governance-screening failure conditions.
/// </summary>
public enum DlpClassificationFailureKind
{
    /// <summary>
    /// The screening service or API was unavailable.
    /// </summary>
    ServiceUnavailable = 0,

    /// <summary>
    /// The screening operation timed out before producing a usable result.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// The screening operation completed but did not produce a determinate allow/block/classification result.
    /// </summary>
    IndeterminateResult = 2,

    /// <summary>
    /// The screening operation returned a result that blocks provider emission or governed execution.
    /// </summary>
    BlockedResult = 3,

    /// <summary>
    /// The screening operation returned a classification that requires configured policy handling before provider emission or execution.
    /// </summary>
    ClassifiedResult = 4
}
