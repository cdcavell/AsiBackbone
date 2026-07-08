using AsiBackbone.Core.Emissions;
using Xunit;

namespace AsiBackbone.Core.Tests.Emissions;

/// <summary>
/// Tests for the GovernanceEmissionResult and GovernanceEmissionError classes, focusing on the behavior of the result branches and error normalization.
/// </summary>
public sealed class GovernanceEmissionResultBranchTests
{
    /// <summary>
    /// Tests that the Delivered result branch correctly normalizes optional values and metadata, ensuring that whitespace is trimmed and empty keys are ignored.
    /// </summary>
    [Fact]
    public void DeliveredNormalizesOptionalValuesAndMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [" "] = "ignored",
            [" key "] = " value "
        };

        var result = GovernanceEmissionResult.Delivered(
            " provider ",
            " record-1 ",
            metadata);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsTerminal);
        Assert.False(result.ShouldRetry);
        Assert.True(result.HasMetadata);
        Assert.Equal("provider", result.ProviderName);
        Assert.Equal("record-1", result.ProviderRecordId);
        Assert.Equal("value", result.Metadata["key"]);
        Assert.False(result.Metadata.ContainsKey(" "));
    }

    /// <summary>
    /// Tests that the Pending result branch treats whitespace provider names and empty metadata as absent, resulting in a Pending status with no provider name and no metadata.
    /// </summary>
    [Fact]
    public void PendingTreatsWhitespaceProviderAndEmptyMetadataAsAbsent()
    {
        var result = GovernanceEmissionResult.Pending(
            " ",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [" "] = "ignored"
            });

        Assert.Equal(GovernanceEmissionStatus.Pending, result.Status);
        Assert.Null(result.ProviderName);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsTerminal);
        Assert.False(result.ShouldRetry);
        Assert.False(result.HasMetadata);
    }

    /// <summary>
    /// Tests that the Deferred result branch uses the error's provider name and retry after UTC, ensuring proper normalization of optional values.
    /// </summary>
    [Fact]
    public void DeferredUsesErrorProviderAndRetryAfterUtc()
    {
        DateTimeOffset retryAfter = new(2026, 7, 8, 8, 30, 0, TimeSpan.FromHours(-4));
        var error = GovernanceEmissionError.Create(
            "provider.deferred",
            "Provider deferred the emission.",
            isRetryable: true,
            providerName: " provider ");

        var result = GovernanceEmissionResult.Deferred(error, retryAfter);

        Assert.Equal(GovernanceEmissionStatus.Deferred, result.Status);
        Assert.Equal("provider", result.ProviderName);
        Assert.Equal(retryAfter.ToUniversalTime(), result.RetryAfterUtc);
        Assert.True(result.ShouldRetry);
        Assert.False(result.IsTerminal);
    }

    /// <summary>
    /// Tests that the Failed result branch correctly reflects the error's retryable flag, ensuring that non-retryable errors do not suggest a retry while retryable errors do.
    /// </summary>
    [Fact]
    public void FailedResultRetryBehaviorFollowsErrorRetryFlag()
    {
        var nonRetryable = GovernanceEmissionResult.Failed(
            GovernanceEmissionError.Create("provider.failed", "Provider failed.", isRetryable: false));
        var retryable = GovernanceEmissionResult.Failed(
            GovernanceEmissionError.Create("provider.retryable", "Provider failed transiently.", isRetryable: true));

        Assert.False(nonRetryable.ShouldRetry);
        Assert.False(nonRetryable.IsTerminal);
        Assert.True(retryable.ShouldRetry);
    }

    /// <summary>
    /// Tests that the RetryableFailure and DeadLettered result branches expose the expected terminal semantics, ensuring that retryable failures suggest a retry while dead-lettered results are terminal and do not suggest a retry.
    /// </summary>
    [Fact]
    public void RetryableFailureAndDeadLetteredExposeExpectedTerminalSemantics()
    {
        var retryableFailure = GovernanceEmissionResult.RetryableFailure(
            GovernanceEmissionError.Create("provider.retryable", "Provider failed transiently."));
        var deadLettered = GovernanceEmissionResult.DeadLettered(
            GovernanceEmissionError.Create("provider.deadletter", "Provider rejected the emission."));

        Assert.True(retryableFailure.ShouldRetry);
        Assert.False(retryableFailure.IsTerminal);
        Assert.False(deadLettered.ShouldRetry);
        Assert.True(deadLettered.IsTerminal);
    }

    /// <summary>
    /// Tests that the failure factory methods (Failed, RetryableFailure, DeadLettered) throw an ArgumentNullException when provided with a null error, ensuring that error details are required for these result branches.
    /// </summary>
    [Fact]
    public void FailureFactoriesRequireErrorDetails()
    {
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceEmissionResult.Failed(null!));
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceEmissionResult.RetryableFailure(null!));
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceEmissionResult.DeadLettered(null!));
    }

    /// <summary>
    /// Tests that the GovernanceEmissionError.Create method normalizes optional values (trimming whitespace) and rejects required blank values for code and message, ensuring that error details are valid and consistent.
    /// </summary>
    [Fact]
    public void GovernanceEmissionErrorNormalizesOptionalValuesAndRejectsRequiredBlanks()
    {
        var error = GovernanceEmissionError.Create(
            " code ",
            " message ",
            isRetryable: true,
            providerName: " provider ",
            providerErrorCode: " provider-code ");

        Assert.Equal("code", error.Code);
        Assert.Equal("message", error.Message);
        Assert.True(error.IsRetryable);
        Assert.Equal("provider", error.ProviderName);
        Assert.Equal("provider-code", error.ProviderErrorCode);
        _ = Assert.Throws<ArgumentException>(() => GovernanceEmissionError.Create(" ", "message"));
        _ = Assert.Throws<ArgumentException>(() => GovernanceEmissionError.Create("code", " "));
    }
}
