using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsiBackbone.DependencyInjection.Tests;

/// <summary>
/// Tests for the <see cref="AsiBackboneServiceCollectionExtensions"/> class.
/// </summary>
public sealed class AsiBackboneServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that the <see cref="AsiBackboneServiceCollectionExtensions.AddAsiBackbone(IServiceCollection, Action{IAsiBackboneBuilder})"/> method invokes the provided configuration action and returns the same service collection instance.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneServiceCollectionExtensions.AddAsiBackbone(IServiceCollection, Action{IAsiBackboneBuilder})"/> method does not register any hidden default services when the configuration action is empty.
    /// </summary>
    [Fact]
    public void AddAsiBackboneEmptyConfigurationDoesNotRegisterHiddenDefaults()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(_ => { });

        Assert.Empty(services);
    }

    /// <summary>
    /// Tests that the <see cref="AsiBackboneServiceCollectionExtensions.AddAsiBackbone(IServiceCollection, Action{IAsiBackboneBuilder})"/> method registers only the named provider services when a partial configuration is provided.
    /// </summary>
    [Fact]
    public void AddAsiBackbonePartialConfigurationRegistersOnlyNamedProviderServices()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(backbone => backbone.UseMarkerA());

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MarkerA>());
        Assert.Null(provider.GetService<MarkerB>());
        Assert.Null(provider.GetService<MarkerC>());
    }

    /// <summary>
    /// Tests that the <see cref="AsiBackboneServiceCollectionExtensions.AddAsiBackbone(IServiceCollection, Action{IAsiBackboneBuilder})"/> method registers all named provider services when a composed configuration is provided.
    /// </summary>
    [Fact]
    public void AddAsiBackboneComposedConfigurationRegistersAllNamedProviderServices()
    {
        var services = new ServiceCollection();

        _ = services.AddAsiBackbone(backbone =>
        {
            _ = backbone.UseMarkerA();
            _ = backbone.UseMarkerB();
            _ = backbone.UseMarkerC();
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
