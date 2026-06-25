namespace AsiBackbone.Core.Emissions;

/// <summary>
/// Represents provider-neutral error information for a governance emission attempt.
/// </summary>
public sealed class GovernanceEmissionError
{
    private GovernanceEmissionError(
        string code,
        string message,
        bool isRetryable,
        string? providerName,
        string? providerErrorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code.Trim();
        Message = message.Trim();
        IsRetryable = isRetryable;
        ProviderName = NormalizeOptional(providerName);
        ProviderErrorCode = NormalizeOptional(providerErrorCode);
    }

    /// <summary>
    /// Gets the provider-neutral error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the provider-neutral diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a value indicating whether the error is expected to be retryable.
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Gets the provider name associated with the error, when available.
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the provider-specific error code, when safe and available.
    /// </summary>
    public string? ProviderErrorCode { get; }

    /// <summary>
    /// Creates provider-neutral error information for a governance emission attempt.
    /// </summary>
    /// <param name="code">The provider-neutral error code.</param>
    /// <param name="message">The provider-neutral diagnostic message.</param>
    /// <param name="isRetryable">A value indicating whether the error is expected to be retryable.</param>
    /// <param name="providerName">Optional provider name.</param>
    /// <param name="providerErrorCode">Optional provider-specific error code.</param>
    /// <returns>The governance emission error.</returns>
    public static GovernanceEmissionError Create(
        string code,
        string message,
        bool isRetryable = false,
        string? providerName = null,
        string? providerErrorCode = null)
    {
        return new GovernanceEmissionError(
            code,
            message,
            isRetryable,
            providerName,
            providerErrorCode);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
