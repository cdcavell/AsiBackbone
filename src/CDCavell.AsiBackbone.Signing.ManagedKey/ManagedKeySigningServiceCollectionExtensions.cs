using CDCavell.AsiBackbone.Core.Signing;
using Microsoft.Extensions.DependencyInjection;

namespace CDCavell.AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Provides dependency injection registration helpers for managed-key signing.
/// </summary>
public static class ManagedKeySigningServiceCollectionExtensions
{
    /// <summary>
    /// Adds managed-key signing with a host-owned managed-key client factory.
    /// </summary>
    public static IServiceCollection AddAsiBackboneManagedKeySigning(
        this IServiceCollection services,
        Action<ManagedKeySigningOptions> configure,
        Func<IServiceProvider, IManagedKeySigningClient> clientFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(clientFactory);

        ManagedKeySigningOptions options = new();
        configure(options);
        options.Validate();

        _ = services.AddSingleton(options);
        _ = services.AddSingleton(clientFactory);
        _ = services.AddSingleton<ManagedKeySigningService>();
        _ = services.AddSingleton<IAsiBackboneSigningService>(serviceProvider =>
            serviceProvider.GetRequiredService<ManagedKeySigningService>());

        return services;
    }

    /// <summary>
    /// Adds managed-key signing using an already-registered <see cref="IManagedKeySigningClient" />.
    /// </summary>
    public static IServiceCollection AddAsiBackboneManagedKeySigning(
        this IServiceCollection services,
        Action<ManagedKeySigningOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        ManagedKeySigningOptions options = new();
        configure(options);
        options.Validate();

        _ = services.AddSingleton(options);
        _ = services.AddSingleton<ManagedKeySigningService>();
        _ = services.AddSingleton<IAsiBackboneSigningService>(serviceProvider =>
            serviceProvider.GetRequiredService<ManagedKeySigningService>());

        return services;
    }
}
