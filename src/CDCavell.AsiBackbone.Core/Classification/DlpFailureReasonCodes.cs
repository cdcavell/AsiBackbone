namespace CDCavell.AsiBackbone.Core.Classification;

/// <summary>
/// Provides stable provider-neutral reason codes for DLP and classification failure policy decisions.
/// </summary>
public static class DlpFailureReasonCodes
{
    /// <summary>
    /// Reason code used when the screening service or API is unavailable.
    /// </summary>
    public const string ServiceUnavailable = "dlp.service_unavailable";

    /// <summary>
    /// Reason code used when screening times out.
    /// </summary>
    public const string Timeout = "dlp.timeout";

    /// <summary>
    /// Reason code used when screening returns an indeterminate result.
    /// </summary>
    public const string IndeterminateResult = "dlp.indeterminate_result";

    /// <summary>
    /// Reason code used when screening returns a blocked result.
    /// </summary>
    public const string BlockedResult = "dlp.blocked_result";

    /// <summary>
    /// Reason code used when screening returns a classified result that requires policy handling.
    /// </summary>
    public const string ClassifiedResult = "dlp.classified_result";

    /// <summary>
    /// Gets the reason code associated with a provider-neutral DLP or classification failure kind.
    /// </summary>
    /// <param name="failureKind">The failure kind.</param>
    /// <returns>The stable reason code.</returns>
    public static string GetFor(DlpClassificationFailureKind failureKind)
    {
        return failureKind switch
        {
            DlpClassificationFailureKind.ServiceUnavailable => ServiceUnavailable,
            DlpClassificationFailureKind.Timeout => Timeout,
            DlpClassificationFailureKind.IndeterminateResult => IndeterminateResult,
            DlpClassificationFailureKind.BlockedResult => BlockedResult,
            DlpClassificationFailureKind.ClassifiedResult => ClassifiedResult,
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "DLP failure kind must be defined.")
        };
    }
}
