using AsiBackbone.AspNetCore.DependencyInjection;
using AsiBackbone.AspNetCore.Endpoints;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Endpoints;

public sealed class AsiBackboneEndpointGovernanceMetadataModeTests
{
    [Fact]
    public void DescriptorToMetadataDefaultsToFullMetadata()
    {
        var descriptor = CreateDescriptor();

        IReadOnlyDictionary<string, string> metadata = descriptor.ToMetadata();

        Assert.Equal("sample.metadata", metadata["endpoint.operation_name"]);
        Assert.Equal("true", metadata["endpoint.requires_liability_handshake"]);
        Assert.Equal("true", metadata["endpoint.emit_governance_audit"]);
        Assert.Contains(typeof(SamplePolicy).FullName!, metadata["endpoint.policy_types"], StringComparison.Ordinal);
        Assert.Equal("robotics.execute", metadata["endpoint.capability_scopes"]);
    }

    [Fact]
    public void DescriptorToMetadataCanUseReducedMode()
    {
        var descriptor = CreateDescriptor();

        IReadOnlyDictionary<string, string> metadata = descriptor.ToMetadata(AsiBackboneEndpointGovernanceMetadataMode.Reduced);

        KeyValuePair<string, string> item = Assert.Single(metadata);
        Assert.Equal("endpoint.operation_name", item.Key);
        Assert.Equal("sample.metadata", item.Value);
        Assert.False(metadata.ContainsKey("endpoint.requires_liability_handshake"));
        Assert.False(metadata.ContainsKey("endpoint.emit_governance_audit"));
        Assert.False(metadata.ContainsKey("endpoint.policy_types"));
        Assert.False(metadata.ContainsKey("endpoint.capability_scopes"));
    }

    [Fact]
    public async Task DefaultServicePassesReducedMetadataToPolicyEvaluator()
    {
        var evaluator = new CapturingPolicyEvaluator();
        using ServiceProvider services = new ServiceCollection()
            .Configure<AsiBackboneEndpointGovernanceOptions>(options =>
            {
                options.MetadataMode = AsiBackboneEndpointGovernanceMetadataMode.Reduced;
            })
            .AddAsiBackboneAspNetCore()
            .AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>(evaluator)
            .BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "trace-reduced-metadata"
        };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new RequireLiabilityHandshakeAttribute()),
            "sample.metadata.reduced");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(httpContext, descriptor, TestContext.Current.CancellationToken);

        Assert.True(result.CanExecute);
        Assert.NotNull(evaluator.CapturedMetadata);
        KeyValuePair<string, string> item = Assert.Single(evaluator.CapturedMetadata);
        Assert.Equal("endpoint.operation_name", item.Key);
        Assert.Equal("sample.metadata.reduced", item.Value);
    }

    [Fact]
    public async Task DefaultServiceDevelopmentDiagnosticsHonorsReducedMetadataMode()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment("Development"))
            .Configure<AsiBackboneEndpointGovernanceOptions>(options =>
            {
                options.EnableDevelopmentDiagnostics = true;
                options.MetadataMode = AsiBackboneEndpointGovernanceMetadataMode.Reduced;
            })
            .AddAsiBackboneAspNetCore()
            .BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "trace-reduced-diagnostics"
        };
        httpContext.Response.Body = new MemoryStream();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "sample.metadata.diagnostics");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(httpContext, descriptor, TestContext.Current.CancellationToken);
        Assert.NotNull(result.FailureResult);
        await result.FailureResult.ExecuteAsync(httpContext);

        string body = await ReadResponseBodyAsync(httpContext);
        Assert.Contains("Reduced", body, StringComparison.Ordinal);
        Assert.Contains("endpoint.operation_name", body, StringComparison.Ordinal);
        Assert.DoesNotContain("endpoint.capability_scopes", body, StringComparison.Ordinal);
        Assert.DoesNotContain("endpoint.emit_governance_audit", body, StringComparison.Ordinal);
    }

    private static AsiBackboneEndpointGovernanceDescriptor CreateDescriptor()
    {
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new RequireLiabilityHandshakeAttribute(),
                new RequireCapabilityGrantAttribute("robotics.execute"),
                new EmitGovernanceAuditAttribute()),
            "sample.metadata");

        return AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, leaveOpen: true);

        return await reader.ReadToEndAsync();
    }

    private sealed class SamplePolicy
    {
    }

    private sealed class CapturingPolicyEvaluator : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public IReadOnlyDictionary<string, string>? CapturedMetadata { get; private set; }

        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapturedMetadata = context.Metadata;

            return ValueTask.FromResult(GovernanceDecision.Allow(
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash));
        }
    }

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "AsiBackbone.AspNetCore.Tests";

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
