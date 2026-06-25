using AsiBackbone.Core.Classification;
using Xunit;

namespace AsiBackbone.Core.Tests.Classification;

public sealed class DlpFailurePolicyOptionsBranchTests
{
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

    [Fact]
    public void GetBehaviorRejectsNullContext()
    {
        var options = new DlpFailurePolicyOptions();

        _ = Assert.Throws<ArgumentNullException>(() => options.GetBehavior(null!));
    }

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

    [Fact]
    public void ContextRejectsNegativeTimeout()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailurePolicyContext.TimeoutFailure(
                DlpIntentRiskLevel.Low,
                TimeSpan.FromTicks(-1)));
    }
}
