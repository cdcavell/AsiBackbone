using Microsoft.Extensions.DependencyInjection;

namespace AsiBackbone.DependencyInjection;

/// <summary>
/// Provides the explicit <c>AddAsiBackbone</c> dependency injection entry point.
/// </summary>
public static class AsiBackboneServiceCollectionExtensions
{
    /// <summary>
    /// Adds an explicit AsiBackbone builder facade for host-selected provider registrations.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">The callback that names the provider registrations the host wants.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method does not register Core, persistence, endpoint governance, telemetry, signing, outbox, or local-development providers by itself.
    /// Provider packages contribute their own <c>Use*</c> extension methods, and only those explicitly called by the host add services.
    /// </remarks>
    public static IServiceCollection AddAsiBackbone(
        this IServiceCollection services,
        Action<IAsiBackboneBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AsiBackboneBuilder(services);
        configure(builder);

        return services;
    }
}
