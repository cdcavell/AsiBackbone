using AsiBackbone.Core.Signing;
using Xunit;

namespace AsiBackbone.Core.Tests.Signing;

public sealed class VerificationRequestBranchTests
{
    [Fact]
    public void RequestNormalizesPurposeAndMetadata()
    {
        SigningMetadata metadata = SigningMetadata.NoSignature;
        var request = new SignatureVerificationRequest(
            " hash-123 ",
            metadata,
            purpose: " audit-receipt ",
            metadata: new Dictionary<string, string>
            {
                [" context "] = " local-dev ",
                [" "] = " ignored ",
                ["nullable"] = null!
            });

        Assert.Equal("hash-123", request.SigningHash);
        Assert.Equal("audit-receipt", request.Purpose);
        Assert.Same(metadata, request.SigningMetadata);
        Assert.True(request.HasMetadata);
        Assert.Equal("local-dev", request.Metadata["context"]);
        Assert.Equal(string.Empty, request.Metadata["nullable"]);
        Assert.False(request.Metadata.ContainsKey(string.Empty));
    }

    [Fact]
    public void RequestUsesEmptyMetadataForMissingOrBlankEntries()
    {
        SigningMetadata metadata = SigningMetadata.NoSignature;
        var defaultRequest = new SignatureVerificationRequest("hash-123", metadata, purpose: " ");
        var blankOnlyRequest = new SignatureVerificationRequest(
            "hash-123",
            metadata,
            metadata: new Dictionary<string, string>
            {
                [" "] = " ignored "
            });

        Assert.Null(defaultRequest.Purpose);
        Assert.False(defaultRequest.HasMetadata);
        Assert.Empty(defaultRequest.Metadata);
        Assert.False(blankOnlyRequest.HasMetadata);
        Assert.Empty(blankOnlyRequest.Metadata);
    }
}
