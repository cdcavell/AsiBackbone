using AsiBackbone.AspNetCore.Actors;
using AsiBackbone.AspNetCore.Correlation;
using AsiBackbone.AspNetCore.DependencyInjection;
using AsiBackbone.AspNetCore.Endpoints;
using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.AspNetCore.Results;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.Results;
using AsiBackbone.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.DependencyInjection;

public sealed class AsiBackboneAspNetCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAsiBackboneAspNetCoreRegistersDefaultOptions()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore();

        using ServiceProvider provider = services.BuildServiceProvider();
        AsiBackboneAspNetCoreOptions options = provider.GetRequiredService<IOptions<AsiBackboneAspNetCoreOptions>>().Value;

        Assert.True(options.IncludeRouteValues);
        Assert.True(options.IncludeEndpointMetadata);
        Assert.True(options.IncludeRequestMethod);
        Assert.False(options.IncludeRequestPath);
        Assert.True(options.UseHttpContextTraceIdentifierAsCorrelationId);
        Assert.Equal("X-Correlation-ID", options.CorrelationIdHeaderName);
        Assert.Contains("X-Request-ID", options.CorrelationIdHeaderNames);
        Assert.Contains("Traceparent", options.CorrelationIdHeaderNames);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRegistersActorContextServices()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        _ = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        _ = scope.ServiceProvider.GetRequiredService<IAsiBackboneHttpActorContextResolver>();
        AsiBackboneHttpActorContextOptions options = scope.ServiceProvider.GetRequiredService<IOptions<AsiBackboneHttpActorContextOptions>>().Value;

        Assert.Contains("sub", options.ActorIdClaimTypes);
        Assert.Contains("name", options.DisplayNameClaimTypes);
        Assert.Equal("actor_type", options.ActorTypeClaimType);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRegistersRequestCorrelationServices()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        _ = scope.ServiceProvider.GetRequiredService<IAsiBackboneHttpRequestCorrelationResolver>();
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRegistersResultMappingOptions()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore();

        using ServiceProvider provider = services.BuildServiceProvider();
        AsiBackboneHttpResultMappingOptions options = provider.GetRequiredService<IOptions<AsiBackboneHttpResultMappingOptions>>().Value;

        Assert.Equal(StatusCodes.Status200OK, options.SuccessStatusCode);
        Assert.Equal(StatusCodes.Status403Forbidden, options.DeniedStatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, options.DeferredStatusCode);
        Assert.Equal(StatusCodes.Status428PreconditionRequired, options.AcknowledgmentRequiredStatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, options.EscalationRecommendedStatusCode);
        Assert.False(options.IncludeReasonMessages);
        Assert.False(options.IncludeTraceId);
        Assert.False(options.IncludePolicyMetadata);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRegistersAcknowledgmentChallengeServices()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        AsiBackboneAcknowledgmentChallengeOptions options =
            scope.ServiceProvider.GetRequiredService<IOptions<AsiBackboneAcknowledgmentChallengeOptions>>().Value;

        _ = scope.ServiceProvider.GetRequiredService<IAsiBackboneAcknowledgmentChallengeService>();
        Assert.Equal("ACKNOWLEDGE_RESPONSIBILITY", options.RequiredAcknowledgmentCode);
        Assert.True(options.IncludeReasonMessage);
        Assert.False(options.IncludeTraceId);
        Assert.False(options.IncludePolicyMetadata);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreAppliesConfiguredOptions()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore(options =>
        {
            options.IncludeRouteValues = false;
            options.IncludeEndpointMetadata = false;
            options.IncludeRequestMethod = false;
            options.IncludeRequestPath = true;
            options.UseHttpContextTraceIdentifierAsCorrelationId = false;
            options.CorrelationIdHeaderName = "X-Request-ID";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        AsiBackboneAspNetCoreOptions options = provider.GetRequiredService<IOptions<AsiBackboneAspNetCoreOptions>>().Value;

        Assert.False(options.IncludeRouteValues);
        Assert.False(options.IncludeEndpointMetadata);
        Assert.False(options.IncludeRequestMethod);
        Assert.True(options.IncludeRequestPath);
        Assert.False(options.UseHttpContextTraceIdentifierAsCorrelationId);
        Assert.Equal("X-Request-ID", options.CorrelationIdHeaderName);
    }

    [Fact]
    public void AddAsiBackboneStrictGovernanceConfiguresFailClosedOptions()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneStrictGovernance();

        AsiBackbonePolicyEvaluatorOptions evaluatorOptions = ResolveOptions<AsiBackbonePolicyEvaluatorOptions>(services);
        AsiBackboneEndpointGovernanceOptions endpointOptions = ResolveOptions<AsiBackboneEndpointGovernanceOptions>(services);

        Assert.True(evaluatorOptions.DenyWhenNoConstraints);
        Assert.True(evaluatorOptions.TreatConstraintExceptionAsDenial);
        Assert.True(evaluatorOptions.TreatThreatContributorExceptionAsDenial);
        Assert.True(evaluatorOptions.PreventThreatAssessmentAllowDowngrade);
        Assert.True(endpointOptions.FailClosedWhenPolicyEvaluatorMissing);
        Assert.True(endpointOptions.FailClosedWhenCapabilityValidatorMissing);
        Assert.True(endpointOptions.FailClosedWhenAuditSinkMissing);
        Assert.True(endpointOptions.RequireGovernanceMetadata);
    }

    [Fact]
    public void UseStrictGovernanceProfileConfiguresFailClosedOptionsThroughBuilderFacade()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackbone(backbone => backbone.UseStrictGovernanceProfile());

        AsiBackbonePolicyEvaluatorOptions evaluatorOptions = ResolveOptions<AsiBackbonePolicyEvaluatorOptions>(services);
        AsiBackboneEndpointGovernanceOptions endpointOptions = ResolveOptions<AsiBackboneEndpointGovernanceOptions>(services);

        Assert.True(evaluatorOptions.DenyWhenNoConstraints);
        Assert.True(evaluatorOptions.TreatConstraintExceptionAsDenial);
        Assert.True(endpointOptions.RequireGovernanceMetadata);
    }

    [Fact]
    public async Task AddAsiBackboneStrictGovernanceOptionsDenyEmptyPolicyEvaluation()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneStrictGovernance();
        AsiBackbonePolicyEvaluatorOptions options = ResolveOptions<AsiBackbonePolicyEvaluatorOptions>(services);
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>(
            [],
            decisionPolicy: null,
            options);

        GovernanceDecision decision = await evaluator.EvaluateAsync(CreateContext(), TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.False(decision.CanProceed);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultNoConstraintsReasonCode,
            Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task AddAsiBackboneStrictGovernanceOptionsConvertConstraintExceptionToDeniedDecision()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneStrictGovernance();
        AsiBackbonePolicyEvaluatorOptions options = ResolveOptions<AsiBackbonePolicyEvaluatorOptions>(services);
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>(
            [new ThrowingConstraint(new InvalidOperationException("sensitive host failure detail"))],
            decisionPolicy: null,
            options);

        GovernanceDecision decision = await evaluator.EvaluateAsync(CreateContext(), TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.False(decision.CanProceed);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultConstraintExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
        Assert.DoesNotContain("sensitive", Assert.Single(decision.Reasons).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRejectsNullServices()
    {
        IServiceCollection? services = null;

        _ = Assert.Throws<ArgumentNullException>(() => services!.AddAsiBackboneAspNetCore());
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreRejectsNullConfigureCallback()
    {
        ServiceCollection services = new();

        _ = Assert.Throws<ArgumentNullException>(() => services.AddAsiBackboneAspNetCore(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAsiBackboneAspNetCoreRejectsInvalidCorrelationHeaderName(string? headerName)
    {
        ServiceCollection services = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAsiBackboneAspNetCore(options => options.CorrelationIdHeaderName = headerName!));

        Assert.Contains("correlation identifier header name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActorContextOptionsRegistrationRejectsInvalidConfiguredOptions()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneAspNetCore();
        _ = services.Configure<AsiBackboneHttpActorContextOptions>(options => options.ActorIdClaimTypes = []);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            ResolveOptions<AsiBackboneHttpActorContextOptions>(services));

        Assert.Contains("HTTP actor context options", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultMappingOptionsRegistrationRejectsInvalidConfiguredOptions()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneAspNetCore();
        _ = services.Configure<AsiBackboneHttpResultMappingOptions>(options => options.DeniedStatusCode = 99);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            ResolveOptions<AsiBackboneHttpResultMappingOptions>(services));

        Assert.Contains("HTTP result mapping options", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AcknowledgmentChallengeOptionsRegistrationRejectsInvalidConfiguredOptions()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneAspNetCore();
        _ = services.Configure<AsiBackboneAcknowledgmentChallengeOptions>(options => options.RequiredAcknowledgmentText = " ");

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            ResolveOptions<AsiBackboneAcknowledgmentChallengeOptions>(services));

        Assert.Contains("Acknowledgment challenge options", exception.Message, StringComparison.Ordinal);
    }

    private static AsiBackboneConstraintEvaluationContext CreateContext()
    {
        return new AsiBackboneConstraintEvaluationContext(
            correlationId: "strict-governance-correlation",
            policyVersion: "strict-governance-v1",
            policyHash: "strict-governance-hash");
    }

    private static TOptions ResolveOptions<TOptions>(IServiceCollection services)
        where TOptions : class
    {
        using ServiceProvider provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<TOptions>>().Value;
    }

    private sealed class ThrowingConstraint(Exception exception) : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "strict-governance.throwing";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }
}
