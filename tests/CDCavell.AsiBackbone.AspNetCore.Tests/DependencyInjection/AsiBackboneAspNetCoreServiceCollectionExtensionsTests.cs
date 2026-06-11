using CDCavell.AsiBackbone.AspNetCore.Actors;
using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.AspNetCore.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.DependencyInjection;

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
}
