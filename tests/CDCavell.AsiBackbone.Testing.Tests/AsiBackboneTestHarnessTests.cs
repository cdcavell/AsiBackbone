using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.AspNetCore.Endpoints;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Evaluation;
using CDCavell.AsiBackbone.Core.Signing;
using CDCavell.AsiBackbone.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.Testing.Tests;

public sealed class AsiBackboneTestHarnessTests
{
    [Fact]
    public async Task AddAsiBackboneTestHarnessAllowsProtectedEndpointWithoutHostPersistenceOrSigningSetup()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddAsiBackboneTestHarness(harness =>
            {
                harness.AllowAllPolicies();
                harness.AllowCapabilityGrants();
            })
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = services.CreateScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider, "trace-protected");
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new RequireCapabilityGrantAttribute("robotics.execute"),
                new EmitGovernanceAuditAttribute()),
            "testing.protected");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(
            httpContext,
            descriptor,
            TestContext.Current.CancellationToken);

        Assert.True(result.CanExecute);
        Assert.NotNull(result.Decision);
        Assert.True(result.Decision.IsAllowed);

        AsiBackboneTestAuditSink auditSink = scope.ServiceProvider.GetRequiredService<AsiBackboneTestAuditSink>();
        Assert.Single(auditSink.Entries);
        Assert.Equal("testing.protected", auditSink.Entries[0].OperationName);
    }

    [Fact]
    public async Task SetPolicyResultUsesDeterministicDecisionByPolicyMarkerType()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddAsiBackboneTestHarness(harness =>
            {
                harness.AllowAllPolicies();
                harness.SetPolicyResult<StrictPolicy>(GovernanceDecision.Deny(
                    "test.denied",
                    "Denied by test harness."));
            })
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = services.CreateScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider, "trace-denied");
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireGovernancePolicyAttribute(typeof(StrictPolicy))),
            "testing.denied");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(
            httpContext,
            descriptor,
            TestContext.Current.CancellationToken);

        Assert.False(result.CanExecute);
        Assert.NotNull(result.Decision);
        Assert.True(result.Decision.IsDenied);
        Assert.Contains("test.denied", result.Decision.ReasonCodes);
    }

    [Fact]
    public async Task DenyAllPoliciesAppliesDefaultDenyDecision()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddAsiBackboneTestHarness(harness =>
            {
                harness.DenyAllPolicies("test.default_denied", "Default denied by test harness.");
            })
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = services.CreateScope();
        IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator =
            scope.ServiceProvider.GetRequiredService<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>();
        var context = new AsiBackboneConstraintEvaluationContext(
            correlationId: "correlation-1",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["endpoint.policy_types"] = typeof(SamplePolicy).FullName ?? typeof(SamplePolicy).Name
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal("correlation-1", decision.CorrelationId);
        Assert.Contains("test.default_denied", decision.ReasonCodes);
    }

    [Fact]
    public async Task RequirePolicyResultFailsClosedForUnconfiguredPolicyMarker()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddAsiBackboneTestHarness(harness =>
            {
                harness.RequirePolicyResult<StrictPolicy>(GovernanceDecision.Allow());
            })
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = services.CreateScope();
        IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator =
            scope.ServiceProvider.GetRequiredService<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>();
        var context = new AsiBackboneConstraintEvaluationContext(
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["endpoint.policy_types"] = typeof(SamplePolicy).FullName ?? typeof(SamplePolicy).Name
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("test_harness.policy_result.missing", decision.ReasonCodes);
    }

    [Fact]
    public async Task DenyCapabilityGrantsBlocksEndpointAfterAllowedPolicyDecision()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddAsiBackboneTestHarness(harness =>
            {
                harness.AllowAllPolicies();
                harness.DenyCapabilityGrants("test.capability_denied", "Capability denied by test harness.");
            })
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = services.CreateScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider, "trace-capability");
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new RequireCapabilityGrantAttribute("robotics.execute")),
            "testing.capability");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(
            httpContext,
            descriptor,
            TestContext.Current.CancellationToken);

        Assert.False(result.CanExecute);
        Assert.NotNull(result.Decision);
        Assert.True(result.Decision.IsDenied);
        Assert.Contains("test.capability_denied", result.Decision.ReasonCodes);
    }

    [Fact]
    public async Task DeterministicSigningServiceReturnsNoSignatureResult()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneTestHarness()
            .BuildServiceProvider(validateScopes: true);

        IAsiBackboneSigningService signingService = services.GetRequiredService<IAsiBackboneSigningService>();

        SigningResult result = await signingService.SignAsync(
            new SigningRequest("test-payload-hash", "SHA256", "test"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSigned);
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider serviceProvider, string traceIdentifier)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            TraceIdentifier = traceIdentifier
        };

        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        return httpContext;
    }

    private sealed class SamplePolicy
    {
    }

    private sealed class StrictPolicy
    {
    }
}
