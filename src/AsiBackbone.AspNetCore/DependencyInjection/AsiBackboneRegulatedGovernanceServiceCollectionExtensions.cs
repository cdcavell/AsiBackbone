using AsiBackbone.Core.Metadata;
using AsiBackbone.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AsiBackbone.AspNetCore.DependencyInjection;

/// <summary>
/// Provides regulated-governance registration helpers for hosts that want a repeatable, fail-closed ASP.NET Core posture.
/// </summary>
public static class AsiBackboneRegulatedGovernanceServiceCollectionExtensions
{
    /// <summary>
    /// Adds ASP.NET Core endpoint governance, applies the strict governance option profile, and registers the
    /// provider-neutral governance metadata sanitation pipeline.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This helper establishes a conservative governance posture; it is not a legal or regulatory compliance
    /// certification. Hosts still own authentication, authorization, policy and threat-model registrations,
    /// capability proof verification, durable replay/use tracking, metadata classifiers, durable audit and outbox
    /// persistence, managed-key signing, monitoring, retention, and final execution enforcement.
    /// </remarks>
    public static IServiceCollection AddAsiBackboneRegulatedGovernance(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddAsiBackboneAspNetCore();
        _ = services.AddAsiBackboneStrictGovernance();

        services.TryAddScoped<IGovernanceMetadataSanitizer>(serviceProvider =>
            new DefaultGovernanceMetadataSanitizer(
                serviceProvider.GetServices<IGovernanceMetadataClassifier>(),
                GovernanceMetadataBudget.Recommended));

        return services;
    }

    /// <summary>
    /// Applies the regulated governance profile through the explicit <c>AddAsiBackbone</c> builder facade.
    /// </summary>
    /// <param name="builder">The AsiBackbone builder facade being configured.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public static IAsiBackboneBuilder UseRegulatedGovernanceProfile(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddAsiBackboneRegulatedGovernance();
        return builder;
    }
}
