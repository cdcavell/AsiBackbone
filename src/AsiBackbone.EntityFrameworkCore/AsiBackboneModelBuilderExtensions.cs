using Microsoft.EntityFrameworkCore;

namespace AsiBackbone.EntityFrameworkCore;

/// <summary>
/// Provides Entity Framework Core model configuration hooks for ASI Backbone persistence integration.
/// </summary>
public static class AsiBackboneModelBuilderExtensions
{
    /// <summary>
    /// Applies ASI Backbone Entity Framework Core configurations to a host-owned model builder.
    /// </summary>
    /// <param name="modelBuilder">The host-owned model builder.</param>
    /// <returns>The same model builder instance for fluent composition.</returns>
    public static ModelBuilder ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AsiBackboneModelBuilderExtensions).Assembly);

        return modelBuilder;
    }
}
