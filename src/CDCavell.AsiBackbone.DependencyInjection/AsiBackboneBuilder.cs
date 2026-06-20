using Microsoft.Extensions.DependencyInjection;

namespace CDCavell.AsiBackbone.DependencyInjection;

/// <summary>
/// Default implementation of the AsiBackbone dependency injection builder facade.
/// </summary>
public sealed class AsiBackboneBuilder : IAsiBackboneBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsiBackboneBuilder" /> class.
    /// </summary>
    /// <param name="services">The host-owned service collection being configured.</param>
    public AsiBackboneBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
