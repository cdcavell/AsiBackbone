using CDCavell.AsiBackbone.AspNetCore.Actors;
using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.AspNetCore.Handshakes;
using CDCavell.AsiBackbone.AspNetCore.Outbox;
using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            }, "ASP.NET Core integration options must be valid.")
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

        _ = services.AddOptions<AsiBackboneHttpResultMappingOptions>()
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
            }, "HTTP result mapping options must be valid.")
            .ValidateOnStart();

        _ = services.AddOptions<AsiBackboneAcknowledgmentChallengeOptions>()
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
            }, "Acknowledgment challenge options must be valid.")
            .ValidateOnStart();

        _ = services.AddHttpContextAccessor();
        _ = services.AddScoped<IAsiBackboneHttpActorContextResolver, HttpContextAsiBackboneActorContextResolver>();
        _ = services.AddScoped<IAsiBackboneHttpRequestCorrelationResolver, HttpContextAsiBackboneRequestCorrelationResolver>();
        _ = services.AddScoped<IAsiBackboneAcknowledgmentChallengeService, DefaultAsiBackboneAcknowledgmentChallengeService>();

        return services;
    }

    /// <summary>
    /// Adds the host-owned governance outbox drain worker using default scheduling options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddAsiBackboneGovernanceOutboxDrainWorker(this IServiceCollection services)
    {
        return services.AddAsiBackboneGovernanceOutboxDrainWorker(_ => { });
    }

    /// <summary>
    /// Adds the host-owned governance outbox drain worker using configured scheduling options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">The worker options configuration callback.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddAsiBackboneGovernanceOutboxDrainWorker(
        this IServiceCollection services,
        Action<AsiBackboneGovernanceOutboxDrainWorkerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        AsiBackboneGovernanceOutboxDrainWorkerOptions options = new();
        configure(options);
        options.Validate();

        _ = services.AddOptions<AsiBackboneGovernanceOutboxDrainWorkerOptions>()
            .Configure(configure)
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
            }, "Governance outbox drain worker options must be valid.")
            .ValidateOnStart();

        services.TryAddScoped<AsiBackboneGovernanceOutboxDrain>();
        _ = services.AddHostedService<AsiBackboneGovernanceOutboxDrainHostedService>();

        return services;
    }
}