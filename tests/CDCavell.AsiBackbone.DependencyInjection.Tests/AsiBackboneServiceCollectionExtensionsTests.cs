using CDCavell.AsiBackbone.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.DependencyInjection.Tests;

public sealed class AsiBackboneServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAsiBackboneInvokesConfigureAndReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        bool invoked = false;

        IServiceCollection returned = services.AddAsiBackbone(builder =>
        {
            invoked = true;
            Assert.Same(services, builder.Services);
        });

        Assert.True(invoked);
        Assert.Same(services, returned);
    }

    [Fact]
    public void AddAsiBackboneEmptyConfigurationDoesNotRegisterHiddenDefaults()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(_ => { });

        Assert.Empty(services);
    }

    [Fact]
    public void AddAsiBackbonePartialConfigurationRegistersOnlyNamedProviderServices()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(backbone =>
        {
            backbone.UseMarkerA();
        });

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MarkerA>());
        Assert.Null(provider.GetService<MarkerB>());
        Assert.Null(provider.GetService<MarkerC>());
    }

    [Fact]
    public void AddAsiBackboneComposedConfigurationRegistersAllNamedProviderServices()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(backbone =>
        {
            backbone.UseMarkerA();
            backbone.UseMarkerB();
            backbone.UseMarkerC();
        });

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MarkerA>());
        Assert.NotNull(provider.GetService<MarkerB>());
        Assert.NotNull(provider.GetService<MarkerC>());
    }
}

internal sealed class MarkerA;

internal sealed class MarkerB;

internal sealed class MarkerC;

internal static class TestAsiBackboneBuilderExtensions
{
    public static IAsiBackboneBuilder UseMarkerA(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<MarkerA>();
        return builder;
    }

    public static IAsiBackboneBuilder UseMarkerB(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<MarkerB>();
        return builder;
    }

    public static IAsiBackboneBuilder UseMarkerC(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<MarkerC>();
        return builder;
    }
}
