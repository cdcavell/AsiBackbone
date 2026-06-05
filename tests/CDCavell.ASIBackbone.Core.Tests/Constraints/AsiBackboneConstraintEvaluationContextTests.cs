using CDCavell.AsiBackbone.Core.Constraints;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Constraints;

/// <summary>
/// Unit tests for <see cref="AsiBackboneConstraintEvaluationContext"/> to verify that it correctly normalizes optional values and metadata.
/// </summary>
public sealed class AsiBackboneConstraintEvaluationContextTests
{
    /// <summary>
    /// Verifies that the default constructor creates an empty context with no optional values or metadata.
    /// </summary>
    [Fact]
    public void ConstructorCreatesEmptyContextByDefault()
    {
        var context = new AsiBackboneConstraintEvaluationContext();

        Assert.Null(context.CorrelationId);
        Assert.Null(context.PolicyVersion);
        Assert.Null(context.PolicyHash);
        Assert.False(context.HasMetadata);
        Assert.Empty(context.Metadata);
    }

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
    /// Verifies that null metadata is normalized to an empty metadata collection.
    /// </summary>
    [Fact]
    public void ConstructorWithNullMetadataReturnsNoMetadata()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: null);

        Assert.False(context.HasMetadata);
        Assert.Empty(context.Metadata);
    }

    /// <summary>
    /// Verifies that empty metadata is normalized to an empty metadata collection.
    /// </summary>
    [Fact]
    public void ConstructorWithEmptyMetadataReturnsNoMetadata()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>());

        Assert.False(context.HasMetadata);
        Assert.Empty(context.Metadata);
    }

    /// <summary>
    /// Verifies that the constructor of <see cref="AsiBackboneConstraintEvaluationContext"/> correctly normalizes metadata by trimming whitespace and ignoring blank keys.
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

    /// <summary>
    /// Verifies that metadata containing only blank keys is normalized to an empty metadata collection.
    /// </summary>
    [Fact]
    public void ConstructorWithOnlyBlankMetadataKeysReturnsNoMetadata()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>
            {
                [" "] = "ignored",
                ["\t"] = "also ignored"
            });

        Assert.False(context.HasMetadata);
        Assert.Empty(context.Metadata);
    }

    /// <summary>
    /// Verifies that duplicate keys after trimming use the later normalized value.
    /// </summary>
    [Fact]
    public void ConstructorWithDuplicateTrimmedMetadataKeysUsesLastValue()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>
            {
                [" region "] = " us-la ",
                ["region"] = " us-tx "
            });

        Assert.True(context.HasMetadata);
        _ = Assert.Single(context.Metadata);
        Assert.Equal("us-tx", context.Metadata["region"]);
    }

    /// <summary>
    /// Verifies that null metadata values are normalized to empty strings while preserving valid keys.
    /// </summary>
    [Fact]
    public void ConstructorWithNullMetadataValueStoresEmptyString()
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>
            {
                [" source "] = null!
            });

        Assert.True(context.HasMetadata);
        Assert.Equal(string.Empty, context.Metadata["source"]);
    }
}
