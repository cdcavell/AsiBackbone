using CDCavell.AsiBackbone.AspNetCore.Actors;
using Microsoft.Extensions.DependencyInjection;

namespace CDCavell.AsiBackbone.AspNetCore.DependencyInjection;

/// <summary>
/// Provides dependency injection registration helpers for ASP.NET Core host integration.
/// </summary>
public static class AsiBackboneAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds ASP.NET Core integration services for AsiBackbone using default options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddAsiBackboneAspNetCore(this IServiceCollection services)
    {
        return services.AddAsiBackboneAspNetCore(_ => { });
    }

    /// <summary>
    /// Adds ASP.NET Core integration services for AsiBackbone using configured options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">The options configuration callback.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddAsiBackboneAspNetCore(
        this IServiceCollection services,
        Action<AsiBackboneAspNetCoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        AsiBackboneAspNetCoreOptions options = new();
        configure(options);
        options.Validate();

        _ = services.AddOptions<AsiBackboneAspNetCoreOptions>()
            .Configure(configure)
            .Validate(static options => !string.IsNullOrWhiteSpace(options.CorrelationIdHeaderName), "CorrelationIdHeaderName must be configured.")
            .ValidateOnStart();

        _ = services.AddOptions<AsiBackboneHttpActorContextOptions>()
            .Validate(static options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }, "HTTP actor context options must be valid.")
            .ValidateOnStart();

        _ = services.AddHttpContextAccessor();
        _ = services.AddScoped<IAsiBackboneHttpActorContextResolver, HttpContextAsiBackboneActorContextResolver>();

        return services;
    }
}
