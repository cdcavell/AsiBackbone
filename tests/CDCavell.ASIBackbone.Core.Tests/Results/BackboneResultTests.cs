using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Results;

public sealed class BackboneResultTests
{
    /// <summary>
    /// Verifies that the Success method creates a result with Succeeded = true, Failed = false, and no messages.
    /// </summary>
    [Fact]
    public void SuccessCreatesSucceededResult()
    {
        var result = BackboneResult.Success();

        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
        Assert.Empty(result.Messages);
    }

    /// <summary>
    /// Verifies that the Success method with a message creates a result with Succeeded = true, Failed = false, and the message is stored without leading/trailing whitespace.
    /// </summary>
    [Fact]
    public void SuccessWithMessageStoresNormalizedMessage()
    {
        var result = BackboneResult.Success(" Operation completed. ");

        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
        _ = Assert.Single(result.Messages);
        Assert.Equal("Operation completed.", result.Messages[0]);
    }

    /// <summary>
    /// Verifies that the Success method with multiple messages creates a result with Succeeded = true, Failed = false, and all messages are stored without leading/trailing whitespace.
    /// </summary>
    [Fact]
    public void SuccessWithBlankMessagesReturnsEmptyMessages()
    {
        var result = BackboneResult.Success(["", "   "]);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    /// <summary>
    /// Verifies that the Failure method creates a result with Succeeded = false, Failed = true, and the message is stored without leading/trailing whitespace.
    /// </summary>
    [Fact]
    public void FailureCreatesFailedResult()
    {
        var result = BackboneResult.Failure(" Operation failed for validation reasons. ");

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        _ = Assert.Single(result.Messages);
        Assert.Equal("Operation failed for validation reasons.", result.Messages[0]);
    }

    /// <summary>
    /// Verifies that the Failure method with multiple messages creates a result with Succeeded = false, Failed = true, and all messages are stored without leading/trailing whitespace.
    /// </summary>
    [Fact]
    public void FailureWithMessagesStoresNormalizedMessages()
    {
        var result = BackboneResult.Failure(
        [
            " First error. ",
            "",
            " Second error. "
        ]);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("First error.", result.Messages[0]);
        Assert.Equal("Second error.", result.Messages[1]);
    }

    /// <summary>
    /// Verifies that the Failure method with blank messages creates a result with Succeeded = false, Failed = true, and a single default failure message.
    /// </summary>
    [Fact]
    public void FailureWithBlankMessagesUsesDefaultFailureMessage()
    {
        var result = BackboneResult.Failure(["", "   "]);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        _ = Assert.Single(result.Messages);
        Assert.Equal("Operation failed.", result.Messages[0]);
    }
}
