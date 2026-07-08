using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AsiBackbone.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for the <see cref="AsiBackboneModelBuilderExtensions"/> class, which provides extension methods for configuring the Entity Framework Core model with ASI Backbone conventions.
/// </summary>
public sealed class AsiBackboneModelBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that ApplyAsiBackboneConfigurations returns the same ModelBuilder instance for fluent host-owned configuration.
    /// </summary>
    [Fact]
    public void ApplyAsiBackboneConfigurationsReturnsModelBuilder()
    {
        ModelBuilder modelBuilder = new();

        ModelBuilder result = modelBuilder.ApplyAsiBackboneConfigurations();

        Assert.Same(modelBuilder, result);
    }

    /// <summary>
    /// Verifies that ApplyAsiBackboneConfigurations guards against null model builders.
    /// </summary>
    [Fact]
    public void ApplyAsiBackboneConfigurationsThrowsForNullModelBuilder()
    {
        ModelBuilder? modelBuilder = null;

        _ = Assert.Throws<ArgumentNullException>(() => modelBuilder!.ApplyAsiBackboneConfigurations());
    }

    /// <summary>
    /// Verifies that ApplyAsiBackboneConfigurations can be called from a host-owned DbContext and applies configurations correctly.
    /// </summary>
    [Fact]
    public void ApplyAsiBackboneConfigurationsCanBeCalledFromHostOwnedDbContext()
    {
        DbContextOptions<HostOwnedDbContext> options =
            new DbContextOptionsBuilder<HostOwnedDbContext>()
                .UseInMemoryDatabase($"asi-backbone-{Guid.NewGuid():N}")
                .Options;

        using HostOwnedDbContext context = new(options);

        _ = context.Model;

        Assert.True(context.AsiBackboneConfigurationsApplied);
        Assert.NotNull(context.Model.FindEntityType(typeof(HostOwnedEntity)));
    }

    private sealed class HostOwnedDbContext(DbContextOptions<HostOwnedDbContext> options)
        : DbContext(options)
    {
        public bool AsiBackboneConfigurationsApplied { get; private set; }

        public DbSet<HostOwnedEntity> HostOwnedEntities => Set<HostOwnedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ModelBuilder result = modelBuilder.ApplyAsiBackboneConfigurations();

            AsiBackboneConfigurationsApplied = ReferenceEquals(modelBuilder, result);
        }
    }

    private sealed class HostOwnedEntity
    {
        public int Id { get; set; }
    }
}
