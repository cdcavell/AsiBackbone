using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Results;

public sealed class OperationReasonTests
{
    [Fact]
    public void CreateNormalizesCodeAndMessage()
    {
        var reason = OperationReason.Create(" validation.required ", " Required value missing. ");

        Assert.Equal("validation.required", reason.Code);
        Assert.Equal("Required value missing.", reason.Message);
        Assert.False(reason.HasMetadata);
        Assert.Empty(reason.Metadata);
    }

    [Fact]
    public void CreateWithMetadataNormalizesKeysAndValues()
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            [" field "] = " Name ",
            ["   "] = "Ignored",
            ["policy"] = " v1 "
        };

        var reason = OperationReason.Create(
            "policy.denied",
            "Policy denied the request.",
            metadata);

        Assert.True(reason.HasMetadata);
        Assert.Equal(2, reason.Metadata.Count);
        Assert.Equal("Name", reason.Metadata["field"]);
        Assert.Equal("v1", reason.Metadata["policy"]);
    }

    [Theory]
    [InlineData(null, "Message.")]
    [InlineData("", "Message.")]
    [InlineData("   ", "Message.")]
    [InlineData("code", null)]
    [InlineData("code", "")]
    [InlineData("code", "   ")]
    public void CreateThrowsForBlankCodeOrMessage(string? code, string? message)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => OperationReason.Create(code!, message!));
    }
}
