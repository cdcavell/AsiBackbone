using Microsoft.Extensions.DependencyInjection;

namespace CDCavell.AsiBackbone.DependencyInjection;

/// <summary>
/// Coordinates explicitly selected AsiBackbone provider registrations for a host service collection.
/// </summary>
public interface IAsiBackboneBuilder
{
    /// <summary>
    /// Gets the host-owned service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }
}
