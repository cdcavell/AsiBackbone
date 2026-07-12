using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Unit tests for validation and normalization performed by <see cref="ManagedKeySigningOptions" />.
/// </summary>
public sealed class ManagedKeySigningOptionsTests
{
    /// <summary>
    /// Verifies that a blank provider name is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankProviderName()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.ProviderName = " ";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Managed-key signing provider name is required.", exception.Message);
    }

    /// <summary>
    /// Verifies that a blank key identifier is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankKeyId()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.KeyId = "\t";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Managed-key signing key ID is required.", exception.Message);
    }

    /// <summary>
    /// Verifies that a blank signature algorithm is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankSignatureAlgorithm()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.SignatureAlgorithm = string.Empty;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Managed-key signing signature algorithm is required.", exception.Message);
    }

    /// <summary>
    /// Verifies that a blank hash algorithm is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankHashAlgorithm()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.HashAlgorithm = "  ";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Managed-key signing hash algorithm is required.", exception.Message);
    }

    /// <summary>
    /// Verifies that a negative retry-attempt count is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsNegativeRetryAttempts()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.MaxRetryAttempts = -1;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(
            "Managed-key signing retry attempts must be greater than or equal to zero.",
            exception.Message);
    }

    /// <summary>
    /// Verifies that a negative retry delay is rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsNegativeRetryDelay()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.RetryDelay = TimeSpan.FromTicks(-1);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(
            "Managed-key signing retry delay must be greater than or equal to zero.",
            exception.Message);
    }

    /// <summary>
    /// Verifies that zero retry attempts and a zero retry delay are valid boundary values.
    /// </summary>
    [Fact]
    public void ValidateAcceptsZeroRetryBoundaries()
    {
        ManagedKeySigningOptions options = CreateValidOptions();
        options.MaxRetryAttempts = 0;
        options.RetryDelay = TimeSpan.Zero;

        Exception? exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the production factory trims supplied descriptors and preserves explicit behavior settings.
    /// </summary>
    [Fact]
    public void CreateTrimsSuppliedValuesAndPreservesBehaviorSettings()
    {
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(17);

        ManagedKeySigningOptions options = ManagedKeySigningOptions.Create(
            keyId: "  managed-key-1  ",
            keyVersion: "  v7  ",
            providerName: "  provider-test  ",
            signatureAlgorithm: "  TEST-SIGNATURE  ",
            hashAlgorithm: "  SHA256  ",
            requireKeyVersion: false,
            returnUnsignedOnFailure: true,
            maxRetryAttempts: 0,
            retryDelay: retryDelay);

        Assert.Equal("managed-key-1", options.KeyId);
        Assert.Equal("v7", options.KeyVersion);
        Assert.Equal("provider-test", options.ProviderName);
        Assert.Equal("TEST-SIGNATURE", options.SignatureAlgorithm);
        Assert.Equal("SHA256", options.HashAlgorithm);
        Assert.False(options.RequireKeyVersion);
        Assert.True(options.ReturnUnsignedOnFailure);
        Assert.Equal(0, options.MaxRetryAttempts);
        Assert.Equal(retryDelay, options.RetryDelay);
    }

    /// <summary>
    /// Verifies that blank optional descriptors use defaults and blank key versions normalize to null.
    /// </summary>
    [Fact]
    public void CreateUsesDefaultsForBlankOptionalDescriptors()
    {
        ManagedKeySigningOptions options = ManagedKeySigningOptions.Create(
            keyId: " managed-key-1 ",
            keyVersion: " ",
            providerName: "\t",
            signatureAlgorithm: string.Empty,
            hashAlgorithm: null);

        Assert.Equal("managed-key-1", options.KeyId);
        Assert.Null(options.KeyVersion);
        Assert.Equal(ManagedKeySigningOptions.DefaultProviderName, options.ProviderName);
        Assert.Equal(ManagedKeySigningOptions.DefaultSignatureAlgorithm, options.SignatureAlgorithm);
        Assert.Equal(ManagedKeySigningOptions.DefaultHashAlgorithm, options.HashAlgorithm);
        Assert.True(options.RequireKeyVersion);
        Assert.False(options.ReturnUnsignedOnFailure);
        Assert.Equal(2, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.RetryDelay);
    }

    /// <summary>
    /// Verifies that a null optional key version normalizes to null.
    /// </summary>
    [Fact]
    public void CreateNormalizesNullKeyVersionToNull()
    {
        ManagedKeySigningOptions options = ManagedKeySigningOptions.Create(
            keyId: "managed-key-1",
            keyVersion: null,
            requireKeyVersion: false,
            retryDelay: TimeSpan.Zero);

        Assert.Null(options.KeyVersion);
    }

    /// <summary>
    /// Verifies that the local-validation factory always enables unsigned failure results
    /// while preserving other options.
    /// </summary>
    [Fact]
    public void CreateLocalValidationForcesUnsignedFailuresAndPreservesOtherOptions()
    {
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(23);

        ManagedKeySigningOptions options = ManagedKeySigningOptions.CreateLocalValidation(
            keyId: " managed-key-local ",
            keyVersion: " local-v2 ",
            providerName: " local-provider ",
            signatureAlgorithm: " LOCAL-SIGNATURE ",
            hashAlgorithm: " SHA-256 ",
            requireKeyVersion: false,
            maxRetryAttempts: 4,
            retryDelay: retryDelay);

        Assert.Equal("managed-key-local", options.KeyId);
        Assert.Equal("local-v2", options.KeyVersion);
        Assert.Equal("local-provider", options.ProviderName);
        Assert.Equal("LOCAL-SIGNATURE", options.SignatureAlgorithm);
        Assert.Equal("SHA-256", options.HashAlgorithm);
        Assert.False(options.RequireKeyVersion);
        Assert.True(options.ReturnUnsignedOnFailure);
        Assert.Equal(4, options.MaxRetryAttempts);
        Assert.Equal(retryDelay, options.RetryDelay);
    }

    private static ManagedKeySigningOptions CreateValidOptions()
    {
        return new ManagedKeySigningOptions
        {
            ProviderName = "provider-test",
            KeyId = "managed-key-1",
            KeyVersion = "v1",
            SignatureAlgorithm = "TEST-SIGNATURE",
            HashAlgorithm = "SHA-256",
            MaxRetryAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(1)
        };
    }
}
