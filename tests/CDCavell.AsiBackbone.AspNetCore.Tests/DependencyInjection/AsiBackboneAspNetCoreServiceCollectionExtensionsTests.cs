using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
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
        Assert.Equal("X-Correlation-ID", options.CorrelationIdHeaderName);
    }

    [Fact]
    public void AddAsiBackboneAspNetCoreAppliesConfiguredOptions()
    {
        ServiceCollection services = new();

        _ = services.AddAsiBackboneAspNetCore(options =>
        {
            options.IncludeRouteValues = false;
            options.IncludeEndpointMetadata = false;
            options.CorrelationIdHeaderName = "X-Request-ID";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        AsiBackboneAspNetCoreOptions options = provider.GetRequiredService<IOptions<AsiBackboneAspNetCoreOptions>>().Value;

        Assert.False(options.IncludeRouteValues);
        Assert.False(options.IncludeEndpointMetadata);
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

        Assert.Contains("CorrelationIdHeaderName", exception.Message, StringComparison.Ordinal);
    }
}
