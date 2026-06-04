using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Results;

public sealed class OperationResultTests
{
    [Fact]
    public void SuccessCreatesSucceededResult()
    {
        var result = OperationResult.Success();

        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
        Assert.Empty(result.Reasons);
        Assert.Empty(result.ReasonCodes);
        Assert.Empty(result.Warnings);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void SuccessWithWarningsStoresNormalizedWarnings()
    {
        var result = OperationResult.Success([
            " First warning. ",
            "",
            " Second warning. "
        ]);

        Assert.True(result.Succeeded);
        Assert.True(result.HasWarnings);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Equal("First warning.", result.Warnings[0]);
        Assert.Equal("Second warning.", result.Warnings[1]);
    }

    [Fact]
    public void FailureWithCodeAndMessageCreatesFailedResult()
    {
        var result = OperationResult.Failure("validation.required", "Required value missing.");

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("validation.required", reason.Code);
        Assert.Equal("Required value missing.", reason.Message);
        Assert.Equal("validation.required", Assert.Single(result.ReasonCodes));
    }

    [Fact]
    public void FailureWithMultipleReasonsStoresReasonCodes()
    {
        var result = OperationResult.Failure([
            OperationReason.Create("policy.denied", "Policy denied the request."),
            OperationReason.Create("constraint.failed", "Constraint failed.")
        ]);

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Reasons.Count);
        Assert.Equal(["policy.denied", "constraint.failed"], result.ReasonCodes);
    }

    [Fact]
    public void FailureWithNoReasonsUsesDefaultFailureReason()
    {
        var result = OperationResult.Failure([]);

        Assert.False(result.Succeeded);
        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }

    [Fact]
    public void FailureWithWarningsStoresNormalizedWarnings()
    {
        var result = OperationResult.Failure(
            [OperationReason.Create("policy.denied", "Policy denied the request.")],
            [" Needs review. ", ""]);

        Assert.False(result.Succeeded);
        Assert.True(result.HasWarnings);
        Assert.Equal("Needs review.", Assert.Single(result.Warnings));
    }
}
