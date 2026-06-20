using CDCavell.AsiBackbone.DependencyInjection;

namespace CDCavell.AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Provides explicit builder facade extension methods for managed-key signing.
/// </summary>
public static class ManagedKeySigningBuilderExtensions
{
    /// <summary>
    /// Adds managed-key signing through the AsiBackbone builder facade with a host-owned managed-key client factory.
    /// </summary>
    public static IAsiBackboneBuilder UseManagedKeySigning(
        this IAsiBackboneBuilder builder,
        Action<ManagedKeySigningOptions> configure,
        Func<IServiceProvider, IManagedKeySigningClient> clientFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(clientFactory);

        _ = builder.Services.AddAsiBackboneManagedKeySigning(configure, clientFactory);
        return builder;
    }

    /// <summary>
    /// Adds managed-key signing through the AsiBackbone builder facade using an already-registered managed-key client.
    /// </summary>
    public static IAsiBackboneBuilder UseManagedKeySigning(
        this IAsiBackboneBuilder builder,
        Action<ManagedKeySigningOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        _ = builder.Services.AddAsiBackboneManagedKeySigning(configure);
        return builder;
    }
}
