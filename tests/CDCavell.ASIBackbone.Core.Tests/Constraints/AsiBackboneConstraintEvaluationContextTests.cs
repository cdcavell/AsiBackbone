using CDCavell.ASIBackbone.Core.Constraints;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Constraints;

/// <summary>
/// Unit tests for <see cref="AsiBackboneConstraintEvaluationContext"/> to verify that it correctly normalizes optional values and metadata.
/// </summary>
public sealed class AsiBackboneConstraintEvaluationContextTests
{
    /// <summary>
    /// Verifies that the constructor of <see cref="AsiBackboneConstraintEvaluationContext"/> correctly normalizes optional values by trimming whitespace and converting empty strings to null.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesOptionalValues()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            correlationId: " correlation-123 ",
            policyVersion: " v1 ",
            policyHash: " hash-abc ");

        Assert.Equal("correlation-123", context.CorrelationId);
        Assert.Equal("v1", context.PolicyVersion);
        Assert.Equal("hash-abc", context.PolicyHash);
    }


    /// <summary>
    /// Verifies that the constructor of <see cref="AsiBackboneConstraintEvaluationContext"/> converts whitespace-only strings to null for optional values.
    /// </summary>
    [Fact]
    public void ConstructorConvertsWhitespaceValuesToNull()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            correlationId: " ",
            policyVersion: "",
            policyHash: null);

        Assert.Null(context.CorrelationId);
        Assert.Null(context.PolicyVersion);
        Assert.Null(context.PolicyHash);
    }

    /// <summary>
    /// Verifies that the constructor of <see cref="AsiBackboneConstraintEvaluationContext"/> correctly normalizes metadata by trimming whitespace and converting empty strings to null.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesMetadata()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>
            {
                [" region "] = " us-la ",
                [" "] = "ignored",
                ["risk"] = " high "
            });

        Assert.True(context.HasMetadata);
        Assert.Equal(2, context.Metadata.Count);
        Assert.Equal("us-la", context.Metadata["region"]);
        Assert.Equal("high", context.Metadata["risk"]);
    }
}
