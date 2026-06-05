using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Results;

/// <summary>
/// Unit tests for the <see cref="OperationResult{TValue}"/> class, verifying the behavior of success and failure scenarios, including value storage, reason codes, warnings, and exception handling when accessing values from failed results.
/// </summary>
public sealed class OperationResultOfTTests
{
    /// <summary>
    /// Verifies that creating a successful OperationResult with a value correctly sets the Succeeded flag, stores the value, and does not include any reasons or reason codes.
    /// </summary>
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

    /// <summary>
    /// Verifies that creating a successful OperationResult with a value and warnings correctly sets the Succeeded flag, stores the value, and includes the provided warnings without any reasons or reason codes.
    /// </summary>
    [Fact]
    public void SuccessWithWarningsStoresValueAndWarnings()
    {
        var result = OperationResult.Success(42, [" Rounded value. "]);

        Assert.True(result.Succeeded);
        Assert.True(result.HasValue);
        Assert.Equal(42, result.Value);
        Assert.Equal("Rounded value.", Assert.Single(result.Warnings));
    }

    /// <summary>
    /// Verifies that creating a failed OperationResult with a specific reason code and message correctly sets the Failed flag, does not store a value, and includes the provided reason code without any warnings.
    /// </summary>
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

    /// <summary>
    /// Verifies that attempting to access the Value property of a failed OperationResult throws an InvalidOperationException with the expected message, ensuring that value access is properly restricted for failed results.
    /// </summary>
    [Fact]
    public void FailureValueAccessThrows()
    {
        var result = OperationResult.Failure<string>(
            "validation.required",
            "Required value missing.");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Equal("Cannot access the value of a failed operation result.", exception.Message);
    }

    /// <summary>
    /// Verifies that creating a failed OperationResult with multiple reasons correctly sets the Failed flag, does not store a value, and includes all provided reason codes in the ReasonCodes property without any warnings.
    /// </summary>
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

    /// <summary>
    /// Verifies that creating a failed OperationResult with no reasons defaults to a single reason with the code "operation.failed" and the message "Operation failed.", ensuring that failure results always include at least one reason for failure even when none are explicitly provided.
    /// </summary>
    [Fact]
    public void FailureWithNoReasonsUsesDefaultFailureReason()
    {
        var result = OperationResult.Failure<int>([]);

        Assert.False(result.Succeeded);
        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }

    /// <summary>
    /// Verifies that creating a failed OperationResult with reasons and warnings correctly sets the Failed flag, does not store a value, and includes all provided reason codes and warnings in their respective properties, ensuring that failure results can contain both reasons and warnings as expected.
    /// </summary>
    [Fact]
    public void FailureWithReasonsAndWarningsStoresReasonsAndWarnings()
    {
        var result = OperationResult.Failure<int>(
            [OperationReason.Create("policy.denied", "Policy denied the request.")],
            [" Needs review. ", ""]);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        Assert.False(result.HasValue);
        Assert.True(result.HasWarnings);
        Assert.Equal("policy.denied", Assert.Single(result.ReasonCodes));
        Assert.Equal("Needs review.", Assert.Single(result.Warnings));
    }

    /// <summary>
    /// Verifies that the generic failure factory method without reasons uses the default failure reason.
    /// </summary>
    [Fact]
    public void FailureWithDefaultGenericReasonUsesDefaultFailureReason()
    {
        var result = OperationResult.Failure<int>();

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        Assert.False(result.HasValue);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
        Assert.Empty(result.Warnings);
    }
}
