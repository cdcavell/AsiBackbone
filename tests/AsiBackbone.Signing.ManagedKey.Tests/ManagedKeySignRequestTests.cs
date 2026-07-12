using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Unit tests for <see cref="ManagedKeySignRequest" /> input validation and normalization.
/// </summary>
public sealed class ManagedKeySignRequestTests
{
    /// <summary>
    /// Verifies that null, empty, and whitespace signing hashes are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsInvalidSigningHash()
    {
        AssertRequiredValueRejected(
            value => new ManagedKeySignRequest(value!, "SHA-256", "TEST-SIGNATURE", "managed-key-1"),
            "signingHash");
    }

    /// <summary>
    /// Verifies that null, empty, and whitespace hash algorithms are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsInvalidHashAlgorithm()
    {
        AssertRequiredValueRejected(
            value => new ManagedKeySignRequest("abc123", value!, "TEST-SIGNATURE", "managed-key-1"),
            "hashAlgorithm");
    }

    /// <summary>
    /// Verifies that null, empty, and whitespace signature algorithms are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsInvalidSignatureAlgorithm()
    {
        AssertRequiredValueRejected(
            value => new ManagedKeySignRequest("abc123", "SHA-256", value!, "managed-key-1"),
            "signatureAlgorithm");
    }

    /// <summary>
    /// Verifies that null, empty, and whitespace key identifiers are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsInvalidKeyId()
    {
        AssertRequiredValueRejected(
            value => new ManagedKeySignRequest("abc123", "SHA-256", "TEST-SIGNATURE", value!),
            "keyId");
    }

    /// <summary>
    /// Verifies that required and optional string values are trimmed.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesRequiredAndOptionalStrings()
    {
        var request = new ManagedKeySignRequest(
            "  abc123  ",
            "  SHA-256  ",
            "  TEST-SIGNATURE  ",
            "  managed-key-1  ",
            "  v1  ",
            "  audit-ledger-record  ");

        Assert.Equal("abc123", request.SigningHash);
        Assert.Equal("SHA-256", request.HashAlgorithm);
        Assert.Equal("TEST-SIGNATURE", request.SignatureAlgorithm);
        Assert.Equal("managed-key-1", request.KeyId);
        Assert.Equal("v1", request.KeyVersion);
        Assert.Equal("audit-ledger-record", request.Purpose);
    }

    /// <summary>
    /// Verifies that blank optional string values normalize to null.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesBlankOptionalStringsToNull()
    {
        var request = new ManagedKeySignRequest(
            "abc123",
            "SHA-256",
            "TEST-SIGNATURE",
            "managed-key-1",
            keyVersion: " ",
            purpose: "\t");

        Assert.Null(request.KeyVersion);
        Assert.Null(request.Purpose);
    }

    /// <summary>
    /// Verifies that metadata keys and values are trimmed, blank keys are ignored, and key comparison is ordinal.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesMetadataUsingOrdinalComparison()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [" Region "] = " east ",
            ["Name"] = " upper ",
            ["name"] = " lower ",
            ["null-value"] = null!,
            [" "] = "discarded"
        };

        var request = new ManagedKeySignRequest(
            "abc123",
            "SHA-256",
            "TEST-SIGNATURE",
            "managed-key-1",
            metadata: metadata);

        Assert.Equal(4, request.Metadata.Count);
        Assert.Equal("east", request.Metadata["Region"]);
        Assert.Equal("upper", request.Metadata["Name"]);
        Assert.Equal("lower", request.Metadata["name"]);
        Assert.Equal(string.Empty, request.Metadata["null-value"]);
        Assert.False(request.Metadata.ContainsKey("NAME"));
        Assert.False(request.Metadata.ContainsKey(" "));
    }

    /// <summary>
    /// Verifies that null, empty, and fully discarded metadata inputs reuse the shared empty metadata shape.
    /// </summary>
    [Fact]
    public void ConstructorUsesSharedEmptyMetadataForEmptyInputs()
    {
        ManagedKeySignRequest nullMetadata = CreateRequest(metadata: null);
        ManagedKeySignRequest emptyMetadata = CreateRequest(new Dictionary<string, string>());
        ManagedKeySignRequest discardedMetadata = CreateRequest(new Dictionary<string, string>
        {
            [" "] = "discarded",
            ["\t"] = "discarded"
        });

        Assert.Empty(nullMetadata.Metadata);
        Assert.Same(nullMetadata.Metadata, emptyMetadata.Metadata);
        Assert.Same(nullMetadata.Metadata, discardedMetadata.Metadata);
    }

    /// <summary>
    /// Verifies that metadata normalization does not mutate or retain the caller-owned dictionary.
    /// </summary>
    [Fact]
    public void ConstructorPreservesCallerOwnedMetadataWithoutMutation()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [" key "] = " value "
        };

        ManagedKeySignRequest request = CreateRequest(metadata);

        Assert.Single(metadata);
        Assert.Equal(" value ", metadata[" key "]);
        Assert.Equal("value", request.Metadata["key"]);
        Assert.NotSame(metadata, request.Metadata);

        metadata[" key "] = "changed";
        metadata["new-key"] = "new-value";

        Assert.Equal("value", request.Metadata["key"]);
        Assert.False(request.Metadata.ContainsKey("new-key"));
    }

    private static ManagedKeySignRequest CreateRequest(IReadOnlyDictionary<string, string>? metadata)
    {
        return new ManagedKeySignRequest(
            "abc123",
            "SHA-256",
            "TEST-SIGNATURE",
            "managed-key-1",
            metadata: metadata);
    }

    private static void AssertRequiredValueRejected(
        Func<string?, ManagedKeySignRequest> requestFactory,
        string expectedParameterName)
    {
        ArgumentNullException nullException = Assert.Throws<ArgumentNullException>(() => requestFactory(null));
        ArgumentException emptyException = Assert.Throws<ArgumentException>(() => requestFactory(string.Empty));
        ArgumentException whitespaceException = Assert.Throws<ArgumentException>(() => requestFactory("  "));

        Assert.Equal(expectedParameterName, nullException.ParamName);
        Assert.Equal(expectedParameterName, emptyException.ParamName);
        Assert.Equal(expectedParameterName, whitespaceException.ParamName);
    }
}
