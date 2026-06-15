using CDCavell.AsiBackbone.Core.Classification;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Classification;

/// <summary>
/// Unit tests for provider-neutral DLP and classification failure policy resolution.
/// </summary>
public sealed class DefaultAsiBackboneDlpFailurePolicyResolverTests
{
    [Fact]
    public async Task ResolveLowRiskServiceUnavailableWarnsAndAllowsByDefault()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low,
            intentCategory: "metadata-only-emission",
            environment: "Development");

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.WarnAndAllow, resolution.Behavior);
        Assert.True(resolution.IsFailOpen);
        Assert.False(resolution.IsFailClosed);
        Assert.True(resolution.Decision.IsWarning);
        Assert.True(resolution.Decision.CanProceed);
        Assert.Contains(DlpFailureReasonCodes.ServiceUnavailable, resolution.Decision.ReasonCodes);
        Assert.Equal("service_unavailable", resolution.Reason.Metadata["dlp.failure_kind"]);
        Assert.Equal("low", resolution.Reason.Metadata["dlp.risk_level"]);
        Assert.Equal("warn_and_allow", resolution.Reason.Metadata["dlp.behavior"]);
        Assert.Equal("metadata-only-emission", resolution.Reason.Metadata["dlp.intent_category"]);
        Assert.Equal("Development", resolution.Reason.Metadata["dlp.environment"]);
    }

    [Fact]
    public async Task ResolveMediumRiskTimeoutRequiresAcknowledgmentByDefault()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();
        DlpFailurePolicyContext context = DlpFailurePolicyContext.TimeoutFailure(
            DlpIntentRiskLevel.Medium,
            timeout: TimeSpan.FromSeconds(2),
            correlationId: "corr-dlp-timeout",
            traceId: "trace-dlp-timeout",
            policyVersion: "policy-v1",
            policyHash: "hash-v1");

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.RequireAcknowledgment, resolution.Behavior);
        Assert.True(resolution.Decision.RequiresAcknowledgment);
        Assert.False(resolution.Decision.CanProceed);
        Assert.Contains(DlpFailureReasonCodes.Timeout, resolution.Decision.ReasonCodes);
        Assert.Contains("2000", resolution.Reason.Message, StringComparison.Ordinal);
        Assert.Equal("2000", resolution.Reason.Metadata["dlp.timeout_ms"]);
        Assert.Equal("corr-dlp-timeout", resolution.Decision.CorrelationId);
        Assert.Equal("trace-dlp-timeout", resolution.Decision.TraceId);
        Assert.Equal("policy-v1", resolution.Decision.PolicyVersion);
        Assert.Equal("hash-v1", resolution.Decision.PolicyHash);
    }

    [Fact]
    public async Task ResolveHighRiskIndeterminateResultFailsClosedByDefault()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.IndeterminateResult,
            DlpIntentRiskLevel.High);

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.Deny, resolution.Behavior);
        Assert.True(resolution.IsFailClosed);
        Assert.False(resolution.IsFailOpen);
        Assert.True(resolution.Decision.IsDenied);
        Assert.False(resolution.Decision.CanProceed);
        Assert.Contains(DlpFailureReasonCodes.IndeterminateResult, resolution.Decision.ReasonCodes);
    }

    [Fact]
    public async Task OverrideCanEscalateHighRiskBlockedResult()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.High,
            DlpClassificationFailureKind.BlockedResult)] = DlpFailureBehavior.Escalate;

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.BlockedResult,
            DlpIntentRiskLevel.High);

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.Escalate, resolution.Behavior);
        Assert.True(resolution.Decision.EscalationRecommended);
        Assert.Contains(DlpFailureReasonCodes.BlockedResult, resolution.Decision.ReasonCodes);
    }

    [Fact]
    public async Task OverrideCanDeferLowRiskClassifiedResult()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.Low,
            DlpClassificationFailureKind.ClassifiedResult)] = DlpFailureBehavior.Defer;

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ClassifiedResult,
            DlpIntentRiskLevel.Low,
            metadata: new Dictionary<string, string>
            {
                [" classification.source "] = " unit-test "
            });

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.Defer, resolution.Behavior);
        Assert.True(resolution.Decision.IsDeferred);
        Assert.Contains(DlpFailureReasonCodes.ClassifiedResult, resolution.Decision.ReasonCodes);
        Assert.Equal("unit-test", resolution.Reason.Metadata["classification.source"]);
    }

    [Theory]
    [InlineData(DlpClassificationFailureKind.ServiceUnavailable, DlpFailureReasonCodes.ServiceUnavailable)]
    [InlineData(DlpClassificationFailureKind.Timeout, DlpFailureReasonCodes.Timeout)]
    [InlineData(DlpClassificationFailureKind.IndeterminateResult, DlpFailureReasonCodes.IndeterminateResult)]
    [InlineData(DlpClassificationFailureKind.BlockedResult, DlpFailureReasonCodes.BlockedResult)]
    [InlineData(DlpClassificationFailureKind.ClassifiedResult, DlpFailureReasonCodes.ClassifiedResult)]
    public async Task ReasonCodesDistinguishApiFailuresFromClassificationFailures(
        DlpClassificationFailureKind failureKind,
        string expectedReasonCode)
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(
            new DlpFailurePolicyOptions
            {
                LowRiskBehavior = DlpFailureBehavior.WarnAndAllow
            });
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            failureKind,
            DlpIntentRiskLevel.Low);

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedReasonCode, resolution.Reason.Code);
        Assert.Contains(expectedReasonCode, resolution.Decision.ReasonCodes);
    }

    [Fact]
    public async Task ResolveHonorsCancellationBeforeResolution()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await resolver.ResolveAsync(context, cancellationTokenSource.Token));
    }
}
