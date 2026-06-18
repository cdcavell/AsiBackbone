using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.AspNetCore.Endpoints;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Evaluation;
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

        await middleware.InvokeAsync(
            httpContext,
            new ThrowingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions()));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task MiddlewareBlocksWhenGovernanceServiceBlocksExecution()
    {
        using ServiceProvider requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };
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

        await middleware.InvokeAsync(
            httpContext,
            new BlockingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions()));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task MiddlewareUsesBodylessDefaultForbiddenResultWhenBlockedWithoutFailureResult()
    {
        using ServiceProvider requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "blocked.default"));
        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            httpContext,
            new DefaultBlockingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions()));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task MiddlewareUsesConfiguredDefaultForbiddenResultFactoryWhenBlockedWithoutFailureResult()
    {
        using ServiceProvider requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "blocked.configured"));
        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            httpContext,
            new DefaultBlockingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions
            {
                DefaultForbiddenResultFactory = _ => Microsoft.AspNetCore.Http.Results.Text(
                    "rich failure response",
                    statusCode: StatusCodes.Status403Forbidden)
            }));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Equal("rich failure response", await ReadResponseBodyAsync(httpContext));
    }

    [Fact]
    public async Task MiddlewarePreservesCustomFailureResultWhenConfiguredDefaultForbiddenResultFactoryExists()
    {
        using ServiceProvider requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireCapabilityGrantAttribute("robotics.execute")),
            "blocked.custom"));
        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            httpContext,
            new TextBlockingEndpointGovernanceService("custom failure response", StatusCodes.Status409Conflict),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions
            {
                DefaultForbiddenResultFactory = _ => Microsoft.AspNetCore.Http.Results.Text(
                    "default factory response",
                    statusCode: StatusCodes.Status403Forbidden)
            }));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal("custom failure response", await ReadResponseBodyAsync(httpContext));
    }

    [Fact]
    public async Task DefaultServiceFailsClosedWhenCapabilityValidatorIsMissing()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = services.CreateScope();
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

    [Fact]
    public async Task DefaultServiceUsesDefaultFailureForDeniedPolicyDecision()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddAsiBackboneAspNetCore()
            .AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>(new DenyingPolicyEvaluator())
            .BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "trace-denied"
        };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireGovernancePolicyAttribute(typeof(SamplePolicy))),
            "policy.denied");
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IAsiBackboneEndpointGovernanceService service = scope.ServiceProvider.GetRequiredService<IAsiBackboneEndpointGovernanceService>();

        AsiBackboneEndpointGovernanceResult result = await service.EvaluateAsync(httpContext, descriptor, TestContext.Current.CancellationToken);

        Assert.False(result.CanExecute);
        Assert.Null(result.FailureResult);
        Assert.NotNull(result.Decision);
        Assert.True(result.Decision.IsDenied);
        Assert.Contains("policy.denied", result.Decision.ReasonCodes);
    }

    [Fact]
    public async Task MiddlewareBlocksUngovernedEndpointWhenRequireGovernanceMetadataIsEnabled()
    {
        using ServiceProvider requestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };

        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(),
            "plain"));

        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            httpContext,
            new ThrowingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions
            {
                RequireGovernanceMetadata = true
            }));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task MiddlewareAllowsUngovernedEndpointWithExplicitMissingGovernanceMetadataOptOut()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowMissingGovernanceMetadataAttribute()),
            "public"));

        bool nextCalled = false;
        var middleware = new AsiBackboneEndpointGovernanceMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            httpContext,
            new ThrowingEndpointGovernanceService(),
            Microsoft.Extensions.Options.Options.Create(new AsiBackboneEndpointGovernanceOptions
            {
                RequireGovernanceMetadata = true
            }));

        Assert.True(nextCalled);
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
                Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status403Forbidden)));
        }
    }

    private sealed class DefaultBlockingEndpointGovernanceService : IAsiBackboneEndpointGovernanceService
    {
        public ValueTask<AsiBackboneEndpointGovernanceResult> EvaluateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(AsiBackboneEndpointGovernanceResult.BlockWithDefaultFailure());
        }
    }

    private sealed class TextBlockingEndpointGovernanceService(string body, int statusCode) : IAsiBackboneEndpointGovernanceService
    {
        public ValueTask<AsiBackboneEndpointGovernanceResult> EvaluateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(AsiBackboneEndpointGovernanceResult.Block(
                Microsoft.AspNetCore.Http.Results.Text(body, statusCode: statusCode)));
        }
    }

    private sealed class DenyingPolicyEvaluator : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<GovernanceDecision>(GovernanceDecision.Deny(
                "policy.denied",
                "The policy denied execution.",
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash));
        }
    }
}
