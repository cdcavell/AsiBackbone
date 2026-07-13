using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Covers managed-key retry backoff option validation and defaults.
/// </summary>
public sealed class ManagedKeySigningOptionsBackoffTests
{
    /// <summary>
    /// Verifies the existing base delay remains 200 milliseconds and the new cap defaults to five seconds.
    /// </summary>
    [Fact]
    public void CreateUsesDocumentedBackoffDefaults()
    {
        ManagedKeySigningOptions options = ManagedKeySigningOptions.Create("managed-key-1");

        Assert.Equal(TimeSpan.FromMilliseconds(200), options.RetryDelay);
        Assert.Equal(TimeSpan.FromSeconds(5), options.MaxRetryDelay);
        Assert.Equal(2, options.MaxRetryAttempts);
    }

    /// <summary>
    /// Verifies explicit backoff settings are preserved by both option factories.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FactoriesPreserveExplicitMaximumRetryDelay(bool localValidation)
    {
        TimeSpan baseDelay = TimeSpan.FromMilliseconds(75);
        TimeSpan maxDelay = TimeSpan.FromMilliseconds(900);

        ManagedKeySigningOptions options = localValidation
            ? ManagedKeySigningOptions.CreateLocalValidation(
                "managed-key-1",
                retryDelay: baseDelay,
                maxRetryDelay: maxDelay)
            : ManagedKeySigningOptions.Create(
                "managed-key-1",
                retryDelay: baseDelay,
                maxRetryDelay: maxDelay);

        Assert.Equal(baseDelay, options.RetryDelay);
        Assert.Equal(maxDelay, options.MaxRetryDelay);
    }

    /// <summary>
    /// Verifies a negative maximum delay is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsNegativeMaximumRetryDelay()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.MaxRetryDelay = TimeSpan.FromTicks(-1);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(
            "Managed-key signing maximum retry delay must be greater than or equal to zero.",
            exception.Message);
    }

    /// <summary>
    /// Verifies the maximum delay cannot be less than the base delay.
    /// </summary>
    [Fact]
    public void ValidateRejectsMaximumRetryDelayBelowBaseDelay()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.RetryDelay = TimeSpan.FromMilliseconds(200);
        options.MaxRetryDelay = TimeSpan.FromMilliseconds(199);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(
            "Managed-key signing maximum retry delay must be greater than or equal to the base retry delay.",
            exception.Message);
    }

    /// <summary>
    /// Verifies zero base and maximum delays retain immediate retry behavior.
    /// </summary>
    [Fact]
    public void ValidateAcceptsZeroBackoffBoundaries()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.RetryDelay = TimeSpan.Zero;
        options.MaxRetryDelay = TimeSpan.Zero;

        Assert.Null(Record.Exception(options.Validate));
    }

    private static ManagedKeySigningOptions CreateValidOptions()
    {
        return new ManagedKeySigningOptions
        {
            KeyId = "managed-key-1",
            KeyVersion = "v1",
            ProviderName = "managed-key-test",
            RetryDelay = TimeSpan.FromMilliseconds(100),
            MaxRetryDelay = TimeSpan.FromSeconds(1)
        };
    }
}
