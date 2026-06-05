using CDCavell.ASIBackbone.Core.Constraints;
using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Results;

/// <summary>
/// Unit tests for the OperationReason class, which represents the reason for an operation's success or failure.
/// </summary>
public sealed class OperationReasonTests
{
    /// <summary>
    /// Verifies that the Create method normalizes the code and message by trimming whitespace and ensuring they are not blank.
    /// </summary>
    [Fact]
    public void CreateNormalizesCodeAndMessage()
    {
        var reason = OperationReason.Create(" validation.required ", " Required value missing. ");

        Assert.Equal("validation.required", reason.Code);
        Assert.Equal("Required value missing.", reason.Message);
        Assert.False(reason.HasMetadata);
        Assert.Empty(reason.Metadata);
    }

    /// <summary>
    /// Verifies that the Create metadata overload accepts null metadata and returns no metadata.
    /// </summary>
    [Fact]
    public void CreateWithNullMetadataReturnsNoMetadata()
    {
        var reason = OperationReason.Create(
            "policy.denied",
            "Policy denied the request.",
            null!);

        Assert.False(reason.HasMetadata);
        Assert.Empty(reason.Metadata);
    }

    /// <summary>
    /// Verifies that the Create method with metadata normalizes the metadata keys and values by trimming whitespace and ignoring blank keys.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Create method throws an <see cref="ArgumentException"/> when a null or blank code or message is provided.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <param name="message">The message to validate.</param>
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

    /// <summary>
    /// Verifies that the Deny and Warning methods of ConstraintEvaluationResult correctly create results with the provided reasons and that null reasons are filtered out.
    /// </summary>
    [Fact]
    public void DenyWithReasonCreatesDeniedResult()
    {
        var reason = OperationReason.Create(
            "constraint.region_blocked",
            "Region is blocked.");

        var result = ConstraintEvaluationResult.Deny(reason);

        Assert.True(result.IsDenied);
        Assert.True(result.HasReasons);
        Assert.Equal("constraint.region_blocked", Assert.Single(result.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Warning method of ConstraintEvaluationResult correctly creates a warning result with the provided reason and that null reasons are filtered out.
    /// </summary>
    [Fact]
    public void WarningWithReasonCreatesWarningResult()
    {
        var reason = OperationReason.Create(
            "constraint.review_recommended",
            "Review is recommended.");

        var result = ConstraintEvaluationResult.Warning(reason);

        Assert.True(result.IsWarning);
        Assert.True(result.HasReasons);
        Assert.Equal("constraint.review_recommended", Assert.Single(result.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Deny and Warning methods of ConstraintEvaluationResult correctly filter out null reasons when creating results from collections of reasons.
    /// </summary>
    [Fact]
    public void DenyWithReasonsContainingNullFiltersNullReasons()
    {
        OperationReason[] reasons =
        [
            OperationReason.Create("constraint.failed", "Constraint failed."),
            null!
        ];

        var result = ConstraintEvaluationResult.Deny(reasons);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.failed", reason.Code);
    }

    /// <summary>
    /// Verifies that the Warning method of ConstraintEvaluationResult correctly filters out null reasons when creating a warning result from a collection of reasons.
    /// </summary>
    [Fact]
    public void WarningWithReasonsContainingNullFiltersNullReasons()
    {
        OperationReason[] reasons =
        [
            OperationReason.Create("constraint.warning", "Constraint warning."),
            null!
        ];

        var result = ConstraintEvaluationResult.Warning(reasons);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.warning", reason.Code);
    }

    /// <summary>
    /// Verifies that the Deny and Warning methods of ConstraintEvaluationResult throw an <see cref="ArgumentNullException"/> when a null reason is provided.
    /// </summary>
    [Fact]
    public void DenyWithNullReasonThrows()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ConstraintEvaluationResult.Deny((OperationReason)null!));
    }

    /// <summary>
    /// Verifies that the Warning method of ConstraintEvaluationResult throws an <see cref="ArgumentNullException"/> when a null reason is provided.
    /// </summary>
    [Fact]
    public void WarningWithNullReasonThrows()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ConstraintEvaluationResult.Warning((OperationReason)null!));
    }

    /// <summary>
    /// Verifies that creating an OperationReason with empty metadata results in no metadata being stored, and that the HasMetadata property is false.
    /// </summary>
    [Fact]
    public void CreateWithEmptyMetadataReturnsNoMetadata()
    {
        var reason = OperationReason.Create(
            "policy.denied",
            "Policy denied the request.",
            new Dictionary<string, string>());

        Assert.False(reason.HasMetadata);
        Assert.Empty(reason.Metadata);
    }

    /// <summary>
    /// Verifies that creating an OperationReason with only blank metadata keys results in no metadata being stored, and that the HasMetadata property is false.
    /// </summary>
    [Fact]
    public void CreateWithOnlyBlankMetadataKeysReturnsNoMetadata()
    {
        var reason = OperationReason.Create(
            "policy.denied",
            "Policy denied the request.",
            new Dictionary<string, string>
            {
                [" "] = "ignored",
                ["\t"] = "also ignored"
            });

        Assert.False(reason.HasMetadata);
        Assert.Empty(reason.Metadata);
    }

    /// <summary>
    /// Verifies that creating an OperationReason with a null metadata value results in the value being stored as an empty string, and that the HasMetadata property is true since there is a valid key.
    /// </summary>
    [Fact]
    public void CreateWithNullMetadataValueStoresEmptyString()
    {
        var reason = OperationReason.Create(
            "policy.denied",
            "Policy denied the request.",
            new Dictionary<string, string>
            {
                [" source "] = null!
            });

        Assert.True(reason.HasMetadata);
        Assert.Equal(string.Empty, reason.Metadata["source"]);
    }

    /// <summary>
    /// Verifies that the constructor of OperationReason normalizes the code, message, and metadata keys and values by trimming whitespace, and that the HasMetadata property is correctly set based on the presence of valid metadata.
    /// </summary>
    [Fact]
    public void ConstructorWithMetadataNormalizesKeysAndValues()
    {
        var reason = new OperationReason(
            " policy.denied ",
            " Policy denied the request. ",
            new Dictionary<string, string>
            {
                [" source "] = " unit-test "
            });

        Assert.Equal("policy.denied", reason.Code);
        Assert.Equal("Policy denied the request.", reason.Message);
        Assert.True(reason.HasMetadata);
        Assert.Equal("unit-test", reason.Metadata["source"]);
    }
}
