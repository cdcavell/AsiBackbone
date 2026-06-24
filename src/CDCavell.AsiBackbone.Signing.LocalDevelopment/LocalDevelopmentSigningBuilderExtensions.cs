using AsiBackbone.Core.Signing;
using AsiBackbone.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AsiBackbone.Signing.LocalDevelopment;

/// <summary>
/// Provides explicit builder facade extension methods for local-development signing.
/// </summary>
public static class LocalDevelopmentSigningBuilderExtensions
{
    /// <summary>
    /// Adds local-development signing and verification through the AsiBackbone builder facade using default options.
    /// </summary>
    public static IAsiBackboneBuilder UseLocalDevelopmentSigning(this IAsiBackboneBuilder builder)
    {
        return builder.UseLocalDevelopmentSigning(LocalDevelopmentSigningOptions.Create());
    }

    /// <summary>
    /// Adds local-development signing and verification through the AsiBackbone builder facade using configured options.
    /// </summary>
    public static IAsiBackboneBuilder UseLocalDevelopmentSigning(
        this IAsiBackboneBuilder builder,
        LocalDevelopmentSigningOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        _ = builder.Services.AddSingleton(options);
        _ = builder.Services.AddSingleton<LocalDevelopmentSigningService>();
        _ = builder.Services.AddSingleton<IAsiBackboneSigningService>(serviceProvider =>
            serviceProvider.GetRequiredService<LocalDevelopmentSigningService>());
        _ = builder.Services.AddSingleton<IAsiBackboneSignatureVerificationService>(serviceProvider =>
            serviceProvider.GetRequiredService<LocalDevelopmentSigningService>());

        return builder;
    }
}
