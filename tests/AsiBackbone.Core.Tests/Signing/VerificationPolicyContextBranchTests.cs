using AsiBackbone.Core.Signing;
using Xunit;

namespace AsiBackbone.Core.Tests.Signing;

/// <summary>
/// Tests for the <see cref="VerificationPolicyContext"/> class, focusing on the behavior of the Create method and normalization of expectations and metadata.
/// </summary>
public sealed class VerificationPolicyContextBranchTests
{
    /// <summary>
    /// Tests that the Create method of the VerificationPolicyContext class correctly normalizes expectations and metadata, trimming whitespace and handling null values appropriately.
    /// </summary>
    [Fact]
    public void CreateNormalizesExpectationsAndMetadata()
    {
        var context = VerificationPolicyContext.Create(
            purpose: " audit-receipt ",
            expectedKeyId: " key-1 ",
            expectedKeyVersion: " v2 ",
            expectedPolicyVersion: " policy-v1 ",
            expectedPolicyHash: " policy-hash ",
            requiredProvider: " local-dev ",
            requiredHashAlgorithm: " SHA-256 ",
            metadata: new Dictionary<string, string>
            {
                [" context "] = " local-dev ",
                [" "] = " ignored ",
                ["nullable"] = null!
            });

        Assert.Equal("audit-receipt", context.Purpose);
        Assert.Equal("key-1", context.ExpectedKeyId);
        Assert.Equal("v2", context.ExpectedKeyVersion);
        Assert.Equal("policy-v1", context.ExpectedPolicyVersion);
        Assert.Equal("policy-hash", context.ExpectedPolicyHash);
        Assert.Equal("local-dev", context.RequiredProvider);
        Assert.Equal("SHA-256", context.RequiredHashAlgorithm);
        Assert.True(context.HasMetadata);
        Assert.Equal("local-dev", context.Metadata["context"]);
        Assert.Equal(string.Empty, context.Metadata["nullable"]);
        Assert.False(context.Metadata.ContainsKey(string.Empty));
    }

    /// <summary>
    /// Tests that the Create method of the VerificationPolicyContext class uses empty metadata for missing or blank entries, ensuring that the context is created with null or empty values as expected.
    /// </summary>
    [Fact]
    public void CreateUsesEmptyMetadataForMissingOrBlankEntries()
    {
        VerificationPolicyContext defaultContext = VerificationPolicyContext.Default;
        var blankOnlyContext = VerificationPolicyContext.Create(
            purpose: " ",
            expectedKeyId: " ",
            expectedKeyVersion: " ",
            expectedPolicyVersion: " ",
            expectedPolicyHash: " ",
            requiredProvider: " ",
            requiredHashAlgorithm: " ",
            metadata: new Dictionary<string, string>
            {
                [" "] = " ignored "
            });

        Assert.Null(defaultContext.Purpose);
        Assert.False(defaultContext.HasMetadata);
        Assert.Empty(defaultContext.Metadata);
        Assert.Null(blankOnlyContext.Purpose);
        Assert.Null(blankOnlyContext.ExpectedKeyId);
        Assert.Null(blankOnlyContext.ExpectedKeyVersion);
        Assert.Null(blankOnlyContext.ExpectedPolicyVersion);
        Assert.Null(blankOnlyContext.ExpectedPolicyHash);
        Assert.Null(blankOnlyContext.RequiredProvider);
        Assert.Null(blankOnlyContext.RequiredHashAlgorithm);
        Assert.False(blankOnlyContext.HasMetadata);
        Assert.Empty(blankOnlyContext.Metadata);
    }
}
