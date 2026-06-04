using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Results;

public sealed class OperationResultOfTTests
{
    [Fact]
    public void SuccessCreatesSucceededResultWithValue()
    {
        var result = OperationResult.Success("approved");

        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
        Assert.True(result.HasValue);
        Assert.Equal("approved", result.Value);
        Assert.Empty(result.Reasons);
        Assert.Empty(result.ReasonCodes);
    }

    [Fact]
    public void SuccessWithWarningsStoresValueAndWarnings()
    {
        var result = OperationResult.Success(42, [" Rounded value. "]);

        Assert.True(result.Succeeded);
        Assert.True(result.HasValue);
        Assert.Equal(42, result.Value);
        Assert.Equal("Rounded value.", Assert.Single(result.Warnings));
    }

    [Fact]
    public void FailureWithCodeAndMessageCreatesFailedResult()
    {
        var result = OperationResult.Failure<string>(
            "validation.required",
            "Required value missing.");

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        Assert.False(result.HasValue);
        Assert.Equal("validation.required", Assert.Single(result.ReasonCodes));
    }

    [Fact]
    public void FailureValueAccessThrows()
    {
        var result = OperationResult.Failure<string>(
            "validation.required",
            "Required value missing.");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Equal("Cannot access the value of a failed operation result.", exception.Message);
    }

    [Fact]
    public void FailureWithMultipleReasonsStoresReasonCodes()
    {
        var result = OperationResult.Failure<int>([
            OperationReason.Create("policy.denied", "Policy denied the request."),
            OperationReason.Create("constraint.failed", "Constraint failed.")
        ]);

        Assert.False(result.Succeeded);
        Assert.Equal(["policy.denied", "constraint.failed"], result.ReasonCodes);
    }

    [Fact]
    public void FailureWithNoReasonsUsesDefaultFailureReason()
    {
        var result = OperationResult.Failure<int>([]);

        Assert.False(result.Succeeded);
        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }
}
