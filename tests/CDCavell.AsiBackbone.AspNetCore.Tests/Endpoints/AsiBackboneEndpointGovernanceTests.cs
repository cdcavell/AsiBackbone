using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Endpoints;

public sealed class AsiBackboneEndpointGovernanceTests
{
    [Fact]
    public void DescriptorReadsAttributeMetadataFromEndpoint()
    {
        var endpoint = new Endpoint(
            static context => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new RequireLiabilityHandshakeAttribute(),
                new RequireCapabilityGrantAttribute("robotics.execute"),
                new EmitGovernanceAuditAttribute()),
            "sample.robotics.execute");

        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);

        Assert.True(descriptor.HasGovernanceMetadata);
        Assert.Equal("sample.robotics.execute", descriptor.OperationName);
        Assert.Contains(typeof(SamplePolicy), descriptor.PolicyTypes);
        Assert.True(descriptor.RequiresLiabilityHandshake);
        Assert.Contains("robotics.execute", descriptor.CapabilityScopes);
        Assert.True(descriptor.EmitGovernanceAudit);
        Assert.Equal("sample.robotics.execute", descriptor.ToMetadata()["endpoint.operation_name"]);
    }

    [Fact]
    public async Task MiddlewareSkipsGovernanceWhenEndpointHasNoGovernanceMetadata()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(static _ => Task.CompletedTask, new EndpointMetadataCollection(), "plain"));
        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext, new ThrowingEndpointGovernanceService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task MiddlewareBlocksWhenGovernanceServiceBlocksExecution()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "blocked"));
        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext, new BlockingEndpointGovernanceService());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultServiceFailsClosedWhenCapabilityValidatorIsMissing()
    {
        ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .BuildServiceProvider(validateScopes: true);
        using ServiceScope scope = services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "trace-123"
        };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "robotics.execute");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(httpContext, descriptor, TestContext.Current.CancellationToken);

        Assert.False(result.CanExecute);
        Assert.NotNull(result.FailureResult);
        Assert.NotNull(result.Decision);
        Assert.Contains("endpoint.capability_validator.missing", result.Decision.ReasonCodes);
    }

    private sealed class SamplePolicy;

    private sealed class ThrowingEndpointGovernanceService : IAsiBackboneEndpointGovernanceService
    {
        public ValueTask<AsiBackboneEndpointGovernanceResult> EvaluateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Governance service should not be invoked for endpoints without governance metadata.");
        }
    }

    private sealed class BlockingEndpointGovernanceService : IAsiBackboneEndpointGovernanceService
    {
        public ValueTask<AsiBackboneEndpointGovernanceResult> EvaluateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(AsiBackboneEndpointGovernanceResult.Block(
                Results.Problem(statusCode: StatusCodes.Status403Forbidden)));
        }
    }
}
