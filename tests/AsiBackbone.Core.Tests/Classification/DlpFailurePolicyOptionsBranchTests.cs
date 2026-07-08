using AsiBackbone.Core.Classification;
using Xunit;

namespace AsiBackbone.Core.Tests.Classification;

/// <summary>
/// Tests for the DlpFailurePolicyOptions and DlpFailurePolicyContext classes, which define the behavior of the DLP failure policy based on risk levels and failure kinds.
/// </summary>
public sealed class DlpFailurePolicyOptionsBranchTests
{
    /// <summary>
    /// Tests that the GetBehavior method returns the expected default behavior for each risk level when no specific overrides are configured.
    /// </summary>
    /// <param name="riskLevel">
    /// The risk level of the DLP intent, which can be Low, Medium, or High. This parameter is used to determine the default behavior for the given risk level.
    /// </param>
    /// <param name="expectedBehavior">
    /// The expected DlpFailureBehavior that should be returned by the GetBehavior method for the given risk level. This parameter is used to verify that the method returns the correct default behavior based on the risk level.
    /// </param>
    [Theory]
    [InlineData(DlpIntentRiskLevel.Low, DlpFailureBehavior.WarnAndAllow)]
    [InlineData(DlpIntentRiskLevel.Medium, DlpFailureBehavior.RequireAcknowledgment)]
    [InlineData(DlpIntentRiskLevel.High, DlpFailureBehavior.Deny)]
    public void GetBehaviorReturnsConfiguredRiskDefault(
        DlpIntentRiskLevel riskLevel,
        DlpFailureBehavior expectedBehavior)
    {
        var options = new DlpFailurePolicyOptions();
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            riskLevel);

        DlpFailureBehavior behavior = options.GetBehavior(context);

        Assert.Equal(expectedBehavior, behavior);
    }

    /// <summary>
    /// Tests that the GetBehavior method returns the expected behavior when a specific override is configured for a given risk level and failure kind. This test verifies that the method correctly applies the override instead of the default behavior.
    /// </summary>
    [Fact]
    public void GetBehaviorPrefersFailureSpecificOverride()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.High,
            DlpClassificationFailureKind.Timeout)] = DlpFailureBehavior.Escalate;
        var context = DlpFailurePolicyContext.TimeoutFailure(
            DlpIntentRiskLevel.High,
            TimeSpan.FromSeconds(3));

        DlpFailureBehavior behavior = options.GetBehavior(context);

        Assert.Equal(DlpFailureBehavior.Escalate, behavior);
    }

    /// <summary>
    /// Tests that the GetBehavior method throws an ArgumentOutOfRangeException when an undefined override behavior is configured for a specific risk level and failure kind. This test verifies that the method correctly rejects invalid behavior values and raises an exception.
    /// </summary>
    [Fact]
    public void GetBehaviorRejectsUndefinedOverrideBehavior()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.Low,
            DlpClassificationFailureKind.Timeout)] = (DlpFailureBehavior)999;
        var context = DlpFailurePolicyContext.TimeoutFailure(DlpIntentRiskLevel.Low);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() => options.GetBehavior(context));
    }

    /// <summary>
    /// Tests that the GetBehavior method throws an ArgumentOutOfRangeException when an undefined default behavior is configured for a specific risk level. This test verifies that the method correctly rejects invalid default behavior values and raises an exception.
    /// </summary>
    [Fact]
    public void GetBehaviorRejectsUndefinedRiskDefaultBehavior()
    {
        var options = new DlpFailurePolicyOptions
        {
            MediumRiskBehavior = (DlpFailureBehavior)999
        };
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.IndeterminateResult,
            DlpIntentRiskLevel.Medium);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() => options.GetBehavior(context));
    }

    /// <summary>
    /// Tests that the GetBehavior method throws an ArgumentNullException when a null context is passed. This test verifies that the method correctly handles null input and raises an exception.
    /// </summary>
    [Fact]
    public void GetBehaviorRejectsNullContext()
    {
        var options = new DlpFailurePolicyOptions();

        _ = Assert.Throws<ArgumentNullException>(() => options.GetBehavior(null!));
    }

    /// <summary>
    /// Tests that the DlpFailurePolicyContext correctly normalizes optional string fields and metadata dictionary entries by trimming whitespace and handling null values. This test verifies that the context creation process produces consistent and expected results for optional fields and metadata.
    /// </summary>
    [Fact]
    public void ContextNormalizesOptionalFieldsAndMetadata()
    {
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.BlockedResult,
            DlpIntentRiskLevel.High,
            intentCategory: " regulated-export ",
            environment: " production ",
            correlationId: " corr-1 ",
            traceId: " trace-1 ",
            policyVersion: " v1 ",
            policyHash: " hash-1 ",
            timeout: TimeSpan.Zero,
            metadata: new Dictionary<string, string>
            {
                [" source "] = " policy-test ",
                [" "] = " ignored ",
                ["nullable"] = null!
            });

        Assert.Equal("regulated-export", context.IntentCategory);
        Assert.Equal("production", context.Environment);
        Assert.Equal("corr-1", context.CorrelationId);
        Assert.Equal("trace-1", context.TraceId);
        Assert.Equal("v1", context.PolicyVersion);
        Assert.Equal("hash-1", context.PolicyHash);
        Assert.True(context.HasTimeout);
        Assert.True(context.HasMetadata);
        Assert.Equal("policy-test", context.Metadata["source"]);
        Assert.Equal(string.Empty, context.Metadata["nullable"]);
        Assert.False(context.Metadata.ContainsKey(string.Empty));
    }

    /// <summary>
    /// Tests that the DlpFailurePolicyContext correctly handles blank string inputs for optional fields and metadata by treating them as null or empty. This test verifies that the context creation process produces consistent and expected results when provided with blank inputs.
    /// </summary>
    [Fact]
    public void ContextUsesEmptyMetadataAndNullOptionalFieldsForBlankInputs()
    {
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ClassifiedResult,
            DlpIntentRiskLevel.Low,
            intentCategory: " ",
            environment: " ",
            correlationId: " ",
            traceId: " ",
            policyVersion: " ",
            policyHash: " ",
            metadata: new Dictionary<string, string>
            {
                [" "] = " ignored "
            });

        Assert.Null(context.IntentCategory);
        Assert.Null(context.Environment);
        Assert.Null(context.CorrelationId);
        Assert.Null(context.TraceId);
        Assert.Null(context.PolicyVersion);
        Assert.Null(context.PolicyHash);
        Assert.False(context.HasTimeout);
        Assert.False(context.HasMetadata);
        Assert.Empty(context.Metadata);
    }

    /// <summary>
    /// Tests that the DlpFailurePolicyContext correctly rejects negative timeout values. This test verifies that the context creation process throws an exception when a negative timeout is provided.
    /// </summary>
    [Fact]
    public void ContextRejectsNegativeTimeout()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailurePolicyContext.TimeoutFailure(
                DlpIntentRiskLevel.Low,
                TimeSpan.FromTicks(-1)));
    }
}
