namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Represents the provider-neutral result of a signature verification operation.
/// </summary>
public sealed class SignatureVerificationResult
{
    private SignatureVerificationResult(
        bool isValid,
        string status,
        string? failureCode,
        string? failureMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        IsValid = isValid;
        Status = status.Trim();
        FailureCode = NormalizeOptional(failureCode);
        FailureMessage = NormalizeOptional(failureMessage);
    }

    /// <summary>
    /// Gets a value indicating whether the signature was verified successfully.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets a provider-neutral verification status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets a provider-neutral failure code when verification did not succeed.
    /// </summary>
    public string? FailureCode { get; }

    /// <summary>
    /// Gets a provider-neutral failure message when verification did not succeed.
    /// </summary>
    public string? FailureMessage { get; }

    /// <summary>
    /// Creates a successful verification result.
    /// </summary>
    public static SignatureVerificationResult Verified()
    {
        return new SignatureVerificationResult(true, "Verified", null, null);
    }

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static SignatureVerificationResult Failed(string failureCode, string? failureMessage = null)
    {
        return new SignatureVerificationResult(false, "Failed", failureCode, failureMessage);
    }

    /// <summary>
    /// Creates a result indicating that no signature metadata was available to verify.
    /// </summary>
    public static SignatureVerificationResult Unsigned(string? failureMessage = null)
    {
        return new SignatureVerificationResult(false, "Unsigned", "signature.missing", failureMessage);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
