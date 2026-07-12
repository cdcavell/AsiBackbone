using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Tests the untrusted managed-key provider metadata boundary.
/// </summary>
public sealed class ManagedKeyProviderMetadataFilterTests
{
    /// <summary>
    /// Verifies that documented provider-neutral keys are retained with predictable canonical casing.
    /// </summary>
    [Fact]
    public void FilterRetainsAllowlistedKeysUsingOrdinalCaseInsensitiveMatching()
    {
        var source = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [" PROVIDER_REGION "] = " east ",
            ["Provider_Request_Id"] = " request-42 ",
            ["provider_status_code"] = " 200 "
        };

        IReadOnlyDictionary<string, string> result = ManagedKeyProviderMetadataFilter.Filter(source);

        Assert.Equal(3, result.Count);
        Assert.Equal("east", result["provider_region"]);
        Assert.Equal("request-42", result["provider_request_id"]);
        Assert.Equal("200", result["provider_status_code"]);
        Assert.DoesNotContain("PROVIDER_REGION", result.Keys);
    }

    /// <summary>
    /// Verifies sensitive names, casing variants, separators, and prefix/suffix variants are rejected.
    /// </summary>
    [Theory]
    [InlineData("password")]
    [InlineData("PassWd")]
    [InlineData("db_password_value")]
    [InlineData("passwd")]
    [InlineData("apikey")]
    [InlineData("api-key")]
    [InlineData("api_key")]
    [InlineData("authorization")]
    [InlineData("AuthorizationHeader")]
    [InlineData("bearer")]
    [InlineData("bearer_token")]
    [InlineData("access-token")]
    [InlineData("client_secret")]
    [InlineData("connection.string")]
    [InlineData("cookie")]
    [InlineData("certificate")]
    [InlineData("private-key")]
    [InlineData("x_provider_region")]
    [InlineData("provider_region_backup")]
    public void FilterRejectsNonAllowlistedAndSensitiveKeys(string key)
    {
        var source = new Dictionary<string, string>
        {
            [key] = "must-not-flow"
        };

        IReadOnlyDictionary<string, string> result = ManagedKeyProviderMetadataFilter.Filter(source);

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies key, value, and scalar-value bounds.
    /// </summary>
    [Fact]
    public void FilterRejectsOversizedOrControlCharacterValues()
    {
        var source = new Dictionary<string, string>
        {
            [new string('k', ManagedKeyProviderMetadataFilter.MaxKeyLength + 1)] = "value",
            ["provider_region"] = new string('v', ManagedKeyProviderMetadataFilter.MaxValueLength + 1),
            ["provider_zone"] = "line1\nline2",
            ["provider_service"] = "kms"
        };

        IReadOnlyDictionary<string, string> result = ManagedKeyProviderMetadataFilter.Filter(source);

        Assert.Single(result);
        Assert.Equal("kms", result["provider_service"]);
    }

    /// <summary>
    /// Verifies provider dictionaries are copied and later caller mutation cannot change retained metadata.
    /// </summary>
    [Fact]
    public void ManagedKeySignResultCopiesAndMinimizesProviderMetadata()
    {
        var source = new Dictionary<string, string>
        {
            ["provider_region"] = "east",
            ["authorization"] = "Bearer secret"
        };

        ManagedKeySignResult result = ManagedKeySignResult.Create(
            "signature",
            "TEST-SIGNATURE",
            "managed-key-1",
            "v1",
            DateTimeOffset.UtcNow,
            metadata: source);

        source["provider_region"] = "west";
        source["provider_service"] = "late-change";

        Assert.Single(result.Metadata);
        Assert.Equal("east", result.Metadata["provider_region"]);
        Assert.DoesNotContain("authorization", result.Metadata.Keys);
        Assert.DoesNotContain("provider_service", result.Metadata.Keys);
    }

    /// <summary>
    /// Verifies null, empty, and fully rejected inputs reuse an immutable empty shape.
    /// </summary>
    [Fact]
    public void FilterReturnsEmptyMetadataWhenNothingSafeRemains()
    {
        IReadOnlyDictionary<string, string> nullResult = ManagedKeyProviderMetadataFilter.Filter(null);
        IReadOnlyDictionary<string, string> emptyResult = ManagedKeyProviderMetadataFilter.Filter(
            new Dictionary<string, string>());
        IReadOnlyDictionary<string, string> rejectedResult = ManagedKeyProviderMetadataFilter.Filter(
            new Dictionary<string, string> { ["password"] = "secret" });

        Assert.Empty(nullResult);
        Assert.Same(nullResult, emptyResult);
        Assert.Same(nullResult, rejectedResult);
    }
}
