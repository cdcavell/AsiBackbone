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
        var context = DlpFailurePolicyContext.Create(
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
        var context = DlpFailurePolicyContext.TimeoutFailure(
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
        Assert.Equal("DLP/classification screening timed out after 2000 ms.", resolution.Reason.Message);
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
        var context = DlpFailurePolicyContext.Create(
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
    public async Task CustomRiskDefaultBehaviorsAreHonoredWhenNoExactOverrideExists()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(
            new DlpFailurePolicyOptions
            {
                LowRiskBehavior = DlpFailureBehavior.Allow,
                MediumRiskBehavior = DlpFailureBehavior.Defer,
                HighRiskBehavior = DlpFailureBehavior.Escalate
            });

        DlpFailurePolicyResolution lowResolution = await resolver.ResolveAsync(
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                DlpIntentRiskLevel.Low),
            TestContext.Current.CancellationToken);
        DlpFailurePolicyResolution mediumResolution = await resolver.ResolveAsync(
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                DlpIntentRiskLevel.Medium),
            TestContext.Current.CancellationToken);
        DlpFailurePolicyResolution highResolution = await resolver.ResolveAsync(
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                DlpIntentRiskLevel.High),
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.Allow, lowResolution.Behavior);
        Assert.True(lowResolution.Decision.IsAllowed);
        Assert.True(lowResolution.Decision.CanProceed);

        Assert.Equal(DlpFailureBehavior.Defer, mediumResolution.Behavior);
        Assert.True(mediumResolution.Decision.IsDeferred);
        Assert.False(mediumResolution.Decision.CanProceed);

        Assert.Equal(DlpFailureBehavior.Escalate, highResolution.Behavior);
        Assert.True(highResolution.Decision.EscalationRecommended);
        Assert.False(highResolution.Decision.CanProceed);
    }

    [Fact]
    public async Task ExactBehaviorOverrideWinsOverRiskDefaultBehavior()
    {
        var options = new DlpFailurePolicyOptions
        {
            MediumRiskBehavior = DlpFailureBehavior.Deny
        };
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.Medium,
            DlpClassificationFailureKind.Timeout)] = DlpFailureBehavior.Allow;

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        var context = DlpFailurePolicyContext.TimeoutFailure(DlpIntentRiskLevel.Medium);

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(DlpFailureBehavior.Allow, resolution.Behavior);
        Assert.True(resolution.Decision.IsAllowed);
        Assert.True(resolution.Decision.CanProceed);
        Assert.Empty(resolution.Decision.ReasonCodes);
        Assert.Equal(DlpFailureReasonCodes.Timeout, resolution.Reason.Code);
    }

    [Fact]
    public async Task OverrideCanEscalateHighRiskBlockedResult()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.High,
            DlpClassificationFailureKind.BlockedResult)] = DlpFailureBehavior.Escalate;

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        var context = DlpFailurePolicyContext.Create(
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
        var context = DlpFailurePolicyContext.Create(
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
    [InlineData(DlpFailureBehavior.Allow)]
    [InlineData(DlpFailureBehavior.WarnAndAllow)]
    [InlineData(DlpFailureBehavior.Deny)]
    [InlineData(DlpFailureBehavior.Defer)]
    [InlineData(DlpFailureBehavior.RequireAcknowledgment)]
    [InlineData(DlpFailureBehavior.Escalate)]
    public void CreateResolutionMapsEveryBehaviorToExpectedGovernanceDecision(DlpFailureBehavior behavior)
    {
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low,
            correlationId: "correlation-250",
            traceId: "trace-250",
            policyVersion: "policy-v250",
            policyHash: "hash-250");

        DlpFailurePolicyResolution resolution = DlpFailurePolicyResolution.Create(context, behavior);

        Assert.Same(context, resolution.Context);
        Assert.Equal(behavior, resolution.Behavior);
        Assert.Equal(DlpFailureReasonCodes.ServiceUnavailable, resolution.Reason.Code);
        Assert.Equal("service_unavailable", resolution.Reason.Metadata["dlp.failure_kind"]);
        Assert.Equal("low", resolution.Reason.Metadata["dlp.risk_level"]);
        Assert.Equal(ToMetadataValue(behavior), resolution.Reason.Metadata["dlp.behavior"]);
        Assert.Equal("correlation-250", resolution.Decision.CorrelationId);
        Assert.Equal("trace-250", resolution.Decision.TraceId);
        Assert.Equal("policy-v250", resolution.Decision.PolicyVersion);
        Assert.Equal("hash-250", resolution.Decision.PolicyHash);

        switch (behavior)
        {
            case DlpFailureBehavior.Allow:
                Assert.True(resolution.Decision.IsAllowed);
                Assert.True(resolution.CanProceed);
                Assert.True(resolution.IsFailOpen);
                Assert.False(resolution.IsFailClosed);
                Assert.False(resolution.Decision.HasReasons);
                Assert.Empty(resolution.Decision.ReasonCodes);
                break;
            case DlpFailureBehavior.WarnAndAllow:
                Assert.True(resolution.Decision.IsWarning);
                Assert.True(resolution.CanProceed);
                Assert.True(resolution.IsFailOpen);
                Assert.False(resolution.IsFailClosed);
                AssertDecisionHasSingleReason(resolution);
                break;
            case DlpFailureBehavior.Deny:
                Assert.True(resolution.Decision.IsDenied);
                Assert.False(resolution.CanProceed);
                Assert.False(resolution.IsFailOpen);
                Assert.True(resolution.IsFailClosed);
                AssertDecisionHasSingleReason(resolution);
                break;
            case DlpFailureBehavior.Defer:
                Assert.True(resolution.Decision.IsDeferred);
                Assert.False(resolution.CanProceed);
                Assert.False(resolution.IsFailOpen);
                Assert.False(resolution.IsFailClosed);
                AssertDecisionHasSingleReason(resolution);
                break;
            case DlpFailureBehavior.RequireAcknowledgment:
                Assert.True(resolution.Decision.RequiresAcknowledgment);
                Assert.False(resolution.CanProceed);
                Assert.False(resolution.IsFailOpen);
                Assert.False(resolution.IsFailClosed);
                AssertDecisionHasSingleReason(resolution);
                break;
            case DlpFailureBehavior.Escalate:
                Assert.True(resolution.Decision.EscalationRecommended);
                Assert.False(resolution.CanProceed);
                Assert.False(resolution.IsFailOpen);
                Assert.False(resolution.IsFailClosed);
                AssertDecisionHasSingleReason(resolution);
                break;
            default:
                throw new InvalidOperationException("Unexpected DLP failure behavior under test.");
        }
    }

    [Theory]
    [InlineData(DlpClassificationFailureKind.ServiceUnavailable, DlpFailureReasonCodes.ServiceUnavailable, "DLP/classification screening service was unavailable.")]
    [InlineData(DlpClassificationFailureKind.IndeterminateResult, DlpFailureReasonCodes.IndeterminateResult, "DLP/classification screening returned an indeterminate result.")]
    [InlineData(DlpClassificationFailureKind.BlockedResult, DlpFailureReasonCodes.BlockedResult, "DLP/classification screening returned a blocked result.")]
    [InlineData(DlpClassificationFailureKind.ClassifiedResult, DlpFailureReasonCodes.ClassifiedResult, "DLP/classification screening returned a classified result that requires configured policy handling.")]
    public void FailureKindMessagesRemainStable(
        DlpClassificationFailureKind failureKind,
        string expectedReasonCode,
        string expectedMessage)
    {
        var context = DlpFailurePolicyContext.Create(
            failureKind,
            DlpIntentRiskLevel.Low);

        DlpFailurePolicyResolution resolution = DlpFailurePolicyResolution.Create(
            context,
            DlpFailureBehavior.WarnAndAllow);

        Assert.Equal(expectedReasonCode, resolution.Reason.Code);
        Assert.Equal(expectedMessage, resolution.Reason.Message);
        Assert.Contains(expectedReasonCode, resolution.Decision.ReasonCodes);
    }

    [Fact]
    public void TimeoutWithoutTimeoutValueUsesGenericMessageAndOmitsTimeoutMetadata()
    {
        DlpFailurePolicyResolution resolution = DlpFailurePolicyResolution.Create(
            DlpFailurePolicyContext.TimeoutFailure(DlpIntentRiskLevel.Low),
            DlpFailureBehavior.WarnAndAllow);

        Assert.Equal(DlpFailureReasonCodes.Timeout, resolution.Reason.Code);
        Assert.Equal("DLP/classification screening timed out.", resolution.Reason.Message);
        Assert.False(resolution.Reason.Metadata.ContainsKey("dlp.timeout_ms"));
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
        var context = DlpFailurePolicyContext.Create(
            failureKind,
            DlpIntentRiskLevel.Low);

        DlpFailurePolicyResolution resolution = await resolver.ResolveAsync(
            context,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedReasonCode, resolution.Reason.Code);
        Assert.Contains(expectedReasonCode, resolution.Decision.ReasonCodes);
    }

    [Fact]
    public void InvalidRiskLevelThrowsArgumentOutOfRangeException()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                (DlpIntentRiskLevel)999));
    }

    [Fact]
    public void InvalidFailureKindThrowsArgumentOutOfRangeException()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailurePolicyContext.Create(
                (DlpClassificationFailureKind)999,
                DlpIntentRiskLevel.Low));

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailureReasonCodes.GetFor((DlpClassificationFailureKind)999));
    }

    [Theory]
    [InlineData(DlpIntentRiskLevel.Low)]
    [InlineData(DlpIntentRiskLevel.Medium)]
    [InlineData(DlpIntentRiskLevel.High)]
    public async Task InvalidDefaultBehaviorThrowsArgumentOutOfRangeException(DlpIntentRiskLevel riskLevel)
    {
        var options = new DlpFailurePolicyOptions();

        switch (riskLevel)
        {
            case DlpIntentRiskLevel.Low:
                options.LowRiskBehavior = (DlpFailureBehavior)999;
                break;
            case DlpIntentRiskLevel.Medium:
                options.MediumRiskBehavior = (DlpFailureBehavior)999;
                break;
            case DlpIntentRiskLevel.High:
                options.HighRiskBehavior = (DlpFailureBehavior)999;
                break;
            default:
                throw new InvalidOperationException("Unexpected DLP risk level under test.");
        }

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            riskLevel);

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await resolver.ResolveAsync(context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidOverrideBehaviorThrowsArgumentOutOfRangeException()
    {
        var options = new DlpFailurePolicyOptions();
        options.BehaviorOverrides[new DlpFailurePolicyKey(
            DlpIntentRiskLevel.Low,
            DlpClassificationFailureKind.ServiceUnavailable)] = (DlpFailureBehavior)999;

        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver(options);
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low);

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await resolver.ResolveAsync(context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void InvalidResolutionBehaviorThrowsArgumentOutOfRangeException()
    {
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DlpFailurePolicyResolution.Create(context, (DlpFailureBehavior)999));
    }

    [Fact]
    public async Task NullContextThrowsArgumentNullException()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await resolver.ResolveAsync(null!, TestContext.Current.CancellationToken));

        _ = Assert.Throws<ArgumentNullException>(() =>
            DlpFailurePolicyResolution.Create(null!, DlpFailureBehavior.Allow));

        var options = new DlpFailurePolicyOptions();
        _ = Assert.Throws<ArgumentNullException>(() => options.GetBehavior(null!));
    }

    [Fact]
    public async Task ResolveHonorsCancellationBeforeResolution()
    {
        var resolver = new DefaultAsiBackboneDlpFailurePolicyResolver();
        var context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await resolver.ResolveAsync(context, cancellationTokenSource.Token));
    }

    [Fact]
    public void NullAndEmptyMetadataRemainSafe()
    {
        DlpFailurePolicyContext nullMetadataContext = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low,
            metadata: null);
        DlpFailurePolicyContext emptyMetadataContext = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low,
            metadata: new Dictionary<string, string>());

        DlpFailurePolicyResolution nullMetadataResolution = DlpFailurePolicyResolution.Create(
            nullMetadataContext,
            DlpFailureBehavior.WarnAndAllow);
        DlpFailurePolicyResolution emptyMetadataResolution = DlpFailurePolicyResolution.Create(
            emptyMetadataContext,
            DlpFailureBehavior.WarnAndAllow);

        Assert.False(nullMetadataContext.HasMetadata);
        Assert.False(emptyMetadataContext.HasMetadata);
        Assert.False(nullMetadataResolution.Reason.Metadata.ContainsKey(string.Empty));
        Assert.False(emptyMetadataResolution.Reason.Metadata.ContainsKey(string.Empty));
        Assert.Equal("service_unavailable", nullMetadataResolution.Reason.Metadata["dlp.failure_kind"]);
        Assert.Equal("service_unavailable", emptyMetadataResolution.Reason.Metadata["dlp.failure_kind"]);
    }

    [Fact]
    public void MetadataNormalizationIgnoresBlankKeysAndTrimsKeysAndValues()
    {
        DlpFailurePolicyContext context = DlpFailurePolicyContext.Create(
            DlpClassificationFailureKind.ServiceUnavailable,
            DlpIntentRiskLevel.Low,
            intentCategory: " ",
            environment: " ",
            metadata: new Dictionary<string, string>
            {
                [" "] = "ignored",
                [" host.key "] = " value ",
                ["empty-value"] = null!
            });

        DlpFailurePolicyResolution resolution = DlpFailurePolicyResolution.Create(
            context,
            DlpFailureBehavior.WarnAndAllow);

        Assert.True(context.HasMetadata);
        Assert.Equal("value", context.Metadata["host.key"]);
        Assert.Equal(string.Empty, context.Metadata["empty-value"]);
        Assert.False(context.Metadata.ContainsKey(string.Empty));
        Assert.Equal("value", resolution.Reason.Metadata["host.key"]);
        Assert.Equal(string.Empty, resolution.Reason.Metadata["empty-value"]);
        Assert.False(resolution.Reason.Metadata.ContainsKey(string.Empty));
        Assert.False(resolution.Reason.Metadata.ContainsKey("dlp.intent_category"));
        Assert.False(resolution.Reason.Metadata.ContainsKey("dlp.environment"));
    }

    [Fact]
    public void IntentCategoryAndEnvironmentAreIncludedOnlyWhenNonblank()
    {
        DlpFailurePolicyResolution blankResolution = DlpFailurePolicyResolution.Create(
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                DlpIntentRiskLevel.Low,
                intentCategory: " ",
                environment: " "),
            DlpFailureBehavior.WarnAndAllow);
        DlpFailurePolicyResolution nonblankResolution = DlpFailurePolicyResolution.Create(
            DlpFailurePolicyContext.Create(
                DlpClassificationFailureKind.ServiceUnavailable,
                DlpIntentRiskLevel.Low,
                intentCategory: " external-api ",
                environment: " Production "),
            DlpFailureBehavior.WarnAndAllow);

        Assert.False(blankResolution.Reason.Metadata.ContainsKey("dlp.intent_category"));
        Assert.False(blankResolution.Reason.Metadata.ContainsKey("dlp.environment"));
        Assert.Equal("external-api", nonblankResolution.Reason.Metadata["dlp.intent_category"]);
        Assert.Equal("Production", nonblankResolution.Reason.Metadata["dlp.environment"]);
    }

    private static void AssertDecisionHasSingleReason(DlpFailurePolicyResolution resolution)
    {
        Assert.True(resolution.Decision.HasReasons);
        Assert.Equal(resolution.Reason.Code, Assert.Single(resolution.Decision.ReasonCodes));
        Assert.Same(resolution.Decision.Reasons[0], Assert.Single(resolution.Decision.Reasons));
        Assert.Equal(resolution.Reason.Code, resolution.Decision.Reasons[0].Code);
        Assert.Equal(resolution.Reason.Message, resolution.Decision.Reasons[0].Message);
    }

    private static string ToMetadataValue(DlpFailureBehavior behavior)
    {
        string text = behavior.ToString();

        return string.Concat(
            text.Select((character, index) =>
                index > 0 && char.IsUpper(character)
                    ? "_" + char.ToLowerInvariant(character)
                    : char.ToLowerInvariant(character).ToString()));
    }
}
