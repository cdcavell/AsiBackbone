using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Results;

/// <summary>
/// Unit tests for the <see cref="OperationResult"/> class, which represents the result of an operation in the ASI Backbone system, including success/failure status, reasons for failure, and any warnings.
/// </summary>
public sealed class OperationResultTests
{
    /// <summary>
    /// Verifies that the Success factory method creates a successful OperationResult with no reasons or warnings, and that the properties are set correctly.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Success factory method with warnings creates a successful OperationResult with normalized warnings.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Failure factory method with a code and message creates a failed OperationResult.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Failure factory method with multiple reasons stores the correct reason codes.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Failure factory method with no reasons uses the default failure reason.
    /// </summary>
    [Fact]
    public void FailureWithNoReasonsUsesDefaultFailureReason()
    {
        var result = OperationResult.Failure([]);

        Assert.False(result.Succeeded);
        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Failure factory method with warnings stores the correct normalized warnings.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Success factory method with null warnings uses an empty warnings collection and does not have warnings.
    /// </summary>
    [Fact]
    public void Success_WithNullWarnings_UsesEmptyWarnings()
    {
        var result = OperationResult.Success(null!);

        Assert.True(result.Succeeded);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// Verifies that the Success factory method with warnings that are null, empty, or whitespace filters out those warnings and does not have warnings.
    /// </summary>
    [Fact]
    public void Success_WithWhitespaceWarnings_FiltersWarnings()
    {
        string[] warnings = [null!, "", " ", "\t"];

        var result = OperationResult.Success(warnings);

        Assert.True(result.Succeeded);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// Verifies that the Success factory method with warnings that have leading/trailing whitespace trims those warnings and has warnings.
    /// </summary>
    [Fact]
    public void Success_WithWarnings_TrimsWarnings()
    {
        var result = OperationResult.Success([" First warning. ", " Second warning. "]);

        Assert.True(result.Succeeded);
        Assert.True(result.HasWarnings);
        Assert.Equal(["First warning.", "Second warning."], result.Warnings);
    }

    /// <summary>
    /// Verifies that the Failure factory method with null reasons uses the default failure reason.
    /// </summary>
    [Fact]
    public void Failure_WithNullReasons_UsesDefaultFailureReason()
    {
        var result = OperationResult.Failure((IEnumerable<OperationReason>?)null!);

        Assert.False(result.Succeeded);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Failure factory method with reasons that contain null filters out the null reasons and creates a failed OperationResult with the non-null reasons.
    /// </summary>
    [Fact]
    public void Failure_WithReasonsContainingNull_FiltersNullReasons()
    {
        OperationReason[] reasons =
        [
            OperationReason.Create("policy.denied", "Policy denied the request."),
            null!
        ];

        var result = OperationResult.Failure(reasons);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("policy.denied", reason.Code);
        Assert.Equal("Policy denied the request.", reason.Message);
    }

    /// <summary>
    /// Verifies that the ToFailure method returns the same instance if the OperationResult is already a failure, and returns a new failed OperationResult with the default failure reason if it is successful.
    /// </summary>
    [Fact]
    public void ToFailure_WhenAlreadyFailed_ReturnsSameInstance()
    {
        var failure = OperationResult.Failure("policy.denied", "Policy denied the request.");

        OperationResult result = failure.ToFailure();

        Assert.Same(failure, result);
    }

    /// <summary>
    /// Verifies that the ToFailure method returns a new failed OperationResult with the default failure reason if the original OperationResult is successful.
    /// </summary>
    [Fact]
    public void ToFailure_WhenSuccessful_ReturnsDefaultFailure()
    {
        OperationResult result = OperationResult.Success().ToFailure();

        Assert.False(result.Succeeded);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("operation.failed", reason.Code);
        Assert.Equal("Operation failed.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Failure factory method with a null reason throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void Failure_WithNullReason_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            OperationResult.Failure((OperationReason)null!));
    }

    /// <summary>
    /// Verifies that the Failure factory method with a null reason for a generic OperationResult throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void FailureOfT_WithNullReason_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            OperationResult.Failure<string>((OperationReason)null!));
    }
}
